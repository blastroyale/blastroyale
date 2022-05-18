using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
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
		IReadOnlyList<RewardData> UnclaimedRewards { get; }

		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		Dictionary<GameId, int> GetMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<RewardData> CollectUnclaimedRewards();

		/// <summary>
		/// Awards the given <paramref name="reward"/> to the player
		/// </summary>
		RewardData GiveReward(RewardData reward);
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class RewardLogic : AbstractBaseLogic<PlayerData>, IRewardLogic
	{
		/// <inheritdoc />
		public IReadOnlyList<RewardData> UnclaimedRewards => Data.UncollectedRewards;

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public Dictionary<GameId, int> GetMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var mapConfig = GameLogic.ConfigsProvider.GetConfig<MapConfig>(matchData.MapId);
			var rewards = new Dictionary<GameId, int>();
			
			// Currently, there is no plan on giving rewards on anything but BR mode
			if (mapConfig.GameMode != GameMode.BattleRoyale)
			{
				return rewards;
			}
			
			// Always perform ordering operation on the configs.
			// If config data placement order changed in google sheet, it could silently screw up this algorithm.
			var gameModeRewardConfigs = GameLogic.ConfigsProvider
			                                     .GetConfigsList<MatchRewardConfig>()
			                                     .Where(x => x.GameMode == mapConfig.GameMode)
			                                     .OrderByDescending(x => x.Placement).ToList();

			// Get worst reward placement reward by default, or specific placement reward thereafter
			var rewardConfig = gameModeRewardConfigs[0];
			var rankValue = (mapConfig.PlayersLimit + 1) - matchData.PlayerRank;
			
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

			var csRewardPair = rewardConfig.RewardPairs.FirstOrDefault(x => x.Key == GameId.CS);
			var csMaxTake = 100; // TODO - Replace with NFT equipment calculation
			var csRewardAmount = csMaxTake * csRewardPair.Value;
			// csRewardPair.Value is the percent of the max CS take that people will be awarded
			
			if (csRewardAmount > 0)
			{
				rewards.Add(GameId.CS, (int) GameLogic.CurrencyLogic.WithdrawFromResourcePool((ulong)csRewardAmount,GameId.CS));
			}

			return rewards;
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var rewards = GetMatchRewards(matchData, didPlayerQuit);
			var rewardsList = new List<RewardData>();

			foreach (var reward in rewards)
			{
				var rewardData = new RewardData(reward.Key, reward.Value);
				
				rewardsList.Add(rewardData);
				Data.UncollectedRewards.Add(rewardData);
			}

			return rewardsList;
		}

		/// <inheritdoc />
		public List<RewardData> CollectUnclaimedRewards()
		{
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);
			
			if (Data.UncollectedRewards.Count == 0)
			{
				throw new LogicException("The player does not have any rewards to collect.");
			}

			Data.UncollectedRewards.Clear();

			return rewards;
		}

		/// <inheritdoc />
		public RewardData GiveReward(RewardData reward)
		{
			var groups = reward.RewardId.GetGroups();

			if (reward.RewardId == GameId.XP)
			{
				GameLogic.PlayerLogic.AddXp((uint) reward.Value);
			}
			else if (groups.Contains(GameIdGroup.Currency))
			{
				GameLogic.CurrencyLogic.AddCurrency(reward.RewardId, (uint) reward.Value);
			}
			else
			{
				throw new LogicException($"The reward '{reward.RewardId}' is not from a group type that is rewardable.");
			}

			return reward;
		}
	}
}