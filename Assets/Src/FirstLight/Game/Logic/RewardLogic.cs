using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's rewards
	/// </summary>
	public interface IRewardDataProvider
	{
		/// <summary>
		/// Requests the list of rewards in buffer to be awarded to the player
		/// </summary>
		IObservableListReader<RewardData> UnclaimedRewards { get; }

		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> CalculateMatchRewards(MatchType matchType, QuantumPlayerMatchData matchData,
		                                       bool didPlayerQuit);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(MatchType matchType, QuantumPlayerMatchData matchData, bool didPlayerQuit);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<RewardData> ClaimUncollectedRewards();
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class RewardLogic : AbstractBaseLogic<PlayerData>, IRewardLogic, IGameLogicInitializer
	{
		private IObservableList<RewardData> _unclaimedRewards;

		public IObservableListReader<RewardData> UnclaimedRewards => _unclaimedRewards;

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_unclaimedRewards = new ObservableList<RewardData>(Data.UncollectedRewards);
		}

		public List<RewardData> CalculateMatchRewards(MatchType matchType, QuantumPlayerMatchData matchData,
		                                              bool didPlayerQuit)
		{
			var rewards = new List<RewardData>();

			if (matchData.PlayerRank == 0)
			{
				throw new MatchDataEmptyLogicException();
			}

			// Currently, there is no plan on giving rewards on anything but BR mode
			if (matchType == MatchType.Custom || didPlayerQuit)
			{
				return rewards;
			}

			// Always perform ordering operation on the configs.
			// If config data placement order changed in google sheet, it could silently screw up this algorithm.
			var gameModeRewardConfigs = GameLogic.ConfigsProvider
			                                     .GetConfigsList<MatchRewardConfig>()
			                                     .OrderByDescending(x => x.Placement).ToList();

			var rewardConfig = gameModeRewardConfigs[0];
			var rankValue = matchData.PlayerRank;

			foreach (var config in gameModeRewardConfigs)
			{
				if (rankValue > config.Placement)
				{
					break;
				}

				if (config.Placement == rankValue)
				{
					rewardConfig = config;
					break;
				}
			}

			if (matchType == MatchType.Ranked)
			{
				GiveCSReward(rewards, rewardConfig);
			}

			if (matchType is MatchType.Ranked or MatchType.Casual)
			{
				GiveBPPReward(rewards, rewardConfig);
			}

			return rewards;
		}

		private void GiveCSReward(ICollection<RewardData> rewards, MatchRewardConfig rewardConfig)
		{
			var rewardPair = rewardConfig.Rewards.FirstOrDefault(x => x.Key == GameId.CS);
			var percent = rewardPair.Value / 100d;
			// rewardPair.Value is the absolute percent of the max take that people will be awarded

			var info = GameLogic.ResourceLogic.GetResourcePoolInfo(GameId.CS);
			var take = (uint) Math.Ceiling(info.WinnerRewardAmount * percent);
			var withdrawn = (int) Math.Min(info.CurrentAmount, take);

			if (withdrawn > 0)
			{
				rewards.Add(new RewardData(GameId.CS, withdrawn));
			}
		}

		private void GiveBPPReward(ICollection<RewardData> rewards, MatchRewardConfig rewardConfig)
		{
			if (rewardConfig.Rewards.TryGetValue(GameId.BPP, out var amount))
			{
				var info = GameLogic.ResourceLogic.GetResourcePoolInfo(GameId.BPP);
				var withdrawn = (int) Math.Min(info.CurrentAmount, amount);
				
				var remainingPoints = GameLogic.BattlePassLogic.GetRemainingPoints();

				withdrawn = (int) Math.Min(withdrawn, remainingPoints);

				if (withdrawn > 0)
				{
					rewards.Add(new RewardData(GameId.BPP, withdrawn));
				}
			}
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(MatchType matchType, QuantumPlayerMatchData matchData,
		                                         bool didPlayerQuit)
		{
			var rewards = CalculateMatchRewards(matchType, matchData, didPlayerQuit);

			foreach (var reward in rewards)
			{
				var rewardData = reward;
   
				if (rewardData.RewardId.IsInGroup(GameIdGroup.ResourcePool))
				{
					rewardData.Value = (int) GameLogic.ResourceLogic.WithdrawFromResourcePool(reward.RewardId, (uint) reward.Value);
				}

				Data.UncollectedRewards.Add(rewardData);
			}

			return rewards;
		}

		/// <inheritdoc />
		public List<RewardData> ClaimUncollectedRewards()
		{
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);

			foreach (var reward in Data.UncollectedRewards)
			{
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.Clear();

			return rewards;
		}

		private RewardData ClaimReward(RewardData reward)
		{
			var groups = reward.RewardId.GetGroups();

			if (reward.RewardId == GameId.XP)
			{
				GameLogic.PlayerLogic.AddXp((uint) reward.Value);
			}
			else if (reward.RewardId == GameId.BPP)
			{
				GameLogic.BattlePassLogic.AddBPP((uint) reward.Value);
			}
			else if (groups.Contains(GameIdGroup.Currency))
			{
				GameLogic.CurrencyLogic.AddCurrency(reward.RewardId, (uint) reward.Value);
			}
			else
			{
				throw
					new LogicException($"The reward '{reward.RewardId}' is not from a group type that is rewardable.");
			}

			return reward;
		}
	}

	public class MatchDataEmptyLogicException : LogicException
	{
		private const string MATCH_DATA_EMPTY_EXCEPTION_MESSAGE =
			"MatchData parameter should not be empty";
		public MatchDataEmptyLogicException() : base(MATCH_DATA_EMPTY_EXCEPTION_MESSAGE)
		{
		}
 
		public MatchDataEmptyLogicException(string message, Exception inner) : base(message, inner)
		{
		}
	}
}
