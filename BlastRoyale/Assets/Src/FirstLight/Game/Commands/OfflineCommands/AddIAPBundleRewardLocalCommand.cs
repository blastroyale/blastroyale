using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands.OfflineCommands
{
	/// <summary>
	/// Adds a reward to the local list of unclaimed rewards (used to sync up server)
	/// and client).
	/// </summary>
	public class AddIAPBundleRewardLocalCommand : IGameCommand
	{
		public ItemData[] BundleRewards;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.ClientOnly;

		public UniTask Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.RewardLogic().RewardToUnclaimedRewards(BundleRewards);
			return UniTask.CompletedTask;
		}
	}
}