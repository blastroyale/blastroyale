using Cysharp.Threading.Tasks;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Scraps an Non-NFT item and awards the player resources 
	/// </summary>
	public struct RepairItemCommand : IGameCommand
	{
		public UniqueId Item;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			var logic = ctx.Logic.EquipmentLogic();
			var info = logic.GetInfo(Item);
			var item = logic.Inventory[Item];
			var cost = logic.GetRepairCost(item, false);

			ctx.Logic.CurrencyLogic().DeductCurrency(cost.Key, cost.Value);
			ctx.Services.MessageBrokerService().Publish(new CurrencyChangedMessage
			{
				Id = cost.Key,
				Change = -(int) cost.Value,
				Category = "repair",
				NewValue = ctx.Logic.CurrencyLogic().GetCurrencyAmount(cost.Key)
			});
			logic.Repair(Item);

			ctx.Services.MessageBrokerService().Publish(new ItemRepairedMessage
			{
				Id = Item,
				GameId = info.Equipment.GameId,
				Name = info.Equipment.GameId.ToString(),
				Durability = info.CurrentDurability,
				DurabilityFinal = info.Equipment.MaxDurability,
				Price = cost
			});
			return UniTask.CompletedTask;
		}
	}
}