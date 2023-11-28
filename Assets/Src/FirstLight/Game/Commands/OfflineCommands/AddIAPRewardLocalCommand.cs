using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;

namespace FirstLight.Game.Commands.OfflineCommands
{
	/// <summary>
	/// Adds a reward to the local list of unclaimed rewards (used to sync up server)
	/// and client).
	/// </summary>
	public class AddIAPRewardLocalCommand : IGameCommand
	{
		public ItemData Reward;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.ClientOnly;

		public void Execute(CommandExecutionContext ctx)
		{
			ctx.Logic.RewardLogic().RewardToUnclaimedRewards(new [] { Reward });
		}
	}
}