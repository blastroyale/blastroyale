using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Editor.EditorTools.ArtTools
{
	public class ArtPrefab
	{
		public GameObject LoadedPrefab;
		public string Path;

		public void SaveChanges()
		{
			PrefabUtility.RecordPrefabInstancePropertyModifications(LoadedPrefab);
			PrefabUtility.SaveAsPrefabAsset(LoadedPrefab, Path, out var worked);
			if(!worked) Debug.LogError("Error saving prefab "+Path);
			PrefabUtility.UnloadPrefabContents(LoadedPrefab);
		}
	}
	
	public class ArtUtils
	{
		public static List<ArtPrefab> GetPrefabs(FolderSelector folder)
		{
			return Directory.GetFiles(folder.Path, "*.prefab", SearchOption.AllDirectories)
				.Select(LoadPrefab)
				.ToList();
		}

		private static ArtPrefab LoadPrefab(string path)
		{
			return new ArtPrefab()
			{
				Path = path,
				LoadedPrefab = PrefabUtility.LoadPrefabContents(path)
			};
		}
	}
}