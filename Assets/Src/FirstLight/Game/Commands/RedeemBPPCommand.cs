using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Modules.Commands;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Award the BPP to the player and if the player reaches a new level, gives automatically the reward
	/// </summary>
	public struct RedeemBPPCommand : IGameCommand
	{
		public PassType PassType;
		
		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		public void Execute(CommandExecutionContext ctx)
		{
			if (PassType == PassType.Paid && !ctx.Logic.BattlePassLogic().HasPurchasedSeason())
			{
				throw new LogicException("Paid Battle Pass not unlocked");
			}
			var rewards = ctx.Logic.BattlePassLogic().ClaimBattlePassPoints(PassType);
			if (rewards.Count == 0) return;
			var rewardItems = ctx.Logic.RewardLogic().CreateItemsFromConfigs(rewards);
			ctx.Logic.RewardLogic().Reward(rewardItems);
			ctx.Services.MessageBrokerService().Publish(new BattlePassLevelUpMessage
			{
				Rewards = rewardItems,
				NewLevel = ctx.Logic.BattlePassLogic().CurrentLevel.Value
			});
		}
	}
}