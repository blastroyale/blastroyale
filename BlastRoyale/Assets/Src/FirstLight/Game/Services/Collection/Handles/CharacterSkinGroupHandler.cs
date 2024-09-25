using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Collection.Handles
{
	public class CharacterSkinGroupHandler : ICollectionGroupHandler
	{
		private IConfigsProvider _configsProvider;
		private IAssetResolverService _assetResolver;
		private CharacterSkinsConfig SkinContainer => _configsProvider.GetConfig<CharacterSkinsConfig>();

		public CharacterSkinGroupHandler(IConfigsProvider configsProvider, IAssetResolverService assetResolver)
		{
			_configsProvider = configsProvider;
			_assetResolver = assetResolver;
		}

		public bool CanHandle(ItemData item)
		{
			return SkinContainer.Skins.Any(s => s.GameId == item.Id);
		}

		public async UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken cancellationToken = default)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == item.Id);
			return await _assetResolver.LoadAssetByReference<Sprite>(skin.Sprite, true, instantiate, cancellationToken: cancellationToken);
		}

		public async UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			var skin = SkinContainer.Skins.FirstOrDefault(s => s.GameId == item.Id);
			var obj = await _assetResolver.LoadAssetByReference<GameObject>(skin.Prefab, true, instantiate);
			return obj;
		}
	}
}