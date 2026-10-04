using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ParkMinDev.UPM.PackageManager.Editor
{
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.PackageManager.Editor", sourceAssembly: "ParkMinPackages.PackageManager.Editor", sourceClassName: "GitDependency")]
	[Serializable]
	internal class GitDependency
	{
		[JsonProperty("packageName")] public string PackageName;
		[JsonProperty("version")] public string Version;
		[JsonProperty("url")] public string URL;
	}

	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.PackageManager.Editor", sourceAssembly: "ParkMinPackages.PackageManager.Editor", sourceClassName: "NuGetDependency")]
	[Serializable]
	internal class NuGetDependency
	{
		[JsonProperty("packageName")] public string PackageName;
		[JsonProperty("version")] public string Version;
	}

	internal enum PackageDependencyState
	{
		Installed,
		NotInstalled,
		VersionMismatch,
		Unavailable
	}

	internal struct PackageDependencyData
	{
		public string Name;
		public string Version;
		public string URL;
		public string InstalledVersion;
		public PackageDependencyState State;
	}

	internal struct PackageData
	{
		public string RepoName;
		public string DisplayName;
		public string Version;
		public string GitCloneURL;
		public string PackageName;
		public string RemoteCommitHash;
		public string CurrentCommitHash;
		public bool IsEmbed;
		public bool IsLocal;
		public bool IsPrivate;
		public IReadOnlyList<PackageDependencyData> GitDependencies;
		public IReadOnlyList<PackageDependencyData> NuGetDependencies;
		public PackageState State
		{
			get
			{
				if (IsEmbed) {
					return PackageState.Embedded;
				}
				if (IsLocal) {
					return PackageState.Local;
				}

				if (CurrentCommitHash == null) {
					return PackageState.UnInstalled;
				}
				else if (CurrentCommitHash == RemoteCommitHash) {
					return PackageState.Installed;
				}
				else if (CurrentCommitHash != RemoteCommitHash) {
					return PackageState.Updateable;
				}

				throw new NotImplementedException();
			}
		}
	}
}
