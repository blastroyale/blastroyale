using System;
using System.Collections.Generic;
using FirstLight;
using FirstLight.GoogleSheetImporter;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.GoogleSheetImporter
{
	/// <summary>
	/// Generic implementation of an importer to load multiple configs into one Scriptable object.
	/// Implement this interface to import a single Google Sheet.
	/// All the process is done in Editor time.
	/// </summary>
	public interface IGoogleSheetConfigsImporter
	{
		/// <summary>
		/// The complete GoogleSheet Url
		/// </summary>
		string GoogleSheetUrl { get; }
		
		/// <summary>
		/// Imports the <paramref name="data"/> that was processed in <seealso cref="CsvParser.ConvertCsv"/> into the game
		/// </summary>
		// ReSharper disable once ParameterTypeCanBeEnumerable.Global
		void Import(List<Dictionary<string, string>> data);
	}

	/// <inheritdoc cref="IGoogleSheetConfigsImporter"/>
	/// <remarks>
	/// It will save the imported google sheet into a scriptable object of <typeparamref name="TScriptableObject"/> type
	/// </remarks>
	public abstract class GoogleSheetScriptableObjectImportContainer<TScriptableObject> : 
		IScriptableObjectImporter, IGoogleSheetConfigsImporter where TScriptableObject : ScriptableObject
	{
		/// <inheritdoc />
		public abstract string GoogleSheetUrl { get; }

		/// <inheritdoc />
		public Type ScriptableObjectType => typeof(TScriptableObject);

		/// <inheritdoc />
		public void Import(List<Dictionary<string, string>> data)
		{
			var type = typeof(TScriptableObject);
			var assets = AssetDatabase.FindAssets($"t:{type.Name}");
			var scriptableObject = assets.Length > 0
				                       ? AssetDatabase.LoadAssetAtPath<TScriptableObject>(AssetDatabase.GUIDToAssetPath(assets[0]))
				                       : ScriptableObject.CreateInstance<TScriptableObject>();

			if (assets.Length == 0)
			{
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{type.Name}.asset");
			}

			OnImport(scriptableObject, data);

			EditorUtility.SetDirty(scriptableObject);
			AssetDatabase.SaveAssetIfDirty(scriptableObject);
			OnImportComplete(scriptableObject);
		}
		
		protected abstract void OnImport(TScriptableObject scriptableObject, List<Dictionary<string, string>> data);

		protected virtual void OnImportComplete(TScriptableObject scriptableObject) { }
	}
	
	/// <inheritdoc cref="IGoogleSheetConfigsImporter"/>
	/// <remarks>
	/// It will import 1 row per data entry. This means each row will represent 1 <typeparamref name="TConfig"/> entry
	/// and import multiple <typeparamref name="TConfig"/>
	/// </remarks>
	public abstract class GoogleSheetConfigsImporter<TConfig, TScriptableObject> : 
		GoogleSheetScriptableObjectImportContainer<TScriptableObject>
		where TScriptableObject : ScriptableObject, IConfigsContainer<TConfig>
	{
		protected override void OnImport(TScriptableObject scriptableObject, List<Dictionary<string, string>> data)
		{
			var configs = new List<TConfig>();
			
			foreach (var row in data)
			{
				configs.Add(Deserialize(row));
			}

			scriptableObject.Configs = configs;
		}

		protected virtual TConfig Deserialize(Dictionary<string, string> data)
		{
			return CsvParser.DeserializeTo<TConfig>(data);
		}
	}
	
	/// <inheritdoc cref="IGoogleSheetConfigsImporter"/>
	/// <remarks>
	/// It will import 1 entire sheet into one single<typeparamref name="TConfig"/>. This means each row will match
	/// a different field of the <typeparamref name="TConfig"/> represented by a Key/Value pair.
	/// </remarks>
	public abstract class GoogleSheetSingleConfigImporter<TConfig, TScriptableObject>  : 
		GoogleSheetScriptableObjectImportContainer<TScriptableObject>
		where TScriptableObject : ScriptableObject, ISingleConfigContainer<TConfig>
	{
		protected override void OnImport(TScriptableObject scriptableObject, List<Dictionary<string, string>> data)
		{
			scriptableObject.Config = Deserialize(data);
		}
		
		protected abstract TConfig Deserialize(List<Dictionary<string, string>> data);
	}

	/// <inheritdoc cref="GoogleSheetSingleConfigImporter{TConfig,TScriptableObject}"/>
	/// <remarks>
	/// This works the same way as <see cref="GoogleSheetSingleConfigImporter{TConfig,TScriptableObject}"/> but supports
	/// importing lists within the config.
	/// </remarks>
	public abstract class GoogleSheetSingleConfigSubListImporter<TConfig, TScriptableObject> :
		GoogleSheetSingleConfigImporter<TConfig, TScriptableObject>
		where TConfig : struct
		where TScriptableObject : ScriptableObject, ISingleConfigContainer<TConfig>
	{
		protected override TConfig Deserialize(List<Dictionary<string, string>> data)
		{
			var config = new TConfig() as object;
			var type = typeof(TConfig);

			for (var i = 0; i < data.Count; i++)
			{
				var row = data[i];
				var fieldName = row["Key"];
				var isSubList = fieldName.EndsWith(CsvParser.SUB_LIST_SUFFIX);
				if (isSubList)
				{
					fieldName = fieldName.Replace(CsvParser.SUB_LIST_SUFFIX, "");
				}

				var field = type.GetField(fieldName);

				if (field == null)
				{
					continue;
				}

				object value;

				if (isSubList)
				{
					value = CsvParser.DeserializeSubList(data, i, field.FieldType, field.Name, GetDeserializers());
				}
				else
				{
					value = CsvParser.DeserializeObject(row["Value"], field.FieldType, GetDeserializers());
				}


				field.SetValue(config, value);
			}

			return (TConfig) config;
		}
		
		protected abstract Func<string, Type, object>[] GetDeserializers();
	}
}