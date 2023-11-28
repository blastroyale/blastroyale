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
	public struct GiveDefaultCollectionItemsCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;
		
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
		
		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			GiveDefaultItems(ctx);
		}
	}
}