using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
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
			var info = ctx.Logic.EquipmentLogic().GetInfo(Item);
			
			if (info.IsNft)
			{
				throw new LogicException($"Not allowed to scrap NFT items on the client, only on the hub and {Item} is a NFT");
			}
			
			ctx.Logic.CurrencyLogic().DeductCurrency(info.UpgradeCost.Key, info.UpgradeCost.Value);
			ctx.Logic.EquipmentLogic().Upgrade(Item);
		}
	}
}