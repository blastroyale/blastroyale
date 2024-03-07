using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Command to ensure player has no invalid data before logging in
	/// </summary>
	public struct CleanupOldDataCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Initialization;

		private bool IsInvalidItem(ItemData i) => i.Id.IsInGroup(GameIdGroup.Deprecated);

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var pd = ctx.Data.GetData<PlayerData>();
			pd.UncollectedRewards.RemoveAll(IsInvalidItem);
			return UniTask.CompletedTask;
		}
	}
}