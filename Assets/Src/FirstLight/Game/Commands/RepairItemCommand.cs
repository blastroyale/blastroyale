using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;

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

		public void Execute(CommandExecutionContext ctx)
		{
			var info = ctx.Logic.EquipmentLogic().GetInfo(Item);
			
			ctx.Logic.CurrencyLogic().DeductCurrency(info.RepairCost.Key, info.RepairCost.Value);
			ctx.Logic.EquipmentLogic().Repair(Item);
		}
	}
}