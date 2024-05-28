using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Editor.EditorTools.Skins
{
	public class SpawnerDebugMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private Transform _sceneDebug;

		private void Start()
		{
			if (Application.isPlaying && _sceneDebug != null)
			{
				Destroy(_sceneDebug.gameObject);
			}
		}

		public async UniTaskVoid HideDebugIcon()
		{
			while (_sceneDebug.childCount > 0)
			{
				DestroyImmediate(_sceneDebug.GetChild(0).gameObject);
				await UniTask.NextFrame();
			}
		}

#if UNITY_EDITOR
		public async UniTaskVoid ShowDebugIcon()
		{
			var component = GetComponentInParent<EntityComponentCollectablePlatformSpawner>();
			if (component == null) component = GetComponentInChildren<EntityComponentCollectablePlatformSpawner>();
			if (component == null) return;
			var toSpawn = component.Prototype.GameId;
			while (_sceneDebug.childCount > 0)
			{
				DestroyImmediate(_sceneDebug.GetChild(0).gameObject);
				await UniTask.NextFrame();
			}
			await Game.Utils.Editor.EditorPrefabUtils.InstantiateGameIdPrefab(toSpawn, _sceneDebug);
		}
#endif
	}
}