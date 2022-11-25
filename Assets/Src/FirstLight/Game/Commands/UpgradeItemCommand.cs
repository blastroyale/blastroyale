using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;

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

		public void Execute(CommandExecutionContext ctx)
		{
			var logic = ctx.Logic.EquipmentLogic();
			var item = logic.Inventory[Item];
			var cost = logic.GetUpgradeCost(item, false);
			
			ctx.Logic.CurrencyLogic().DeductCurrency(cost.Key, cost.Value);
			logic.Upgrade(Item);
		}
	}
}