using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
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
			var item = ItemFactory.Collection(newItem);
			if (ctx.Logic.CollectionLogic().IsItemOwned(ItemFactory.Collection(oldItem)) && !ctx.Logic.CollectionLogic().IsItemOwned(item))
			{
				ctx.Logic.CollectionLogic().UnlockCollectionItem(item);
				ctx.Services.MessageBrokerService().Publish(new CollectionItemUnlockedMessage()
				{
					Source = CollectionUnlockSource.ServerGift,
					EquippedItem = item
				});
				return true;
			}
			return false;
		}
		
		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			TryGiveUpdatedItem(ctx, GameId.Male01Avatar, GameId.MaleAssassin);
			TryGiveUpdatedItem(ctx, GameId.Male02Avatar, GameId.MaleSuperstar);
			TryGiveUpdatedItem(ctx, GameId.Female01Avatar, GameId.FemaleAssassin);
			TryGiveUpdatedItem(ctx, GameId.Female02Avatar, GameId.FemaleSuperstar);
		}
	}
}