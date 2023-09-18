using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Collects all the reward on the to the player's current inventory.
	/// </summary>
	public struct CollectUnclaimedRewardsCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			var trophiesBefore = ctx.Logic.PlayerLogic().Trophies.Value;
			ctx.Logic.RewardLogic().ClaimUncollectedRewards();
			var trophiesAfter = ctx.Logic.PlayerLogic().Trophies.Value;
			if (trophiesBefore != trophiesAfter)
			{
				ctx.Services.MessageBrokerService().Publish(new TrophiesUpdatedMessage()
				{
					Season = ctx.Data.GetData<PlayerData>().TrophySeason,
					NewValue = trophiesAfter,
					OldValue = trophiesBefore
				});
			}
		}
	}
}