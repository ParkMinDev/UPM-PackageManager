using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace ParkMinDev.UPM.PackageManager.Editor
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.PackageManager.Editor", sourceAssembly: "ParkMinPackages.PackageManager.Editor", sourceClassName: "PackageManagerWindow")]
	internal class PackageManagerWindow : EditorWindow
	{
		const string _showDependenciesEditorPrefsKey = "ParkMinDev.UPM.PackageManager.ShowDependencies";

		async Awaitable CreateGUI() {
			if (_cts != null) {
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}

			_cts = new CancellationTokenSource();

			//초기화
			string personalAccessToken = PersonalAccessTokenManager.LoadToken();
			string owner = "ParkMinDev";
			string[] exceptRepos = new string[] { "Package-Dev" };

			PublicGitRepoDatas publicGitRepoDatas = AssetDatabase.LoadAssetAtPath<PublicGitRepoDatas>(
				"Packages/com.parkmindev.upm.packagemanager/PublicGitRepoDatas/PublicGitRepoDatas.asset"
			);

			rootVisualElement.Clear();

			// - Window UI -
			Label title = new Label("ParkMinDev Package Manager");
			title.style.unityTextAlign = TextAnchor.UpperCenter;
			title.style.unityFontStyleAndWeight = FontStyle.Bold;
			title.style.color = (Color)new Color32(0, 255, 37, 255);
			rootVisualElement.Add(title);

			Button pacakgesFolderButton = new Button { text = "Packages Folder" };
			pacakgesFolderButton.style.width = 126;
			pacakgesFolderButton.style.height = 20;
			pacakgesFolderButton.style.alignSelf = Align.Center;
			rootVisualElement.Add(pacakgesFolderButton);

			Foldout personalAccessTokenFoldout = new Foldout { text = "GitHub Personal Access Token (비공개 패키지용)", value = false };
			personalAccessTokenFoldout.style.unityFontStyleAndWeight = FontStyle.Bold;
			TextField personalAccessTokenTextField = new TextField { tooltip = "공개 패키지는 토큰 없이 사용할 수 있습니다. 비공개 패키지는 GitHub PAT를 입력하세요." };
			personalAccessTokenTextField.textEdition.placeholder = personalAccessTokenTextField.tooltip;
			personalAccessTokenFoldout.Add(personalAccessTokenTextField);
			rootVisualElement.Add(personalAccessTokenFoldout);

			VisualElement selectedActions = new VisualElement();
			selectedActions.style.flexDirection = FlexDirection.Row;
			selectedActions.style.justifyContent = Justify.Center;
			selectedActions.style.flexShrink = 0;
			Button installSelectedButton = new Button { text = "Install Selected" };
			Button removeSelectedButton = new Button { text = "Remove Selected" };
			foreach (Button button in new[] { installSelectedButton, removeSelectedButton }) {
				button.style.width = 130;
				button.style.height = 20;
				selectedActions.Add(button);
			}
			rootVisualElement.Add(selectedActions);

			VisualElement refreshActions = new VisualElement();
			refreshActions.style.flexDirection = FlexDirection.Row;
			refreshActions.style.justifyContent = Justify.Center;
			refreshActions.style.alignItems = Align.Center;
			refreshActions.style.flexShrink = 0;
			Button refreshButton = new Button { text = "Refresh" };
			refreshButton.style.width = 100;
			refreshButton.style.height = 20;
			Toggle showDependenciesToggle = new Toggle("Show Dependencies");
			showDependenciesToggle.style.marginLeft = 8;
			refreshActions.Add(refreshButton);
			refreshActions.Add(showDependenciesToggle);
			rootVisualElement.Add(refreshActions);

			// - Package Sections -
			ScrollView scrollView = new ScrollView { verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible };
			scrollView.style.flexGrow = 1;
			scrollView.style.minHeight = 0;
			scrollView.style.paddingLeft = 10;
			scrollView.style.paddingRight = 0;
			scrollView.Add(CreatePackageSection("Public Git Packages", out VisualElement publicGitPackagesContainer));
			scrollView.Add(CreatePackageSection("Public ParkMinDev", out VisualElement publicParkMinPackagesContainer));
			VisualElement privateParkMinDevSection = CreatePackageSection("Private ParkMinDev", out VisualElement privateParkMinPackagesContainer);
			privateParkMinDevSection.style.display = DisplayStyle.None;
			scrollView.Add(privateParkMinDevSection);
			rootVisualElement.Add(scrollView);

			Label refreshStateLabel = new Label("Refreshing...");
			refreshStateLabel.style.unityTextAlign = TextAnchor.UpperCenter;
			refreshStateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			rootVisualElement.Add(refreshStateLabel);
			List<GitItemUI> itemUiList = new List<GitItemUI>();
			bool showDependencies = EditorPrefs.GetBool(_showDependenciesEditorPrefsKey, false);
			showDependenciesToggle.SetValueWithoutNotify(showDependencies);

			//pacakgesFolderButton 구현
			pacakgesFolderButton.clicked += async () =>
			{
				UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Packages/com.parkmindev.upm.packagemanager/UnityPackages");
				Selection.activeObject = folder;
				EditorUtility.FocusProjectWindow();
				EditorGUIUtility.PingObject(folder);
				AssetDatabase.OpenAsset(folder);
			};

			//PersonalAccessToken UI 구현
			personalAccessTokenTextField.value = PersonalAccessTokenManager.LoadToken();
			personalAccessTokenTextField.RegisterValueChangedCallback(evt =>
			{
				PersonalAccessTokenManager.SaveToken(evt.newValue);
			});
			showDependenciesToggle.RegisterValueChangedCallback(evt =>
			{
				showDependencies = evt.newValue;
				EditorPrefs.SetBool(_showDependenciesEditorPrefsKey, showDependencies);
				foreach (GitItemUI itemUI in itemUiList) {
					itemUI.SetDependenciesVisible(showDependencies);
				}
			});

			//_installSelectedButton 구현
			installSelectedButton.clicked += async () =>
			{
				await ClientUtility.AddAndRemoveAsyncWithProgressBar(itemUiList.Where(ui => ui.Checked).Select(ui => ui.GitURL).ToArray(), null);
				CreateGUI();
			};

			//_removeSelectedButton 구현
			removeSelectedButton.clicked += async () =>
			{
				await ClientUtility.AddAndRemoveAsyncWithProgressBar(null, itemUiList.Where(ui => ui.Checked).Select(ui => ui.PackageName).ToArray());
				CreateGUI();
			};

			//_refreshButton 구현
			refreshButton.clicked += () =>
			{
				CreateGUI();
			};

			//아이템 채우기 구현
			try {
				Action afterButtonClickAction = () =>
				{
					CreateGUI();
				};

				PackageCollection packageCollection = await ClientUtility.ListAsync(true);
				if (_cts.IsCancellationRequested) throw new OperationCanceledException();
				PackageDependencyResolver dependencyResolver = new PackageDependencyResolver(packageCollection);

				//Public Repo
				foreach (PublicGitRepoData data in publicGitRepoDatas.Value) {
					PublicGitItemUI publicGitItemUI = new PublicGitItemUI(packageCollection, publicGitPackagesContainer,
						data.DisplayName,
						data.Version,
						data.CloneURL,
						data.PackageName,
						afterButtonClickAction
					);
					publicGitItemUI.SetDependencies(
						dependencyResolver.ResolveGit(data.GitDependencies),
						dependencyResolver.ResolveNuGet(data.NuGetDependencies)
					);
					publicGitItemUI.SetDependenciesVisible(showDependencies);
					itemUiList.Add(publicGitItemUI);
				}

				//Private Organization Repo
				List<PackageData> requestAsync = await PackageDataManager.RequestToOwnerAsync(
					personalAccessToken,
					owner,
					exceptRepos,
					packageCollection,
					dependencyResolver,
					_cts.Token
				);

				foreach (PackageData packageData in requestAsync) {
					GitItemUI eachGitItemUI = new GitItemUI(
						packageData.IsPrivate ? privateParkMinPackagesContainer : publicParkMinPackagesContainer,
						packageData.DisplayName,
						packageData.Version,
						packageData.GitCloneURL,
						packageData.PackageName,
						afterButtonClickAction
					);
					eachGitItemUI.SetDependencies(packageData.GitDependencies, packageData.NuGetDependencies);
					eachGitItemUI.SetDependenciesVisible(showDependencies);
					eachGitItemUI.State = packageData.State;
					itemUiList.Add(eachGitItemUI);
				}

				privateParkMinDevSection.style.display = privateParkMinPackagesContainer.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
				refreshStateLabel.style.display = DisplayStyle.None;
			}
			catch (OperationCanceledException) { }
			catch (Exception e) {
				Debug.LogException(e);
				throw;
			}
		}
		void OnDisable() {
			if (_cts != null) {
				_cts.Cancel();
				_cts.Dispose();
				_cts = null;
			}
		}

		CancellationTokenSource _cts;

		static VisualElement CreatePackageSection(string title, out VisualElement packagesContainer) {
			VisualElement section = new VisualElement();
			section.style.flexShrink = 0;
			Label heading = new Label($"[ {title} ]");
			heading.style.marginTop = 10;
			heading.style.marginBottom = 3;
			heading.style.unityFontStyleAndWeight = FontStyle.Bold;
			heading.style.fontSize = 15;
			heading.style.color = (Color)new Color32(138, 180, 248, 255);
			packagesContainer = new VisualElement();
			packagesContainer.style.flexShrink = 0;
			section.Add(heading);
			section.Add(packagesContainer);
			return section;
		}

		//Type
		class PublicGitItemUI : GitItemUI
		{
			public PublicGitItemUI(
				PackageCollection packageCollection,
				VisualElement parent,
				string displayName,
				string version,
				string gitURL,
				string packageName,
				Action afterButtonClickAction
			) : base(parent, displayName, version, gitURL, packageName, afterButtonClickAction) {
				UnityEditor.PackageManager.PackageInfo packageInfo = packageCollection.FirstOrDefault(info => info.name == packageName);
				if (packageInfo != null) {
					if (packageInfo.source == PackageSource.Embedded) {
						State = PackageState.Embedded;
					}
					else {
						State = PackageState.Installed;
					}
				}
				else
					State = PackageState.UnInstalled;
			}
		}

		class GitItemUI : ItemUI
		{
			public GitItemUI(
				VisualElement parent,
				string displayName,
				string version,
				string gitURL,
				string packageName,
				Action afterButtonClickAction
			) : base(parent) {
				_gitURL = gitURL;
				_packageName = packageName;
				DisplayName = $"{displayName} #{(string.IsNullOrWhiteSpace(version) ? "최신" : version)}";

				_installButton.clicked += async () =>
				{
					await ClientUtility.AddAsyncWithProgressBar(gitURL);
					afterButtonClickAction?.Invoke();
				};
				_removeButton.clicked += async () =>
				{
					await ClientUtility.RemoveAsyncWithProgressBar(packageName);
					afterButtonClickAction?.Invoke();
				};
				_embedButton.clicked += async () =>
				{
					await ClientUtility.EmbedAsyncWithProgressBar(packageName);
					afterButtonClickAction?.Invoke();
				};
			}
			public string GitURL
			{
				get { return _gitURL; }
			}
			public string PackageName
			{
				get { return _packageName; }
			}
			readonly string _gitURL;
			readonly string _packageName;
		}

		class ItemUI
		{
			public ItemUI(VisualElement parent) {
				// - Package Row -
				VisualElement item = new VisualElement();
				item.style.flexShrink = 0;
				VisualElement row = new VisualElement();
				row.style.flexDirection = FlexDirection.Row;
				row.style.justifyContent = Justify.SpaceBetween;
				row.style.alignItems = Align.Center;
				row.style.flexShrink = 0;

				VisualElement selection = new VisualElement();
				selection.style.flexDirection = FlexDirection.Row;
				selection.style.alignItems = Align.Center;
				selection.style.flexGrow = 1;
				selection.style.minWidth = 0;
				_toggle = new Toggle();
				_displayNameLabel = new Label();
				_displayNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
				selection.Add(_toggle);
				selection.Add(_displayNameLabel);

				VisualElement actions = new VisualElement();
				actions.style.flexDirection = FlexDirection.Row;
				actions.style.alignItems = Align.Center;
				actions.style.flexShrink = 0;
				_stateLabel = new Label();
				_stateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
				_installButton = new Button { text = "설치" };
				_removeButton = new Button { text = "삭제" };
				_embedButton = new Button { text = "Embed" };
				actions.Add(_stateLabel);
				foreach (Button button in new[] { _installButton, _removeButton, _embedButton }) {
					button.style.unityFontStyleAndWeight = FontStyle.Bold;
					actions.Add(button);
				}
				row.Add(selection);
				row.Add(actions);
				item.Add(row);

				// - Dependencies -
				_dependenciesContainer = new VisualElement();
				_dependenciesContainer.style.display = DisplayStyle.None;
				_dependenciesContainer.style.marginLeft = 22;
				_dependenciesContainer.style.marginRight = 4;
				_dependenciesContainer.style.marginBottom = 3;
				_dependenciesContainer.style.flexShrink = 0;
				item.Add(_dependenciesContainer);

				State = PackageState.UnInstalled;

				parent.Add(item);
			}

			public void SetDependencies(
				IReadOnlyList<PackageDependencyData> gitDependencies,
				IReadOnlyList<PackageDependencyData> nuGetDependencies
			) {
				_dependenciesContainer.Clear();
				AddDependencyRows("Git", gitDependencies);
				AddDependencyRows("NuGet", nuGetDependencies);
				UpdateDependenciesVisibility();
			}
			public void SetDependenciesVisible(bool visible) {
				_showDependencies = visible;
				UpdateDependenciesVisibility();
			}
			public PackageState State
			{
				get { return _state; }
				set
				{
					_state = value;

					switch (_state) {
						case PackageState.UnInstalled:
							_stateLabel.text = "설치안됨";
							_installButton.enabledSelf = true;
							_removeButton.enabledSelf = false;
							_embedButton.enabledSelf = false;
							_stateLabel.style.color = new StyleColor(StyleKeyword.Null);
							break;
						case PackageState.Updateable:
							_stateLabel.text = "업데이트가능";
							_installButton.enabledSelf = true;
							_removeButton.enabledSelf = true;
							_embedButton.enabledSelf = true;
							_stateLabel.style.color = new StyleColor(Color.yellow);
							break;
						case PackageState.Installed:
							_stateLabel.text = "설치됨";
							_installButton.enabledSelf = true;
							_removeButton.enabledSelf = true;
							_embedButton.enabledSelf = true;
							_stateLabel.style.color = new StyleColor(Color.green);
							break;
						case PackageState.Local:
							_stateLabel.text = "로컬 연결";
							_installButton.enabledSelf = false;
							_removeButton.enabledSelf = true;
							_embedButton.enabledSelf = false;
							_stateLabel.style.color = new StyleColor(Color.green);
							break;
						case PackageState.Embedded:
							_stateLabel.text = "Embed";
							_installButton.enabledSelf = false;
							_removeButton.enabledSelf = false;
							_embedButton.enabledSelf = false;
							_stateLabel.style.color = new StyleColor(Color.green);
							break;
					}
				}
			}
			public string DisplayName
			{
				get { return _displayName; }
				set
				{
					_displayName = value;
					_displayNameLabel.text = value;
				}
			}
			public bool Checked
			{
				get { return _toggle.value; }
				set { _toggle.value = value; }
			}

			protected Toggle _toggle;
			protected Label _displayNameLabel;
			protected Label _stateLabel;
			protected VisualElement _dependenciesContainer;
			protected Button _installButton;
			protected Button _removeButton;
			protected Button _embedButton;
			PackageState _state;
			string _displayName;
			bool _showDependencies;

			void AddDependencyRows(
				string category,
				IReadOnlyList<PackageDependencyData> dependencies
			) {
				if (dependencies == null || dependencies.Count == 0) {
					return;
				}

				foreach (PackageDependencyData dependency in dependencies) {
					VisualElement dependencyRow = new VisualElement();
					dependencyRow.style.flexDirection = FlexDirection.Row;
					dependencyRow.style.justifyContent = Justify.SpaceBetween;

					string version = string.IsNullOrWhiteSpace(dependency.Version) ? string.Empty : $" {dependency.Version}";
					Label dependencyNameLabel = new Label($"{category} · {dependency.Name}{version}");
					dependencyNameLabel.style.flexGrow = 1;
					dependencyNameLabel.tooltip = dependency.URL;

					Label dependencyStateLabel = new Label(GetDependencyStateText(dependency));
					dependencyStateLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
					dependencyStateLabel.style.color = GetDependencyStateColor(dependency.State);

					dependencyRow.Add(dependencyNameLabel);
					dependencyRow.Add(dependencyStateLabel);
					_dependenciesContainer.Add(dependencyRow);
				}
			}
			void UpdateDependenciesVisibility() {
				_dependenciesContainer.style.display = _showDependencies && _dependenciesContainer.childCount > 0
					? DisplayStyle.Flex
					: DisplayStyle.None;
			}
			static string GetDependencyStateText(PackageDependencyData dependency) {
				switch (dependency.State) {
					case PackageDependencyState.Installed:
						return "설치됨";
					case PackageDependencyState.NotInstalled:
						return "설치안됨";
					case PackageDependencyState.VersionMismatch:
						return string.IsNullOrWhiteSpace(dependency.InstalledVersion)
							? "버전 불일치"
							: $"버전 불일치 ({dependency.InstalledVersion})";
					case PackageDependencyState.Unavailable:
						return "확인 불가";
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			static StyleColor GetDependencyStateColor(PackageDependencyState state) {
				switch (state) {
					case PackageDependencyState.Installed:
						return new StyleColor(Color.green);
					case PackageDependencyState.NotInstalled:
					case PackageDependencyState.VersionMismatch:
						return new StyleColor(Color.yellow);
					case PackageDependencyState.Unavailable:
						return new StyleColor(StyleKeyword.Null);
					default:
						throw new ArgumentOutOfRangeException(nameof(state), state, null);
				}
			}
		}
	}
}
