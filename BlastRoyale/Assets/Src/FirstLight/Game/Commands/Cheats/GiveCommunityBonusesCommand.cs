using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands.Cheats
{
	/// <summary>
	/// Give stuff to players on community builds
	/// </summary>
	public class GiveCommunityBonusesCommand : IGameCommand, IEnvironmentLock
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			// Add some coins as well to repair and upgrade
			ctx.Logic.CurrencyLogic().AddCurrency(GameId.COIN, 1_000_000);
			return UniTask.CompletedTask;
		}

		public string[] AllowedEnvironments() => new[] {FLEnvironment.DEVELOPMENT.Name, FLEnvironment.COMMUNITY.Name};
	}
}