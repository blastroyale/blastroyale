using Cysharp.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	public class RefundEventPassesCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.GameEvents().RefundEventPasses();
			return UniTask.CompletedTask;
		}
	}
}