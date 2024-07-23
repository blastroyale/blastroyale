using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.Data.DataTypes;
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

		public bool CanHandle(ItemData item)
		{
			CheckConfigInitialize();
			return _canHandleLookup.Contains(item.Id);
		}

		public async UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken cancellationToken = default)
		{
			var assetRef = _spriteLookup[item.Id];
			return await _assetResolver.LoadAssetByReference<Sprite>(assetRef, true, instantiate, cancellationToken: cancellationToken);
		}

		public async UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			CheckConfigInitialize();
			var assetRef = _prefabLookup[item.Id];
			var obj = await _assetResolver.LoadAssetByReference<GameObject>(assetRef, true, instantiate);
			if (!instantiate) return obj;
			// Workaround, somehow the playercharacter monocomponent detaches the first child of the prefab, i tried to fix the code there but i couldn't do it quickly

			var a = obj.AddComponent<RenderersContainerMonoComponent>();
			a.UpdateRenderers();

			return obj;
		}
	}
}