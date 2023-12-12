using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Utils.Editor
{
#if UNITY_EDITOR
	public class EditorPrefabUtils
	{
		private static Dictionary<GameId, AssetReference> _editorAssetReferences;
		
		public static async UniTask<GameObject> InstantiateGameIdPrefab(GameId id, Transform parent)
		{
			if (_editorAssetReferences == null)
			{
				var asset = await Addressables.LoadAssetAsync<MatchAssetConfigs>(AddressableId.Configs_AdventureAssetConfigs.GetConfig().Address);
				_editorAssetReferences = asset.Configs.ToDictionary(k => k.Key, k => k.Value);
			}
			if (_editorAssetReferences.TryGetValue(id, out var toSpawnAsset))
			{
				return await toSpawnAsset.InstantiateAsync(parent);
			}
			return null;
		}
	}
#endif
}