using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FirstLight.GoogleSheetImporter;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Networking;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.GoogleSheetImporter
{
	/// <summary>
	/// Customizes the visual inspector of the google sheet importing tool <seealso cref="GoogleSheetImporter"/>
	/// </summary>
	[CustomEditor(typeof(GoogleSheetImporter))]
	public class GoogleSheetToolImporter : Editor
	{
		private static List<ImportData> _importers;
		
		private void Awake()
		{
			_importers = GetAllImporters();
		}
		
		[MenuItem("Tools/GoogleSheet Importer/Import Google Sheet Data")]
		private static void ImportAllGoogleSheetData()
		{
			_importers = GetAllImporters();
			
			foreach (var importer in _importers)
			{
				ImportSheetAsync(importer, "");
			}
			
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		
		[DidReloadScripts]
		public static void OnCompileScripts()
		{
			_importers = GetAllImporters();
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
			var tool = (GoogleSheetImporter) target;
			var guiContent = new GUIContent("Spreadsheet ID (optional)", 
				"(Optional) Put the Google Spreadsheet Id to replace from the one set in SheetImporter. " +
				"Will use the one set in the SheetImporter by default if not set or empty. " +
				"Use this option if you duplicate the Google Sheet file for testing purposes.");
			
			tool.ReplaceSpreadsheetId = EditorGUILayout.TextField(guiContent, tool.ReplaceSpreadsheetId);
			
			if (GUILayout.Button("Import All Sheets"))
			{
				foreach (var importer in _importers)
				{
					ImportSheetAsync(importer, tool.ReplaceSpreadsheetId);
				}
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
			
			EditorGUILayout.Space();

			foreach (var importer in _importers)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(importer.Type.Name);
				if (GUILayout.Button("Import"))
				{
					ImportSheetAsync(importer, tool.ReplaceSpreadsheetId);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}
				if(typeCheck.IsAssignableFrom(importer.Type) && GUILayout.Button("Select Object"))
				{
					var scriptableObjectType = (importer.Importer as IScriptableObjectImporter)?.ScriptableObjectType;
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

					Selection.activeObject = scriptableObject;
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		private static List<ImportData> GetAllImporters()
		{
			var importerInterface = typeof(IGoogleSheetConfigsImporter);
			var importerAttribute = typeof(GoogleSheetImportOrderAttribute);
			var importers = new List<ImportData>();
			
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				foreach (var type in assembly.GetTypes())
				{
					if (!type.IsAbstract && !type.IsInterface && importerInterface.IsAssignableFrom(type))
					{
						var importOrder = Int32.MaxValue;
						var attribute = type.GetCustomAttribute(importerAttribute);
						if (attribute != null)
						{
							importOrder = ((GoogleSheetImportOrderAttribute) attribute).ImportOrder;
						}
						
						importers.Add(new ImportData
						{
							Type = type,
							Importer = Activator.CreateInstance(type) as IGoogleSheetConfigsImporter,
							ImportOrder = importOrder
						});
					}
				}
			}

			importers.Sort((elem1, elem2) =>
			{
				if (elem1.ImportOrder == elem2.ImportOrder)
				{
					return string.Compare(elem1.Importer.GetType().UnderlyingSystemType.Name, 
					                      elem2.Importer.GetType().UnderlyingSystemType.Name, 
					                      StringComparison.Ordinal);
				}
				
				return elem1.ImportOrder.CompareTo(elem2.ImportOrder);
			});
			return importers;
		}
		
		private static async void ImportSheetAsync(ImportData data, string spreadsheetId)
		{
			var indexStart = data.Importer.GoogleSheetUrl.IndexOf("/d/", StringComparison.Ordinal) + 3;
			var indexCount = data.Importer.GoogleSheetUrl.IndexOf("/edit#", StringComparison.Ordinal) - indexStart;
			var url = data.Importer.GoogleSheetUrl.Replace("edit#", "export?format=csv&");
			var finalUrl = string.IsNullOrWhiteSpace(spreadsheetId) ? url : url.Remove(indexStart, indexCount).Insert(indexStart, spreadsheetId);
			var request = UnityWebRequest.Get(finalUrl);

			await AsyncOperation(request.SendWebRequest());

			if (request.result != UnityWebRequest.Result.Success)
			{
				throw new Exception(request.error);
			}

			var values = CsvParser.ConvertCsv(request.downloadHandler.text);

			if (values.Count == 0)
			{
				Debug.LogWarning($"The return sheet was not in CSV format:\n{request.downloadHandler.text}");
			}
			else
			{
				data.Importer.Import(values);
			}
			
			Debug.Log($"Finished importing google sheet data from {data.Type.Name}");
		}

		private static async Task AsyncOperation(AsyncOperation operation)
		{
			while (!operation.isDone)
			{
				await Task.Yield();
			}
		}

		private struct ImportData
		{
			public Type Type;
			public IGoogleSheetConfigsImporter Importer;
			public int ImportOrder;
		}
	}
}