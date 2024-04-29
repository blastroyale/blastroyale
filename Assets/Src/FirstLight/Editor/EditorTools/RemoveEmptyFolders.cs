using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools
{
	/// <summary>
	/// Remove empty folders automatically.
	/// </summary>
	public class RemoveEmptyFolders
	{
		/// <summary>
		/// Use this flag to simulate a run, before really deleting any folders.
		/// </summary>
		private static bool dryRun = false;

		[MenuItem("Tools/Remove empty folders")]
		private static void RemoveEmptyFoldersMenuItem()
		{
			var index = Application.dataPath.IndexOf("/Assets", StringComparison.Ordinal);
			var projectSubfolders = Directory.GetDirectories(Application.dataPath, "*", SearchOption.AllDirectories);

			// Create a list of all the empty subfolders under Assets.
			var emptyFolders = projectSubfolders.Where(IsEmptyRecursive).ToArray();

			foreach (var folder in emptyFolders)
			{
				// Verify that the folder exists (may have been already removed).
				if (Directory.Exists(folder))
				{
					var assetPath = folder.Substring(index + 1);
					Debug.Log($"Deleting: {assetPath}");

					if (!dryRun)
					{
						// Delete
						AssetDatabase.DeleteAsset(assetPath);
					}
				}
			}

			// Refresh the asset database once we're done.
			AssetDatabase.Refresh();
		}

		/// <summary>
		/// A helper method for determining if a folder is empty or not.
		/// </summary>
		private static bool IsEmptyRecursive(string path)
		{
			// A folder is empty if it (and all its subdirs) have no files (ignore .meta files)
			return Directory.GetFiles(path).Length == 0 || !Directory.GetFiles(path).Select(file => !file.EndsWith(".meta")).Any()
				&& Directory.GetDirectories(path, string.Empty, SearchOption.AllDirectories).All(IsEmptyRecursive);
		}
	}
}