using System;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	public class MarkProductsBundleFirstAppearedCommand : IGameCommandWithResult<DateTime>
	{
		public string BundleId;

		public DateTime FirstAppeared { get; private set; }

		public DateTime GetResult()
		{
			return FirstAppeared;
		}

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			FirstAppeared = ctx.Logic.PlayerStoreLogic().MarkProductsBundleFirstAppeared(BundleId);
			return UniTask.CompletedTask;
		}
	}
}