using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	public class UpdatePlayerDailyDealsStoreConfigurationCommand : IGameCommand
	{
		public PlayerDailyDealsConfiguration PlayerDailyDealsConfiguration;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			
			ctx.Logic.PlayerStoreLogic().UpdatePlayerDailyDeals(PlayerDailyDealsConfiguration);
			
			return UniTask.CompletedTask;
		}
	}
}