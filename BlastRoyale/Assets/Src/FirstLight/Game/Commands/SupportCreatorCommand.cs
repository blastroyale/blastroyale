using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	public class SupportCreatorCommand : IGameCommand
	{
		public string CreatorCode;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.ContentCreatorLogic().UpdateCreatorSupport(CreatorCode);

			return UniTask.CompletedTask;
		}
	}
}