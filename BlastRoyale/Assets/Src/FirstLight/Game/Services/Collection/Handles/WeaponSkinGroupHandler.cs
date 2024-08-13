using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs.Collection;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.MonoComponent;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Collection.Handles
{
	public class WeaponSkinCollectionHandler : ICollectionGroupHandler
	{
		private readonly IConfigsProvider _configsProvider;
		private readonly IAssetResolverService _assetResolver;

		private Dictionary<GameId, WeaponSkinConfigEntry> _meleeWeapons;

		public WeaponSkinCollectionHandler(IConfigsProvider configsProvider, IAssetResolverService assetResolver)
		{
			_configsProvider = configsProvider;
			_assetResolver = assetResolver;
			_configsProvider.OnConfigVersionChanged += UpdateConfigs;
		}

		private void CheckConfigInitialize()
		{
			if (_meleeWeapons == null)
			{
				UpdateConfigs();
			}
		}

		private void UpdateConfigs()
		{
			_meleeWeapons = _configsProvider.GetConfig<WeaponSkinsConfig>().MeleeWeapons.AsDictionary();
		}

		public bool CanHandle(ItemData item)
		{
			CheckConfigInitialize();
			return _meleeWeapons.ContainsKey(item.Id);
		}

		public async UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken cancellationToken = default)
		{
			var assetRef = _meleeWeapons[item.Id].Sprite;
			return await _assetResolver.LoadAssetByReference<Sprite>(assetRef, true, instantiate, cancellationToken: cancellationToken);
		}

		public async UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			CheckConfigInitialize();
			var assetRef = _meleeWeapons[item.Id].Prefab;
			var obj = await _assetResolver.LoadAssetByReference<GameObject>(assetRef, true, instantiate);
			if (!instantiate) return obj;
			// Workaround, somehow the playercharacter monocomponent detaches the first child of the prefab, i tried to fix the code there but i couldn't do it quickly

			var a = obj.AddComponent<RenderersContainerMonoComponent>();
			a.UpdateRenderers();

			return obj;
		}
	}
}