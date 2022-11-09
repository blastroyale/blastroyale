using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using FirstLight.Game.Services;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Collects all the reward on the to the player's current inventory.
	/// </summary>
	public struct CollectIAPRewardCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public void Execute(CommandExecutionContext ctx)
		{
			var rewards = ctx.Logic.RewardLogic().ClaimIAPRewards();
			ctx.Services.MessageBrokerService().Publish(new IAPPurchaseCompletedMessage {Rewards = rewards});
		}
	}
}