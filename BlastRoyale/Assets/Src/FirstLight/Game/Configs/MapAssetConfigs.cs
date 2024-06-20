using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.AssetImporter;
using FirstLight.Game.Infos;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Configs
{

	[Serializable]
	public struct MapAssetConfig
	{
		public AssetReferenceScene Scene;
		public AssetReferenceT<MapAsset> QuantumMap;
	}
	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="MapConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "MapAssetConfigs", menuName = "ScriptableObjects/Configs/MapAssetConfigs")]
	[IgnoreServerSerialization]
	public class MapAssetConfigs : ScriptableObject
	{
		public List<Pair<GameId, MapAssetConfig>> Configs;


		public bool TryGetConfigForMap(GameId mapId, out MapAssetConfig config)
		{
			foreach (var pair in Configs.Where(pair => pair.Key == mapId))
			{
				config = pair.Value;
				return true;
			}

			config = default;
			return false;
		}
	}
}