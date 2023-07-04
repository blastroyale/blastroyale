using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Gives the player new skins
	/// </summary>
	public struct GetNewSkinsCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		private bool TryGiveUpdatedItem(CommandExecutionContext ctx, GameId oldItem, GameId newItem)
		{
			var newCollectionItem = new CollectionItem(newItem);
			if (ctx.Logic.CollectionLogic().IsItemOwned(new (oldItem)) && !ctx.Logic.CollectionLogic().IsItemOwned(newCollectionItem))
			{
				ctx.Logic.CollectionLogic().UnlockCollectionItem(newCollectionItem);
				ctx.Services.MessageBrokerService().Publish(new CollectionItemUnlockedMessage()
				{
					Source = CollectionUnlockSource.ServerGift,
					EquippedItem = newCollectionItem
				});
				return true;
			}
			return false;
		}
		
		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			TryGiveUpdatedItem(ctx, GameId.Male01Avatar, GameId.MalePunk);
			TryGiveUpdatedItem(ctx, GameId.Male02Avatar, GameId.MaleSuperstar);
			TryGiveUpdatedItem(ctx, GameId.Female01Avatar, GameId.FemalePunk);
			TryGiveUpdatedItem(ctx, GameId.Female02Avatar, GameId.FemaleSuperstar);
		}
	}
}