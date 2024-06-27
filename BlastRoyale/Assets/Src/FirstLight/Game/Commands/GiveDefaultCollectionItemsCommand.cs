using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Gives the player new skins
	/// </summary>
	public struct GiveDefaultCollectionItemsCommand : IGameCommand
	{
		private static readonly GameId[] DiscordModItems = {
			GameId.Avatar1,
			GameId.MeleeSkinCactus,
			GameId.MeleeSkinPowerPan,
		};

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Initialization;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			GiveDefaultItems(ctx);
			SyncItemsBasedOnFlag(ctx, PlayerFlags.DiscordMod, DiscordModItems, "discordmod");
			SyncItemsBasedOnFlag(ctx, PlayerFlags.FLGOfficial, GetAllCollectionItems(ctx), "flg");
			return UniTask.CompletedTask;
		}

		private IEnumerable<GameId> GetAllCollectionItems(CommandExecutionContext ctx)
		{
			return ctx.Logic.CollectionLogic().GetCollectionsCategories().SelectMany(c => c.Id.GetIds().ToList().Where(id => !id.IsInGroup(GameIdGroup.GenericCollectionItem)));
		}

		private void GiveDefaultItems(CommandExecutionContext ctx)
		{
			foreach (var category in ctx.Logic.CollectionLogic().DefaultCollectionItems)
			{
				foreach (var collectible in category.Value)
				{
					if (ctx.Logic.CollectionLogic().IsItemOwned(collectible))
					{
						continue;
					}

					ctx.Logic.CollectionLogic().UnlockCollectionItem(collectible);
					ctx.Services.MessageBrokerService().Publish(new CollectionItemUnlockedMessage()
					{
						Source = CollectionUnlockSource.DefaultItem,
						EquippedItem = collectible
					});
				}
			}
		}

		private void SyncItemsBasedOnFlag(CommandExecutionContext ctx, PlayerFlags flag, IEnumerable<GameId> itemsToGive, string itemtag)
		{
			var hasFlag = ctx.Logic.PlayerLogic().Flags.HasFlag(flag);

			if (hasFlag)
			{
				GiveItemsBasedOnTag(ctx, itemsToGive, itemtag);
			}
			else
			{
				RemoveItemsBasedOnTag(ctx, itemtag);
			}
		}

		private static void RemoveItemsBasedOnTag(CommandExecutionContext ctx, string itemtag)
		{
			foreach (var collectionsCategory in ctx.Logic.CollectionLogic().GetCollectionsCategories())
			{
				var owned = ctx.Logic.CollectionLogic().GetOwnedCollection(collectionsCategory);
				var toRemove = new List<ItemData>();
				foreach (var itemData in owned)
				{
					if (itemData.TryGetMetadata<CollectionMetadata>(out var collectionMetadata))
					{
						if (collectionMetadata.TryGetTrait("origin", out var value) && value == itemtag)
						{
							toRemove.Add(itemData);
						}
					}
				}

				foreach (var itemData in toRemove)
				{
					ctx.Logic.CollectionLogic().RemoveFromPlayer(itemData);
				}
			}
		}

		private void GiveItemsBasedOnTag(CommandExecutionContext ctx, IEnumerable<GameId> itemsToGive, string itemtag)
		{
			foreach (var id in itemsToGive)
			{
				// Check if the mod already have the item, if so we don't give it again
				if (ctx.Logic.CollectionLogic().IsItemOwned(ItemFactory.Collection(id)))
				{
					continue;
				}

				var collectible = ItemFactory.Collection(id, new CollectionTrait("origin", itemtag));
				// If we already gave the item for the mod
				if (ctx.Logic.CollectionLogic().IsItemOwned(collectible))
				{
					continue;
				}

				ctx.Logic.CollectionLogic().UnlockCollectionItem(collectible);
				ctx.Services.MessageBrokerService().Publish(new CollectionItemUnlockedMessage()
				{
					EquippedItem = collectible
				});
			}
		}
	}
}