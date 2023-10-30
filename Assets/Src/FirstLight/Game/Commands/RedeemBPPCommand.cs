using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Award the BPP to the player and if the player reaches a new level, gives automatically the reward
	/// </summary>
	public struct RedeemBPPCommand : IGameCommand
	{
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
			var freeRewards = ctx.Logic.BattlePassLogic().ClaimBattlePassPoints(PassType.Free);
			var paidRewards = ctx.Logic.BattlePassLogic().ClaimBattlePassPoints(PassType.Paid);
			var allRewards = freeRewards.Concat(paidRewards).ToList();
			if (!allRewards.Any()) return;
			
			var rewardItems = ctx.Logic.RewardLogic().CreateItemsFromConfigs(allRewards);
			ctx.Logic.RewardLogic().Reward(rewardItems);
			
			var newLevel = ctx.Logic.BattlePassLogic().CurrentLevel.Value;
			ctx.Services.MessageBrokerService().Publish(new BattlePassLevelUpMessage
			{
				Rewards = rewardItems,
				NewLevel = newLevel
			});
		}
	}
}