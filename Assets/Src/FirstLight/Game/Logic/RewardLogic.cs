using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic.RPC;
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
		List<RewardData> CalculateMatchRewards(MatchType matchType, List<QuantumPlayerMatchData> matchData,
											   int executingPlayer,
											   bool didPlayerQuit,
											   out int trophyChange);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(MatchType matchType, List<QuantumPlayerMatchData> matchData,
										  int executingPlayer, bool didPlayerQuit, out int trophyChange);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<RewardData> ClaimUncollectedRewards();

		List<Equipment> ClaimIAPRewards();

		/// <summary>
		/// Adds an IAP reward to the list of unclaimed rewards. This is used when doing an IAP, to
		/// sync up the server and client, without having to do another request (since the server
		/// adds it on it's end).
		/// </summary>
		void AddIAPReward(RewardData reward);
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

		public List<RewardData> CalculateMatchRewards(MatchType matchType, List<QuantumPlayerMatchData> matchData,
													  int executingPlayer,
													  bool didPlayerQuit,
													  out int trophyChange)
		{
			var rewards = new List<RewardData>();
			var localMatchData = matchData[executingPlayer];
			trophyChange = 0;

			if (localMatchData.PlayerRank == 0)
			{
				throw new MatchDataEmptyLogicException();
			}

			// Currently, there is no plan on giving rewards on anything but BR mode
			if (matchType == MatchType.Custom || didPlayerQuit)
			{
				GiveTrophiesReward(rewards, matchData, localMatchData, out trophyChange);
				return rewards;
			}

			// Always perform ordering operation on the configs.
			// If config data placement order changed in google sheet, it could silently screw up this algorithm.
			var gameModeRewardConfigs = GameLogic.ConfigsProvider
				.GetConfigsList<MatchRewardConfig>()
				.OrderByDescending(x => x.Placement).ToList();

			var rewardConfig = gameModeRewardConfigs[0];
			var rankValue = localMatchData.PlayerRank;

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
				GiveTrophiesReward(rewards, matchData, localMatchData, out trophyChange);
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

		private void GiveTrophiesReward(ICollection<RewardData> rewards,
										IReadOnlyCollection<QuantumPlayerMatchData> players,
										QuantumPlayerMatchData localPlayerData,
										out int trophyChangeOut)
		{
			var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

			var tempPlayers = new List<QuantumPlayerMatchData>(players);
			tempPlayers.SortByPlayerRank(false);

			var trophyChange = 0d;

			// Losses; Note: PlayerRank starts from 1, not from 0
			for (var i = 0; i < localPlayerData.PlayerRank - 1; i++)
			{
				trophyChange += CalculateEloChange(0d, tempPlayers[i].Data.PlayerTrophies,
					localPlayerData.Data.PlayerTrophies, gameConfig.TrophyEloRange,
					gameConfig.TrophyEloK.AsDouble);
			}

			// Wins; Note: PlayerRank starts from 1, not from 0
			for (var i = (int) localPlayerData.PlayerRank; i < players.Count; i++)
			{
				trophyChange += CalculateEloChange(1d, tempPlayers[i].Data.PlayerTrophies,
					localPlayerData.Data.PlayerTrophies, gameConfig.TrophyEloRange,
					gameConfig.TrophyEloK.AsDouble);
			}

			var finalTrophyChange = (int) Math.Round(trophyChange);

			if (finalTrophyChange < 0 && Math.Abs(finalTrophyChange) > Data.Trophies)
			{
				finalTrophyChange = (int) -Data.Trophies;
			}

			trophyChangeOut = finalTrophyChange;
			rewards.Add(new RewardData(GameId.Trophies, finalTrophyChange));
		}

		private double CalculateEloChange(double score, uint trophiesOpponent, uint trophiesPlayer, int eloRange,
										  double eloK)
		{
			var eloBracket = Math.Pow(10, ((int) trophiesOpponent - (int) trophiesPlayer) / (float) eloRange);

			return eloK * (score - 1 / (1 + eloBracket));
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(MatchType matchType, List<QuantumPlayerMatchData> matchData,
												 int executingPlayer,
												 bool didPlayerQuit,
												 out int trophyChange)
		{
			var rewards = CalculateMatchRewards(matchType, matchData, executingPlayer, didPlayerQuit, out trophyChange);

			foreach (var reward in rewards)
			{
				var rewardData = reward;

				if (rewardData.RewardId.IsInGroup(GameIdGroup.ResourcePool))
				{
					rewardData.Value =
						(int) GameLogic.ResourceLogic.WithdrawFromResourcePool(reward.RewardId, (uint) reward.Value);
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
				if (reward.RewardId.IsInGroup(GameIdGroup.IAP)) continue;
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.RemoveAll(r => rewards.Contains(r));

			return rewards;
		}

		public List<Equipment> ClaimIAPRewards()
		{
			var rewards = new List<Equipment>(1);

			foreach (var reward in Data.UncollectedRewards)
			{
				if (!reward.RewardId.IsInGroup(GameIdGroup.IAP)) continue;
				rewards.Add(ClaimEquipmentReward(reward.RewardId));
			}

			Data.UncollectedRewards.RemoveAll(r => r.RewardId.IsInGroup(GameIdGroup.IAP));

			return rewards;
		}

		public void AddIAPReward(RewardData reward)
		{
			Data.UncollectedRewards.Add(reward);
		}

		private RewardData ClaimReward(RewardData reward)
		{
			if (reward.RewardId == GameId.XP)
			{
				GameLogic.PlayerLogic.AddXp((uint) reward.Value);
			}
			else if (reward.RewardId == GameId.BPP)
			{
				GameLogic.BattlePassLogic.AddBPP((uint) reward.Value);
			}
			else if (reward.RewardId == GameId.Trophies)
			{
				GameLogic.PlayerLogic.UpdateTrophies(reward.Value);
			}
			else if (reward.RewardId.IsInGroup(GameIdGroup.Currency))
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

		private Equipment ClaimEquipmentReward(GameId id)
		{
			var config = GameLogic.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>()
				.First(cfg => cfg.GameId == id);

			var equipment = GameLogic.EquipmentLogic.GenerateEquipmentFromConfig(config);
			GameLogic.EquipmentLogic.AddToInventory(equipment);
			return equipment;
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