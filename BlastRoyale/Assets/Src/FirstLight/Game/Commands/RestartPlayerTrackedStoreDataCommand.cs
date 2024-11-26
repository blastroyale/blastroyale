using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Initializes battle pass data and also claim previous season rewards
	/// </summary>
	public struct RestartPlayerTrackedStoreDataCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;
		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Initialization;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.PlayerStoreLogic().TryResetTrackedStoreData();
			
			return UniTask.CompletedTask;
		}
	}
}