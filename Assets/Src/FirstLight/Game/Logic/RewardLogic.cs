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
		Dictionary<GameId, int> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit);
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
		List<RewardData> ClaimUncollectedRewards();

		/// <summary>
		/// Awards the given <paramref name="reward"/> to the player
		/// </summary>
		RewardData ClaimReward(RewardData reward);
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
		public Dictionary<GameId, int> CalculateMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
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
			                                     .OrderByDescending(x => x.Placement).ToList();

			// Get worst reward placement reward by default, or specific placement reward thereafter
			var rewardConfig = gameModeRewardConfigs[0];
			var rankValue = 10; //(mapConfig.PlayersLimit + 1) - matchData.PlayerRank;
			// TODO - remove the hard coded 10, when matchData.PlayerRank is fixed. PlayerRank does needs fixing for BR.
			
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
			var csPercent = csRewardPair.Value / 100f;
			// csRewardPair.Value is the absolute percent of the max CS take that people will be awarded
			
			var csMaxTake = 100; // TODO - Replace with NFT equipment calculation
			var csRewardAmount = csMaxTake * csPercent;
			var csWithdrawn = (int) GameLogic.CurrencyLogic.WithdrawFromResourcePool((ulong) csRewardAmount, GameId.CS);
			
			
			if (csWithdrawn > 0)
			{
				rewards.Add(GameId.CS, csWithdrawn);
			}

			return rewards;
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(QuantumPlayerMatchData matchData, bool didPlayerQuit)
		{
			var rewards = CalculateMatchRewards(matchData, didPlayerQuit);
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
		public List<RewardData> ClaimUncollectedRewards()
		{
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);
			
			if (Data.UncollectedRewards.Count == 0)
			{
				throw new LogicException("The player does not have any rewards to collect.");
			}
			
			foreach (var reward in Data.UncollectedRewards)
			{
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.Clear();

			return rewards;
		}

		/// <inheritdoc />
		public RewardData ClaimReward(RewardData reward)
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