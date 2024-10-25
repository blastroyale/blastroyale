using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.AssetImporter;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MapAssetConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MapAssetConfigs", menuName = "ScriptableObjects/Map/MapAssetConfigs")]
	[IgnoreServerSerialization]
	public class MapAssetConfigIndex : ScriptableObject
	{
		[Serializable]
		public class Entry
		{
			public GameId Id;
			public AssetReferenceT<MapAssetConfig> Ref;
		}

		[TableList] public List<Entry> Configs;

		/// <summary>
		/// Return a new asset reference for the map config, so you can loadaddressable directly on it
		/// </summary>
		/// <param name="map"></param>
		/// <param name="entry"></param>
		/// <returns></returns>
		public bool TryGetConfigForMap(GameId map, out AssetReferenceT<MapAssetConfig> entry)
		{
			foreach (var config in Configs)
			{
				if (config.Id == map)
				{
					entry = new AssetReferenceT<MapAssetConfig>(config.Ref.AssetGUID);
					return true;
				}
			}

			entry = null;
			return false;
		}

#if UNITY_EDITOR
		[Button]
		public void Fill()
		{
			var assetPaths = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(MapAssetConfig)))
				.Select(AssetDatabase.GUIDToAssetPath);

			foreach (var assetpath in assetPaths)
			{
				var id = FindId(assetpath);
				if (id != GameId.Random)
				{
					if (Configs.Any(entry => entry.Id == id))
					{
						continue;
					}

					Configs.Add(new Entry()
					{
						Id = id,
						Ref = new AssetReferenceT<MapAssetConfig>(AssetDatabase.AssetPathToGUID(assetpath))
					});
				}
			}

			return;
		}

		public GameId FindId(string path)
		{
			foreach (var gameId in GameIdGroup.Map.GetIds())
			{
				var mapid = gameId.ToString().ToLowerInvariant();
				if (path.ToLowerInvariant().Contains(mapid))
				{
					return gameId;
				}
			}

			return GameId.Random;
		}
#endif
	}
}