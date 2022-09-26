using FirstLight.Game.Logic;
using System.Linq;
using FirstLight.Game.Messages;
using FirstLight.Services;
using FirstLight.Game.Services;

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
		public void Execute(IGameLogic gameLogic, IDataProvider dataProvider)
		{
			gameLogic.MessageBrokerService.Publish(new UnclaimedRewardsCollectingStartedMessage() {Rewards = gameLogic.RewardLogic.UnclaimedRewards.ToList()});
			var rewards = gameLogic.RewardLogic.ClaimUncollectedRewards();
			gameLogic.MessageBrokerService.Publish(new UnclaimedRewardsCollectedMessage { Rewards = rewards });
		}
	}
}