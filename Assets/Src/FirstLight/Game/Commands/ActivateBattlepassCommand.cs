using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public class ActivateBattlepassCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.BattlePassLogic().Purchase();
			return UniTask.CompletedTask;
		}
	}
}