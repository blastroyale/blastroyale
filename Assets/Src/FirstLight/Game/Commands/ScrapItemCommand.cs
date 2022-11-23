using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Scraps an Non-NFT item and awards the player resources 
	/// </summary>
	public struct ScrapItemCommand : IGameCommand
	{
		public UniqueId Item;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
			var info = ctx.Logic.EquipmentLogic().Scrap(Item);
			
			ctx.Logic.CurrencyLogic().AddCurrency(info.ScrappingValue.Key, info.ScrappingValue.Value);
		}
	}
}