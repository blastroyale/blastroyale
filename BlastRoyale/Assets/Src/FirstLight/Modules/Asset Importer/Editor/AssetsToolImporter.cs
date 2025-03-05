using System;
using System.Collections.Generic;
using FirstLight.AssetImporter;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.AssetImporter
{
	/// <summary>
	/// Customizes the visual inspector of the importing tool <seealso cref="AssetsImporter"/>
	/// </summary>
	[CustomEditor(typeof(AssetsImporter))]
	public class AssetsToolImporter : Editor
	{
		private static List<ImportData> _importers;
		
		[MenuItem("Tools/Assets Importer/Import Assets Data")]
		private static void ImportAllGoogleSheetData()
		{
			_importers = GetAllImporters();
			
			foreach (var importer in _importers)
			{
				importer.Importer.Import();
			}
			
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		
		/// <inheritdoc />
		public override void OnInspectorGUI()
		{
			if (_importers == null)
			{
				// Not yet initialized. Will initialized as soon has all scripts finish compiling
				return;
			}
			
			var typeCheck = typeof(IScriptableObjectImporter);
			var tool = (AssetsImporter) target;
			
			if (GUILayout.Button("Import Assets Data"))
			{
				foreach (var importer in _importers)
				{
					importer.Importer.Import();
				}
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			foreach (var importer in _importers)
			{
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(importer.Type.Name);
				
				if (GUILayout.Button("Update Path"))
				{
					var scriptableObject = GetScriptableObject(importer);

					var path = EditorUtility.OpenFolderPanel("Select Folder Path", scriptableObject.AssetsFolderPath,
					                                         "");
					scriptableObject.AssetsFolderPath = path.Substring(path.IndexOf("Assets/", StringComparison.Ordinal));
					
					importer.Importer.Import();
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				
				if (GUILayout.Button("Import"))
				{
					importer.Importer.Import();
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				if(typeCheck.IsAssignableFrom(importer.Type) && GUILayout.Button("Select Object"))
				{
					Selection.activeObject = GetScriptableObject(importer);
				}
				EditorGUILayout.EndHorizontal();
			}
		}
		
		public static List<ImportData> GetAllImporters()
		{
			if (_importers != null) return _importers;
			var importerInterface = typeof(IAssetConfigsImporter);
			var importers = new List<ImportData>();
			
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (!type.IsAbstract && !type.IsInterface && importerInterface.IsAssignableFrom(type))
					{
						importers.Add(new ImportData
						{
							Type = type,
							Importer = Activator.CreateInstance(type) as IAssetConfigsImporter
						});
					}
				}
			}

			return importers;
		}

		private static AssetConfigsScriptableObject GetScriptableObject(ImportData data)
		{
			var scriptableObjectType = data.Importer.ScriptableObjectType;
			var assets = AssetDatabase.FindAssets($"t:{scriptableObjectType?.Name}");
			var scriptableObject = assets.Length > 0 ? 
				                       AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assets[0]), scriptableObjectType) :
				                       CreateInstance(scriptableObjectType);

			if (assets.Length == 0 && scriptableObjectType != null)
			{
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{scriptableObjectType.Name}.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			return scriptableObject as AssetConfigsScriptableObject;
		}

		public struct ImportData
		{
			public Type Type;
			public IAssetConfigsImporter Importer;
		}
	}
}