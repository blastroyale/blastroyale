using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Services;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.Commands;
using Quantum;

namespace FirstLight.Game.Commands
{
	/// <summary>
	/// Collects all the reward on the to the player's current inventory.
	/// </summary>
	public class
		CollectUnclaimedRewardsCommand : IGameCommandWithResult<IReadOnlyList<ItemData>> // This needs to be a class due to the GivenRewards use after command execution
	{
		/// <summary>
		/// Set during command execution as a "Result"
		/// </summary>
		public IReadOnlyList<ItemData> GivenRewards { get; private set; }

		/// <summary>
		/// If this field is != null it will only claim this specific reward, otherwise it will claim it all
		/// </summary>
		public ItemData UncollectedReward;

		public CommandAccessLevel AccessLevel() => CommandAccessLevel.Player;

		public CommandExecutionMode ExecutionMode() => CommandExecutionMode.Server;

		/// <inheritdoc />
		public UniTask Execute(CommandExecutionContext ctx)
		{
			if (UncollectedReward != null)
			{
				var given = new List<ItemData>();
				
				if (UncollectedReward.Id == GameId.Bundle)
				{
					given.AddRange(ctx.Logic.RewardLogic().ClaimUnclaimedRewards());
				}
				else
				{
					given.Add(ctx.Logic.RewardLogic().ClaimUnclaimedReward(UncollectedReward));	
				}
				
				GivenRewards = given.ToArray();
				return UniTask.CompletedTask;
			}

			var trophiesBefore = ctx.Logic.PlayerLogic().Trophies.Value;
			var rewards = ctx.Logic.RewardLogic().ClaimUnclaimedRewards();
			ctx.Services.MessageBrokerService().Publish(new ClaimedRewardsMessage()
			{
				Rewards = rewards
			});
			GivenRewards = rewards;
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

			return UniTask.CompletedTask;
		}

		public IReadOnlyList<ItemData> GetResult()
		{
			return GivenRewards;
		}
	}
}