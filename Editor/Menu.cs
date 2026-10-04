using UnityEditor;
using UnityEngine;

namespace ParkMinDev.UPM.PackageManager.Editor
{
	internal class Menu
	{
		[MenuItem("ParkMinDev/Package Manager")]
		static void Execute() {
			PackageManagerWindow window = EditorWindow.GetWindow<PackageManagerWindow>();
			window.titleContent = new GUIContent("ParkMinDev Package Manager");
			window.minSize = new Vector2(500, 300);
			window.maxSize = new Vector2(500, 10000);
		}
	}
}
