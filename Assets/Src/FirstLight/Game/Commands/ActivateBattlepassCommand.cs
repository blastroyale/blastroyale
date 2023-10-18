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

		public void Execute(CommandExecutionContext ctx)
		{
			if (ctx.Logic.BattlePassLogic().Purchase())
			{
				ctx.Services.MessageBrokerService().Publish(new BattlePassPurchasedMessage());
			}
		}
	}
}