using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace ParkMinDev.UPM.PackageManager.Editor
{
	internal static class GitRestAPI
	{
		//GetOrganizationReposAsync
		public static async UniTask<List<Repo>> GetOwnerReposAsync(string personalAccessToken, string owner, CancellationToken cancellationToken) {
			string escapedOwner = UnityWebRequest.EscapeURL(owner);
			string accountType;
			using (UnityWebRequest request = UnityWebRequest.Get($"https://api.github.com/users/{escapedOwner}")) {
				string json = await SendBearerRequestAsync(request, personalAccessToken, cancellationToken);
				accountType = JObject.Parse(json)["type"]?.ToString();
			}
			string endpoint = accountType == "Organization" ? "orgs" : "users";
			string ownerRepositoryEndpoint = $"https://api.github.com/{endpoint}/{escapedOwner}/repos";
			string[] repositoryEndpoints = accountType != "Organization" && !string.IsNullOrWhiteSpace(personalAccessToken) ? new[] { ownerRepositoryEndpoint, "https://api.github.com/user/repos" } : new[] { ownerRepositoryEndpoint };
			List<Repo> repositories = new List<Repo>();

			// Repository discovery
			foreach (string repositoryEndpoint in repositoryEndpoints) {
				for (int page = 1; ; page++) {
					using (UnityWebRequest request = UnityWebRequest.Get($"{repositoryEndpoint}?per_page=100&page={page}")) {
						string json = await SendBearerRequestAsync(request, personalAccessToken, cancellationToken);
						List<Repo> batch = JsonConvert.DeserializeObject<List<Repo>>(json);
						repositories.AddRange(batch.Where(repo => string.Equals(repo.owner?.login, owner, StringComparison.OrdinalIgnoreCase)));
						if (batch.Count < 100) break;
					}
				}
			}
			return repositories.GroupBy(repo => repo.name, StringComparer.OrdinalIgnoreCase).Select(group => group.First()).ToList();
		}
        public class Repo
		{
			public string name;
			public string default_branch;
			public string clone_url;
			public RepoOwner owner;
			[JsonProperty("private")] public bool IsPrivate;
		}
		public class RepoOwner
		{
			public string login;
		}

		//GetOrganizationLastCommitHashAsync
		public static async UniTask<string> GetLastCommitHashAsync(string personalAccessToken, string organization, string repoName, string branchName, CancellationToken cancellationToken) {
			string escapedBranchName = UnityWebRequest.EscapeURL(branchName);
			string url = $"https://api.github.com/repos/{organization}/{repoName}/branches/{escapedBranchName}";

			using (UnityWebRequest request = UnityWebRequest.Get(url)) {
				string json = await SendBearerRequestAsync(request, personalAccessToken, cancellationToken);
				JObject obj = JObject.Parse(json);
				string sha = obj["commit"]?["sha"]?.ToString();
				return sha;
			}
		}

		//GetPackageJsonAsync
		public static async UniTask<PackageJson> GetPackageJsonAsync(string personalAccessToken, string organization, string repoName, string branch, string packagePath, CancellationToken cancellationToken) {
			string packageJsonPath = string.IsNullOrWhiteSpace(packagePath) ? "package.json" : $"{packagePath}/package.json";
			string decodedJson = await GetRepositoryFileAsync(personalAccessToken, organization, repoName, branch, packageJsonPath, cancellationToken);
			PackageJson result = JsonConvert.DeserializeObject<PackageJson>(decodedJson);
			if (result == null || string.IsNullOrWhiteSpace(result.name) || string.IsNullOrWhiteSpace(result.version)) throw new InvalidOperationException($"Invalid package name or version in {repoName}/{packageJsonPath}.");
			return result;
		}
		public static async UniTask<UpmInfoJson> GetUpmInfoAsync(string personalAccessToken, string organization, string repoName, string branch, CancellationToken cancellationToken) {
			string decodedJson = await GetRepositoryFileAsync(personalAccessToken, organization, repoName, branch, "parkmin-upm.json", cancellationToken);
			if (decodedJson != null) {
				UpmInfoJson info = JsonConvert.DeserializeObject<UpmInfoJson>(decodedJson);
				if (info == null || info.schemaVersion != 1 || info.packages == null || info.packages.Any(package => package == null)) {
					throw new InvalidOperationException($"Invalid or unsupported parkmin-upm.json in {repoName}.");
				}
				return info;
			}

			// Legacy metadata
			decodedJson = await GetRepositoryFileAsync(personalAccessToken, organization, repoName, branch, "parkmin-dependencies.json", cancellationToken);
			if (decodedJson == null) return null;
			LegacyDependenciesJson legacy = JsonConvert.DeserializeObject<LegacyDependenciesJson>(decodedJson);
			if (legacy == null || legacy.schemaVersion != 1) throw new InvalidOperationException($"Invalid legacy dependency metadata in {repoName}.");
			return new UpmInfoJson {
				schemaVersion = 1,
				packages = new List<UpmPackageJson> {
					new UpmPackageJson {
						packagePath = legacy.packagePath,
						gitDependencies = legacy.gitDependencies,
						nugetDependencies = legacy.nugetDependencies
					}
				}
			};
		}
		public class PackageJson
		{
			public string name;
			public string displayName;
			public string version;
		}
		public class UpmInfoJson
		{
			public int schemaVersion;
			public List<UpmPackageJson> packages;
		}
		public class UpmPackageJson
		{
			public string packagePath;
			public List<GitDependency> gitDependencies = new List<GitDependency>();
			public List<NuGetDependency> nugetDependencies = new List<NuGetDependency>();
		}

		class LegacyDependenciesJson : UpmPackageJson
		{
			public int schemaVersion;
		}

		//Internal
		static async UniTask<string> GetRepositoryFileAsync(
			string personalAccessToken,
			string organization,
			string repoName,
			string branch,
			string filePath,
			CancellationToken cancellationToken
		) {
			string escapedBranch = UnityWebRequest.EscapeURL(branch);
			string normalizedFilePath = filePath.Replace('\\', '/').Trim('/');
			string escapedFilePath = string.Join("/", normalizedFilePath.Split('/').Select(UnityWebRequest.EscapeURL));
			string url = $"https://api.github.com/repos/{organization}/{repoName}/contents/{escapedFilePath}?ref={escapedBranch}";

			using (UnityWebRequest request = UnityWebRequest.Get(url)) {
				string json = await SendBearerRequestAsync(request, personalAccessToken, cancellationToken, true);
				if (string.IsNullOrEmpty(json)) {
					return null;
				}

				JObject obj = JObject.Parse(json);
				string base64 = obj["content"]?.ToString();
				if (string.IsNullOrEmpty(base64)) {
					throw new Exception($"{filePath} not found");
				}

				base64 = base64.Replace("\n", "").Replace("\r", "");
				byte[] bytes = Convert.FromBase64String(base64);
				return Encoding.UTF8.GetString(bytes);
			}
		}
		static async UniTask<string> SendBearerRequestAsync(UnityWebRequest request, string personalAccessToken, CancellationToken cancellationToken, bool allowNotFound = false) {
			if (!string.IsNullOrWhiteSpace(personalAccessToken))
				request.SetRequestHeader("Authorization", "Bearer " + personalAccessToken);
			request.SetRequestHeader("Accept", "application/vnd.github+json");
			request.SetRequestHeader("User-Agent", "ParkMinDev PackageManager");

			try {
				await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
			}
			catch (UnityWebRequestException) when (allowNotFound && request.responseCode == 404) {
				return null;
			}

			if (request.result != UnityWebRequest.Result.Success) {
				throw new Exception(
					"GitHub request failed. " +
					"URL: " + request.url + ", " +
					"HTTP: " + request.responseCode + ", " +
					"Error: " + request.error + ", " +
					"Body: " + request.downloadHandler.text);
			}

			return request.downloadHandler.text;
		}
	}
}
