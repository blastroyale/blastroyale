using System;
using System.Collections.Generic;
using System.Linq;
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
	/// Object to represent the origin of a reward.
	/// All information needed to be able to calculate a reward should be self
	/// contained in this object.
	/// </summary>
	public class RewardSource
	{
		/// <summary>
		/// Reflects if a player quit early in the game
		/// </summary>
		public bool DidPlayerQuit { get; set; }
		
		/// <summary>
		/// Reflects how many players were participating in the game
		/// </summary>
		public int GamePlayerCount { get; set; }
		
		/// <summary>
		/// Reflects the match data collected by quantum simulation
		/// </summary>
		public QuantumPlayerMatchData MatchData { get; set; }
		
		/// <summary>
		/// Reflects the type of the given match
		/// </summary>
		public MatchType MatchType { get; set; }
	}
	
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
		/// Checks if we are currently collecting rewards (running <see cref="IRewardLogic.ClaimUncollectedRewards"/>).
		/// </summary>
		bool IsCollecting { get; }

		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> CalculateMatchRewards(RewardSource source);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="RewardSource"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(RewardSource source);

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
		public bool IsCollecting { get; private set; }

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_unclaimedRewards = new ObservableList<RewardData>(Data.UncollectedRewards);
		}

		public List<RewardData> CalculateMatchRewards(RewardSource source)
		{
			var rewards = new List<RewardData>();
			var matchType = source.MatchType;
			
			if (source.MatchData.PlayerRank == 0)
			{
				throw new MatchDataEmptyLogicException();
			}

			// We don't reward quitters and we don't reward players for Custom games
			if (matchType == MatchType.Custom || source.DidPlayerQuit)
			{
				return rewards;
			}

			// Always perform ordering operation on the configs.
			// If config data placement order changed in google sheet, it could silently screw up this algorithm.
			var gameModeRewardConfigs = GameLogic.ConfigsProvider
			                                     .GetConfigsList<MatchRewardConfig>()
			                                     .OrderByDescending(x => x.Placement).ToList();

			var rewardConfig = gameModeRewardConfigs[0];
			
			// We calculate rank value for rewards based on the number of players in a match versus maximum of 30
			var rankValue = Math.Min(1 + Math.Round(30 / (double)source.GamePlayerCount) * (source.MatchData.PlayerRank - 1), 30);
			
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
		public List<RewardData> GiveMatchRewards(RewardSource source)
		{
			var rewards = CalculateMatchRewards(source);

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
			IsCollecting = true;
			
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);

			foreach (var reward in Data.UncollectedRewards)
			{
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.Clear();
			
			IsCollecting = false;

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
