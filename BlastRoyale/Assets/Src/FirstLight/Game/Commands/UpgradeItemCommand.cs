using Cysharp.Threading.Tasks;
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
	public struct UpgradeItemCommand : IGameCommand
	{
		public UniqueId Item;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			var logic = ctx.Logic.EquipmentLogic();
			var info = logic.GetInfo(Item);
			var item = logic.Inventory[Item];
			var cost = logic.GetUpgradeCost(item, false);

			ctx.Logic.CurrencyLogic().DeductCurrency(cost.Key, cost.Value);
			ctx.Services.MessageBrokerService().Publish(new CurrencyChangedMessage
			{
				Id = cost.Key,
				Change = -(int) cost.Value,
				Category = "upgrade",
				NewValue = ctx.Logic.CurrencyLogic().GetCurrencyAmount(cost.Key)
			});
			logic.Upgrade(Item);

			ctx.Services.MessageBrokerService().Publish(new ItemUpgradedMessage
			{
				Id = Item,
				GameId = info.Equipment.GameId,
				Name = info.Equipment.GameId.ToString(),
				Durability = info.CurrentDurability,
				Level = (uint)(item.Level + 1),
				Price = cost
			});
			return UniTask.CompletedTask;
		}
	}
}