using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	public class UpdatePlayerStoreDataCommand : IGameCommand
	{
		public string CatalogItemId;

		public StoreItemData StoreItemData;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
	
			ctx.Logic.PlayerStoreLogic().UpdateLastPlayerPurchase(CatalogItemId, StoreItemData);
			return UniTask.CompletedTask;
		}
	}
}