using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Game.Services.Collection.Handles
{
	public class WeaponSkinCollectionHandler : ICollectionGroupHandler
	{
		private IConfigsProvider _configsProvider;
		private IAssetResolverService _assetResolver;
		private WeaponSkinsConfig Configs => _configsProvider.GetConfig<WeaponSkinsConfig>();

		// Optimized structures
		private HashSet<GameId> _canHandleLookup;
		private Dictionary<GameId, AssetReferenceSprite> _spriteLookup;
		private Dictionary<GameId, AssetReferenceGameObject> _prefabLookup;
		private Dictionary<GameId, WeaponSkinConfigGroup> _groupLookup;

		public WeaponSkinCollectionHandler(IConfigsProvider configsProvider, IAssetResolverService assetResolver)
		{
			_configsProvider = configsProvider;
			_assetResolver = assetResolver;
			_configsProvider.OnConfigVersionChanged += UpdateConfigs;
		}

		private void CheckConfigInitialize()
		{
			if (_canHandleLookup == null)
			{
				UpdateConfigs();
			}
		}

		private void UpdateConfigs()
		{
			try
			{
				_canHandleLookup = Configs.Groups.Select(groups => groups.Value)
					.Select(group => group.Configs)
					.SelectMany(x => x)
					.Select(a => a.SkinId)
					.ToHashSet();


				_spriteLookup = Configs.Groups.Select(groups => groups.Value)
					.Select(group => group.Configs)
					.SelectMany(x => x)
					.ToDictionary(c => c.SkinId, c => c.Sprite);

				_prefabLookup = Configs.Groups.Select(groups => groups.Value)
					.Select(group => group.Configs)
					.SelectMany(x => x)
					.ToDictionary(c => c.SkinId, c => c.Prefab);

				_groupLookup = Configs.Groups
					.SelectMany(keyValuePair => keyValuePair.Value.Configs.Select(config => new {Group = keyValuePair.Value, SkinId = config.SkinId}))
					.ToDictionary(item => item.SkinId, item => item.Group);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}


		public bool CanHandle(GameId id)
		{
			CheckConfigInitialize();
			return _canHandleLookup.Contains(id);
		}

		public async Task<Sprite> LoadCollectionItemSprite(GameId id, bool instantiate = true)
		{
			var assetRef = _spriteLookup[id];
			return await _assetResolver.LoadAssetByReference<Sprite>(assetRef, true, instantiate);
		}


		public async Task<GameObject> LoadCollectionItem3DModel(GameId id, bool menuModel = false, bool instantiate = true)
		{
			CheckConfigInitialize();
			var assetRef = _prefabLookup[id];
			var obj = await _assetResolver.LoadAssetByReference<GameObject>(assetRef, true, instantiate);
			if (!instantiate) return obj;
			// Workaround, somehow the playercharacter monocomponent detaches the first child of the prefab, i tried to fix the code there but i couldn't do it quickly


			var a = obj.AddComponent<RenderersContainerMonoComponent>();
			a.UpdateRenderers();
			var skin = obj.GetComponent<WeaponSkinMonoComponent>();
			var animator = skin.AnimatorController != null ? skin.AnimatorController : _groupLookup[id].DefaultAnimationOverwrite;
			var component = obj.AddComponent<RuntimeAnimatorMonoComponent>();
			component.AnimatorController = animator;

			// Menu model doesn't need dirty anchor hack
			if (!menuModel)
			{
				var container = new GameObject();
				obj.transform.parent = container.transform;
				return container;
			}

			return obj;
		}
	}
}