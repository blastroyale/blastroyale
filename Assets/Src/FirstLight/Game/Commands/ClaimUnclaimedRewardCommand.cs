using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
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
	public struct ClaimUnclaimedRewardCommand : IGameCommand
	{
		public ItemData ToClaim;
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			var msg = new RewardClaimedMessage {Reward = ctx.Logic.RewardLogic().ClaimUnclaimedReward(ToClaim)};
			ctx.Services.MessageBrokerService().Publish(msg);
			return UniTask.CompletedTask;
		}
	}
}