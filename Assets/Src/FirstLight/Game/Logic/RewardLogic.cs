using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Newtonsoft.Json;
using Quantum;
using UnityEngine.Purchasing;

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
		public List<QuantumPlayerMatchData> MatchData { get; set; }

		/// <summary>
		/// TODO
		/// </summary>
		public int ExecutingPlayer { get; set; }

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
		List<RewardData> CalculateMatchRewards(RewardSource source, out int trophyChange);

		/// <summary>
		/// Check if the <see cref="UnclaimedRewards"/> list contains a reward that could
		/// belong to a purchase made from the store.
		/// </summary>
		bool HasUnclaimedPurchase(Product product);

		/// <summary>
		/// Checks if there are any items belonging to the <see cref="GameIdGroup.IAP"/> in the
		/// <see cref="UnclaimedRewards"/> list.
		/// </summary>
		bool HasUnclaimedPurchases();
		
		/// <summary>
		/// Obtains the rewards for a given tutorial step
		/// </summary>
		List<ItemData> GetRewardsFromTutorial(TutorialSection section);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="RewardSource"/> performance from a game completed
		/// </summary>
		List<RewardData> GiveMatchRewards(RewardSource source, out int trophyChange);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<RewardData> ClaimUncollectedRewards();

		/// <summary>
		/// Claims all the unclaimed IAP rewards (that were purchased from the shop).
		/// </summary>
		/// <returns></returns>
		List<KeyValuePair<UniqueId,Equipment>> ClaimIAPRewards();

		/// <summary>
		/// Adds an IAP reward to the list of unclaimed rewards. This is used when doing an IAP, to
		/// sync up the server and client, without having to do another request (since the server
		/// adds it on it's end).
		/// </summary>
		void AddIAPReward(RewardData reward);

		/// <summary>
		/// Generic item handler to give items to player as rewards
		/// </summary>
		/// <param name="items"></param>
		void GiveItems(List<ItemData> items);
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

		public void ReInit()
		{
			{
				var listeners = _unclaimedRewards.GetObservers();
				_unclaimedRewards = new ObservableList<RewardData>(Data.UncollectedRewards);
				_unclaimedRewards.AddObservers(listeners);
			}
			
			_unclaimedRewards.InvokeUpdate();
		}

		public List<RewardData> CalculateMatchRewards(RewardSource source, out int trophyChange)
		{
			var rewards = new List<RewardData>();
			var localMatchData = source.MatchData[source.ExecutingPlayer];
			trophyChange = 0;

			if (localMatchData.PlayerRank == 0)
			{
				throw new MatchDataEmptyLogicException();
			}

			var gameModeConfig =
				GameLogic.ConfigsProvider.GetConfig<QuantumGameModeConfig>(localMatchData.GameModeId);
			var teamSize = Math.Max(1, gameModeConfig.MaxPlayersInTeam);
			var maxTeamsInMatch = gameModeConfig.MaxPlayers / teamSize;
			
			// Always perform ordering operation on the configs.
			// If config data placement order changed in google sheet, it could silently screw up this algorithm.
			var gameModeRewardConfigs = GameLogic.ConfigsProvider
			                                     .GetConfigsList<MatchRewardConfig>()
			                                     .OrderByDescending(x => x.Placement).ToList();
			var gameModeTrophyConfigs = GameLogic.ConfigsProvider //clean this up
												 .GetConfigsList<TrophyRewardConfig>()
												 .OrderByDescending(x => x.Placement).ToList();
			var rewardConfig = gameModeRewardConfigs[0];
			var trophyRewardConfig = gameModeTrophyConfigs[0];

			// We calculate rank value for rewards based on the actual number of players/teams in a match (including bots)
			// versus the maximum number of players/teams that are supposed to be in a match. This interpolation is needed
			// in case we allow rewarded matches with lower number of players, for instance in case we ever do "no bots ranked"
			var rankValue = (int) Math.Min(1 + Math.Floor(maxTeamsInMatch / (double)((source.GamePlayerCount / teamSize) - 1) * (localMatchData.PlayerRank - 1)), maxTeamsInMatch);
			
			//clean this up
			foreach (var config in gameModeRewardConfigs)
			{
				if (teamSize == config.TeamSize && rankValue > config.Placement)
				{
					break;
				}

				if (config.Placement == rankValue && config.TeamSize == teamSize)
				{
					rewardConfig = config;
					break;
				}
			}

			foreach (var config in gameModeTrophyConfigs)
			{
				if (teamSize == config.TeamSize && rankValue > config.Placement)
				{
					break;
				}

				if (config.Placement == rankValue && config.TeamSize == teamSize)
				{
					trophyRewardConfig = config;
					break;
				}
			}

			// We don't reward quitters and we don't reward players for Custom games or games played alone (if we ever allow it)
			if (source.MatchType == MatchType.Custom || source.DidPlayerQuit || source.GamePlayerCount == 1)
			{
				if (source.MatchType == MatchType.Ranked && source.DidPlayerQuit)
				{
					CalculateTrophiesReward(rewards, source.MatchData, localMatchData, trophyRewardConfig, out trophyChange);
				}

				return rewards;
			}

			if (source.MatchType == MatchType.Ranked)
			{
				CalculateCSReward(rewards, rewardConfig, localMatchData.Data.CollectedOwnedNfts);
				CalculateTrophiesReward(rewards, source.MatchData, localMatchData, trophyRewardConfig, out trophyChange);
			}

			if (source.MatchType is MatchType.Ranked or MatchType.Casual)
			{
				CalculateBPPReward(rewards, rewardConfig);
			}

			return rewards;
		}

		public bool HasUnclaimedPurchase(Product product)
		{
			var productReward = JsonConvert.DeserializeObject<RewardData>(product.definition.payout.data);

			for (var i = 0; i < _unclaimedRewards.Count; i++)
			{
				if ( _unclaimedRewards[i].RewardId == productReward.RewardId)
				{
					return true;
				}
			}

			return false;
		}

		public bool HasUnclaimedPurchases()
		{
			for (var i = 0; i < _unclaimedRewards.Count; i++)
			{
				if (_unclaimedRewards[i].RewardId.IsInGroup(GameIdGroup.IAP))
				{
					return true;
				}
			}

			return false;
		}

		/// <inheritdoc />
		public List<RewardData> GiveMatchRewards(RewardSource source, out int trophyChange)
		{
			var rewards = CalculateMatchRewards(source, out trophyChange);

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
			IsCollecting = true;
			
			var rewards = new List<RewardData>(Data.UncollectedRewards.Count);

			foreach (var reward in Data.UncollectedRewards)
			{
				if (reward.RewardId.IsInGroup(GameIdGroup.IAP)) continue;
				rewards.Add(ClaimReward(reward));
			}

			Data.UncollectedRewards.RemoveAll(r => rewards.Contains(r));
			
			IsCollecting = false;

			return rewards;
		}

		public List<KeyValuePair<UniqueId,Equipment>> ClaimIAPRewards()
		{
			var rewards = new List<KeyValuePair<UniqueId,Equipment>>(1);

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

		public void GiveItems(List<ItemData> items)
		{
			foreach (var item in items)
			{
				if (item.ItemObject is Equipment eq)
				{
					GameLogic.EquipmentLogic.AddToInventory(eq);
				}
				else
				{
					_unclaimedRewards.Add(new RewardData()
					{
						Value = item.Amount,
						RewardId = item.Id
					});
				}
			}
		}

		public List<ItemData> GetRewardsFromTutorial(TutorialSection section)
		{
			var rewards = new List<ItemData>();
			var tutorialRewardsCfg = GameLogic.ConfigsProvider.GetConfigsList<TutorialRewardConfig>();
			var tutorialRewardsCount = tutorialRewardsCfg.Count(c => c.Section == section);

			// Omit rest of calculations if the tutorial doesn't have any rewards to give
			if (tutorialRewardsCount == 0) return rewards;
			
			var rewardsCfg = GameLogic.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var rewardsConfigs = rewardsCfg.Where(c => tutorialRewardsCfg.First(c => c.Section == section).RewardIds.Contains((uint)c.Id));
			
			foreach (var rewardConfig in rewardsConfigs)
			{
				if (rewardConfig.IsEquipment())
				{
					var equipment = GameLogic.EquipmentLogic.GenerateEquipmentFromConfig(rewardConfig);
					rewards.Add(new ItemData()
					{
						Amount = rewardConfig.Amount,
						Id = equipment.GameId,
						ItemObject = equipment
					});
				}
				else
				{
					// We always want to give a set amount of BPP only to complete first BP level during tutorial
					var finalAmount = section == TutorialSection.FIRST_GUIDE_MATCH && rewardConfig.GameId == GameId.BPP
						? (int) GameLogic.BattlePassLogic.GetRequiredPointsForLevel()
						: rewardConfig.Amount;
					
					rewards.Add(new ItemData()
					{
						Amount = finalAmount,
						Id = rewardConfig.GameId,
					});
				}
			}
			return rewards;
		}

		private RewardData ClaimReward(RewardData reward)
		{
			if (reward.RewardId == GameId.XP)
			{
				GameLogic.PlayerLogic.AddXP((uint) reward.Value);
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
					new LogicException($"The reward '{reward.RewardId.ToString()}' is not from a group type that is rewardable.");
			}

			return reward;
		}

		private KeyValuePair<UniqueId,Equipment> ClaimEquipmentReward(GameId id)
		{
			var config = GameLogic.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>()
				.First(cfg => cfg.GameId == id);

			var equipment = GameLogic.EquipmentLogic.GenerateEquipmentFromConfig(config);
			var uniqueId = GameLogic.EquipmentLogic.AddToInventory(equipment);
			return new KeyValuePair<UniqueId, Equipment>(uniqueId, equipment);
		}

		private void CalculateCSReward(ICollection<RewardData> rewards, MatchRewardConfig rewardConfig, uint collectedNFTsCount)
		{
			var rewardPair = rewardConfig.Rewards.FirstOrDefault(x => x.Key == GameId.CS);
			var percent = rewardPair.Value / 100d;
			// rewardPair.Value is the absolute percent of the max take that people will be awarded

			var info = GameLogic.ResourceLogic.GetResourcePoolInfo(GameId.CS);
			
			var takeForCollectedItems = info.WinnerRewardAmount * collectedNFTsCount;
			var take = (uint) Math.Ceiling(takeForCollectedItems * percent);
			var withdrawn = (int) Math.Min(info.CurrentAmount, take);

			if (withdrawn > 0)
			{
				rewards.Add(new RewardData(GameId.CS, withdrawn));
			}
		}

		private void CalculateBPPReward(ICollection<RewardData> rewards, MatchRewardConfig rewardConfig)
		{
			if (rewardConfig.Rewards.TryGetValue(GameId.BPP, out var amount))
			{
				var info = GameLogic.ResourceLogic.GetResourcePoolInfo(GameId.BPP);
				var withdrawn = (int) Math.Min(info.CurrentAmount, amount);
				var remainingPoints = GameLogic.BattlePassLogic.GetRemainingPointsOfBp();

				withdrawn = (int) Math.Min(withdrawn, remainingPoints);

				if (withdrawn > 0)
				{
					rewards.Add(new RewardData(GameId.BPP, withdrawn));
				}
			}
		}

		private void CalculateTrophiesReward(ICollection<RewardData> rewards,
										IReadOnlyCollection<QuantumPlayerMatchData> players,
										QuantumPlayerMatchData localPlayerData,
										TrophyRewardConfig rewardConfig,
										out int trophyChangeOut)
		{
			trophyChangeOut = 0;

			var playerTrophies = localPlayerData.Data.PlayerTrophies;
			var bracket = 0;
			foreach (var rewardBracket in rewardConfig.BracketReward)
			{
				if (playerTrophies > rewardBracket.Key)
				{
					continue;
				}
				bracket = rewardBracket.Key;
				break;
			}
			
			if (rewardConfig.BracketReward.TryGetValue(bracket, out var amount))
			{
				var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();
				
				var killsMade = (int)localPlayerData.Data.PlayersKilledCount;
				var finalTrophyChange = amount + (int)Math.Floor(gameConfig.TrophiesPerKill.AsDouble * killsMade);
				
				if (finalTrophyChange < 0 && Math.Abs(finalTrophyChange) > Data.Trophies)
				{
					finalTrophyChange = (int) -Data.Trophies;
				}
				
				trophyChangeOut = finalTrophyChange;
				rewards.Add(new RewardData(GameId.Trophies, finalTrophyChange));
			}
			
			// The logic below is left here DELIBERATELY
			// We will reuse it a bit later to calculate MMR which we will potentially keep hidden

			// var tempPlayers = new List<QuantumPlayerMatchData>(players);
			// tempPlayers.SortByPlayerRank(false);
			//
			// var trophyChange = 0d;
			//
			// // Losses; Note: PlayerRank starts from 1, not from 0
			// for (var i = 0; i < localPlayerData.PlayerRank - 1; i++)
			// {
			// 	trophyChange += CalculateEloChange(0d, tempPlayers[i].Data.PlayerTrophies,
			// 		localPlayerData.Data.PlayerTrophies, gameConfig.TrophyEloRange,
			// 		gameConfig.TrophyEloK.AsDouble, gameConfig.TrophyMinChange.AsDouble);
			// }
			//
			// // Wins; Note: PlayerRank starts from 1, not from 0
			// for (var i = (int) localPlayerData.PlayerRank; i < players.Count; i++)
			// {
			// 	trophyChange += CalculateEloChange(1d, tempPlayers[i].Data.PlayerTrophies,
			// 		localPlayerData.Data.PlayerTrophies, gameConfig.TrophyEloRange,
			// 		gameConfig.TrophyEloK.AsDouble, gameConfig.TrophyMinChange.AsDouble);
			// }
		}

		private double CalculateEloChange(double score, uint trophiesOpponent, uint trophiesPlayer, int eloRange,
										  double eloK, double minTrophyChange)
		{
			var eloBracket = Math.Pow(10, ((int) trophiesOpponent - (int) trophiesPlayer) / (double) eloRange);
			var trophyChange = eloK * (score - 1 / (1 + eloBracket));

			return trophyChange < 0 ? Math.Min(trophyChange, -minTrophyChange) : Math.Max(trophyChange, minTrophyChange);
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
