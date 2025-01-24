using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	public class MarkProductsBundleBannerAsSeenCommand : IGameCommand
	{
		public string BundleId;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.PlayerStoreLogic().MarkProductsBundleAsSeen(BundleId);
			return UniTask.CompletedTask;
		}
	}
}