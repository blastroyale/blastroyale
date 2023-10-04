using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Upgrades an Non-NFT item and awards the player resources 
	/// </summary>
	public struct FuseItemCommand : IGameCommand
	{
		public UniqueId Item;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
			var logic = ctx.Logic.EquipmentLogic();
			var info = logic.GetInfo(Item);
			var item = info.Equipment;
			var cost = info.FuseCost;

			foreach(var price in cost)
			{
				ctx.Logic.CurrencyLogic().DeductCurrency(price.Key, price.Value);
				ctx.Services.MessageBrokerService().Publish(new CurrencyChangedMessage
				{
					Id = price.Key,
					Change = -(int) price.Value,
					Category = "fuse",
					NewValue = ctx.Logic.CurrencyLogic().GetCurrencyAmount(price.Key)
				});
			}

			logic.Fuse(Item);

			ctx.Services.MessageBrokerService().Publish(new ItemFusedMessage
			{
				Id = Item,
				GameId = info.Equipment.GameId,
				Name = info.Equipment.GameId.ToString(),
				Durability = info.CurrentDurability,
				rarity = item.Rarity +1,
				Price = cost
			});
		}
	}
}