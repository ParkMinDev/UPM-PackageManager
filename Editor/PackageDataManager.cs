using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor.PackageManager;
using Cysharp.Threading.Tasks;

namespace ParkMinPackages.PackageManager.Editor
{
	internal static class PackageDataManager
	{
		// public static async Awaitable<List<PackageData>> asd(string gitURL, CancellationToken cancellationToken) {
		// 	
		// 	
		// }


		public static async UniTask<List<PackageData>> RequestToOwnerAsync(
			string personalAccessToken,
			string owner,
			string[] exceptRepos,
			PackageCollection unityPackageCollection,
			PackageDependencyResolver dependencyResolver,
			CancellationToken cancellationToken
		) {
			List<PackageData> packageDatas = new List<PackageData>();
			HashSet<string> exceptRepoSet = new HashSet<string>(exceptRepos, StringComparer.OrdinalIgnoreCase);

			List<GitRestAPI.Repo> repoList = await GitRestAPI.GetOwnerReposAsync(personalAccessToken, owner, cancellationToken);
			if (cancellationToken.IsCancellationRequested) throw new OperationCanceledException();

			foreach (GitRestAPI.Repo repo in repoList) {
				if (exceptRepoSet.Contains(repo.name))
					continue;

				GitRestAPI.UpmInfoJson info = await GitRestAPI.GetUpmInfoAsync(personalAccessToken, owner, repo.name, repo.default_branch, cancellationToken);
				if (info == null || info.packages.Count == 0) continue;
				string remoteLastCommitHash = await GitRestAPI.GetLastCommitHashAsync(personalAccessToken, owner, repo.name, repo.default_branch, cancellationToken);

				foreach (GitRestAPI.UpmPackageJson package in info.packages) {
					string[] packagePathSegments = string.IsNullOrWhiteSpace(package.packagePath)
						? Array.Empty<string>()
						: package.packagePath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
					if (packagePathSegments.Any(segment => segment == "." || segment == ".." || segment.Contains(":") || segment.Contains("?") || segment.Contains("#"))) {
						throw new InvalidOperationException($"Invalid packagePath in {repo.name}/parkmin-upm.json");
					}

					string packagePath = string.Join("/", packagePathSegments);
					GitRestAPI.PackageJson remotePackageJson = await GitRestAPI.GetPackageJsonAsync(personalAccessToken, owner, repo.name, repo.default_branch, packagePath, cancellationToken);
					if (packageDatas.Any(data => data.PackageName == remotePackageJson.name)) throw new InvalidOperationException($"Duplicate package declaration: {remotePackageJson.name}");
					PackageInfo unityPackageInfo = unityPackageCollection.FirstOrDefault(packageInfo => packageInfo.name == remotePackageJson.name);

					PackageData packageData = new PackageData();
					packageData.RepoName = repo.name;
					packageData.DisplayName = string.IsNullOrEmpty(packagePath) ? repo.name : $"{repo.name}/{packagePath}";
					packageData.Version = remotePackageJson.version;
					packageData.GitCloneURL = string.IsNullOrEmpty(packagePath)
						? repo.clone_url
						: $"{repo.clone_url}?path=/{string.Join("/", packagePathSegments.Select(Uri.EscapeDataString))}";
					packageData.PackageName = remotePackageJson.name;
					packageData.CurrentCommitHash = unityPackageInfo?.git?.hash;
					packageData.RemoteCommitHash = remoteLastCommitHash;
					packageData.IsEmbed = unityPackageInfo?.source == PackageSource.Embedded;
					packageData.IsLocal = unityPackageInfo?.source == PackageSource.Local;
					packageData.IsPrivate = repo.IsPrivate;
					packageData.GitDependencies = dependencyResolver.ResolveGit(package.gitDependencies);
					packageData.NuGetDependencies = dependencyResolver.ResolveNuGet(package.nugetDependencies);
					packageDatas.Add(packageData);
				}
			}


			return packageDatas;
		}
	}
}
