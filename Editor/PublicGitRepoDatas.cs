using System.Collections.Generic;
using UnityEngine;

namespace ParkMinDev.UPM.PackageManager.Editor
{
	//[CreateAssetMenu(fileName = "PublilcGitRepoData", menuName = "PublilcGitRepoData", order = 0)]
	[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ParkMinPackages.PackageManager.Editor", sourceAssembly: "ParkMinPackages.PackageManager.Editor", sourceClassName: "PublicGitRepoDatas")]
	internal class PublicGitRepoDatas : ScriptableObject
	{
		public IReadOnlyList<PublicGitRepoData> Value
		{
			get { return _value; }
		}

		[SerializeField] List<PublicGitRepoData> _value = new List<PublicGitRepoData>();
	}
}
