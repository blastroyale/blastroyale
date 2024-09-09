using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.Collection.Handles;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.Collection
{
	public interface ICollectionService
	{
		bool IsDefaultItem(GameId gameId);

		UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true);
		UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken cancellationToken = default);

		/// <summary>
		/// Find the cosmetic of ids present in the group, 
		/// </summary>
		/// <param name="group"></param>
		/// <param name="returnDefault">If no skin was present for given group, return the default skin</param>
		/// <param name="ids"></param>
		/// <returns></returns>
		ItemData GetCosmeticForGroup(IEnumerable<GameId> cosmeticLoadout, GameIdGroup group, bool returnDefault = true);
	}

	interface ICollectionGroupHandler
	{
		bool CanHandle(ItemData item);
		UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken cancellationToken = default);
		UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true);
	}

	public class CollectionService : ICollectionService
	{
		// Used if the player doesn't have any skin equipped, this should never be the case is a fallback to always render something
		private static readonly Dictionary<GameIdGroup, GameId> DefaultItems = new ()
		{
			{GameIdGroup.PlayerSkin, GameId.PlayerSkinBrandMale},
			{GameIdGroup.MeleeSkin, GameId.MeleeSkinDefault},
			{GameIdGroup.Glider, GameId.Turbine},
			{GameIdGroup.DeathMarker, GameId.Demon},
			{GameIdGroup.Footprint, GameId.FootprintDot},
			{GameIdGroup.ProfilePicture, GameId.AvatarBrandmale},
		};

		private IAssetResolverService _assetResolver;
		private IConfigsProvider _configsProvider;
		private IGameDataProvider _dataProvider;
		private IGameCommandService _commandService;
		private ICollectionGroupHandler[] _handlers;

		public CollectionService(IAssetResolverService assetResolver,
								 IConfigsProvider configsProvider,
								 IMessageBrokerService messageBrokerService,
								 IGameDataProvider dataProvider,
								 IGameCommandService commandService)
		{
			_assetResolver = assetResolver;
			_configsProvider = configsProvider;
			_dataProvider = dataProvider;
			_commandService = commandService;
			_handlers = new ICollectionGroupHandler[]
			{
				new ProfilePictureHandler(_configsProvider, _assetResolver),
				new CharacterSkinGroupHandler(_configsProvider, _assetResolver),
				new WeaponSkinCollectionHandler(_configsProvider, _assetResolver),
				new FlagGroupHandler(_configsProvider, _assetResolver)
			};
		}

		public bool IsDefaultItem(GameId gameId)
		{
			return DefaultItems.ContainsValue(gameId);
		}

		public ItemData GetCosmeticForGroup(IEnumerable<GameId> cosmeticLoadout, GameIdGroup group, bool returnDefault = true)
		{
			if (cosmeticLoadout != null)
			{
				foreach (var gameId in cosmeticLoadout)
				{
					if (gameId.IsInGroup(group))
					{
						return ItemFactory.Collection(gameId);
					}
				}
			}

			return ItemFactory.Collection(returnDefault ? DefaultItems[group] : default);
		}

		public async UniTask<Sprite> LoadCollectionItemSprite(ItemData item, bool instantiate = true, CancellationToken cancellationToken = default)
		{
			foreach (var handler in _handlers)
			{
				if (handler.CanHandle(item))
				{
					return await handler.LoadCollectionItemSprite(item, instantiate, cancellationToken);
				}
			}

			return await _assetResolver.RequestAsset<GameId, Sprite>(item.Id, true, instantiate);
		}

		public async UniTask<GameObject> LoadCollectionItem3DModel(ItemData item, bool menuModel = false, bool instantiate = true)
		{
			foreach (var handler in _handlers)
			{
				if (handler.CanHandle(item))
				{
					return await handler.LoadCollectionItem3DModel(item, menuModel, instantiate);
				}
			}

			return await _assetResolver.RequestAsset<GameId, GameObject>(item.Id, true, instantiate);
		}
	}
}