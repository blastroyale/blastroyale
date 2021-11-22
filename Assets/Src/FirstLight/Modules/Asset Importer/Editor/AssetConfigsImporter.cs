using System;
using System.Collections.Generic;
using FirstLight;
using FirstLight.AssetImporter;
using Quantum;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.AssetImporter
{
	/// <summary>
	/// Asset importers allow to create custom in editor time processors of specific assets in the project.
	/// They import their correspondent asset type and map it inside a container with their respective id.
	/// </summary>
	public interface IAssetConfigsImporter : IScriptableObjectImporter
	{
		/// <summary>
		/// Imports all assets belonging to this asset config scope into a defined container
		/// </summary>
		void Import();
	}

	/// <inheritdoc cref="IAssetConfigsImporter"/>
	/// <remarks>
	/// It will save the asset data into a scriptable object of <typeparamref name="TScriptableObject"/> type
	/// </remarks>
	public abstract class AssetsConfigsImporter<TId, TAsset, TScriptableObject> : IAssetConfigsImporter
		where TId : struct
		where TScriptableObject : AssetConfigsScriptableObject<TId, TAsset>
	{
		/// <inheritdoc />
		public Type ScriptableObjectType => typeof(TScriptableObject);

		/// <inheritdoc />
		public void Import()
		{
			var type = typeof(TScriptableObject);
			var soAssets = AssetDatabase.FindAssets($"t:{type.Name}");
			var scriptableObject = soAssets.Length > 0
				                       ? AssetDatabase.LoadAssetAtPath<TScriptableObject>(AssetDatabase.GUIDToAssetPath(soAssets[0]))
				                       : ScriptableObject.CreateInstance<TScriptableObject>();

			if (soAssets.Length == 0)
			{
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{type.Name}.asset");
			}

			var assetGuids = new List<string>(AssetDatabase.FindAssets($"t:{typeof(TAsset).Name}", new[]
			{
				scriptableObject.AssetsFolderPath
			}));
			var assetsPaths = assetGuids.ConvertAll(a => AssetDatabase.GUIDToAssetPath(a));

			scriptableObject.Configs.Clear();
			scriptableObject.Configs.AddRange(OnImportIds(scriptableObject, assetGuids, assetsPaths));
			OnImportComplete(scriptableObject);
			EditorUtility.SetDirty(scriptableObject);
			
			Debug.Log($"Finished importing asset data of '{typeof(TAsset).Name}' type with '{typeof(TId).Name}' as identifier.\n" +
			          $"To: '{typeof(TScriptableObject).Name}' - From '{scriptableObject.AssetsFolderPath}' ");
		}
		
		protected virtual TId[] GetIds()
		{
			return Enum.GetValues(typeof(TId)) as TId[];
		}

		protected virtual string IdPattern(TId id)
		{
			return id.ToString();
		}

		protected virtual List<Pair<TId, AssetReference>> OnImportIds(TScriptableObject scriptableObject,
		                                                              List<string> assetGuids, 
		                                                              List<string> assetsPaths)
		{
			var ids = GetIds();
			var list = new List<Pair<TId, AssetReference>>(ids.Length);

			for (var i = 0; i < ids.Length; i++)
			{
				var indexOf = IndexOfId(IdPattern(ids[i]), assetsPaths);

				if (indexOf < 0)
				{
					continue;
				}

				list.Add(new Pair<TId, AssetReference>(ids[i], new AssetReference(assetGuids[indexOf])));
			}

			return list;
		}

		protected int IndexOfId(string id, IList<string> assetsPath)
		{
			for (var i = 0; i < assetsPath.Count; i++)
			{
				if (assetsPath[i].Contains($"/{id}."))
				{
					return i;
				}
			}

			return -1;
		}

		protected virtual void OnImportComplete(TScriptableObject scriptableObject) { }
	}
}