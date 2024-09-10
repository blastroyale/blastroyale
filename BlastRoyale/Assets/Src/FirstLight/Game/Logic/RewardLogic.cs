using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote.FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Models;
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
		public List<QuantumPlayerMatchData> MatchData { get; set; }

		/// <summary>
		/// Player Rank of the local player in MatchData array
		/// </summary>
		public int ExecutingPlayer { get; set; }

		/// <summary>
		/// Simulation match config
		/// </summary>
		public SimulationMatchConfig MatchConfig { get; set; }

		/// <summary>
		/// Items the player collected during the match
		/// </summary>
		public Dictionary<GameId, ushort> CollectedItems { get; set; }
	}

	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's rewards
	/// </summary>
	public interface IRewardDataProvider
	{
		/// <summary>
		/// Requests the list of rewards in buffer to be awarded to the player
		/// </summary>
		IObservableListReader<ItemData> UnclaimedRewards { get; }

		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="matchData"/> performance from a game completed
		/// </summary>
		List<ItemData> CalculateMatchRewards(RewardSource source, out int trophyChange);

		/// <summary>
		/// Obtains the rewards for a given tutorial step
		/// </summary>
		IEnumerable<ItemData> GetRewardsFromTutorial(TutorialSection section);

		/// <summary>
		/// Creates an item based on a reward config.
		/// The config is a Chest Like" structure that defines rules for item generation.
		/// </summary>
		ItemData CreateItemFromConfig(EquipmentRewardConfig config);
	}

	/// <inheritdoc />
	public interface IRewardLogic : IRewardDataProvider
	{
		/// <summary>
		/// Generate a list of rewards based on the players <paramref name="RewardSource"/> performance from a game completed
		/// </summary>
		List<ItemData> GiveMatchRewards(RewardSource source, out int trophyChange);

		/// <summary>
		/// Collects all the unclaimed rewards in the player's inventory
		/// </summary>
		List<ItemData> ClaimUnclaimedRewards();

		/// <summary>
		/// Claims specific uncolledted item. Will throw an exception if the user
		/// does not have the item as unclaimed reward.
		/// Returns UniqueId of equipment if generated
		/// </summary>
		ItemData ClaimUnclaimedReward(ItemData item);

		/// <summary>
		/// Generic item handler to give items to player as rewards
		/// If autoclaim is true then the rewards will be added instantly to inventory
		/// instead of the UnclaimedRewards inventory
		/// </summary>
		void Reward(IEnumerable<ItemData> items);

		/// <summary>
		/// Reward items to player but instead of adding the items to the player
		/// items directly, it will add to the player unclaimed rewards.
		/// Whenever player gets to main menu, it will claim all unclaimed rewards.
		/// This is to play animations even if the user quits the game.
		/// </summary>
		void RewardToUnclaimedRewards(IEnumerable<ItemData> items);

		/// <summary>
		/// Generates ItemData of all given configs.
		/// Those configs represents a "chest like" structure containing rules on how to generate items.
		/// </summary>
		IReadOnlyCollection<ItemData> CreateItemsFromConfigs(IEnumerable<EquipmentRewardConfig> configs);
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class RewardLogic : AbstractBaseLogic<PlayerData>, IRewardLogic, IGameLogicInitializer
	{
		public const int EXTRA_GAME_MODE_CHECK_MINUTES = 10;

		private IObservableList<ItemData> _unclaimedRewards;

		public IObservableListReader<ItemData> UnclaimedRewards => _unclaimedRewards;

		public RewardLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_unclaimedRewards = new ObservableList<ItemData>(Data.UncollectedRewards);
		}

		public void ReInit()
		{
			var listeners = _unclaimedRewards.GetObservers();
			_unclaimedRewards = new ObservableList<ItemData>(Data.UncollectedRewards);
			_unclaimedRewards.AddObservers(listeners);
			_unclaimedRewards.InvokeUpdate();
		}

		public static int GetModifiedReward(int currentValue, GameId id, SimulationMatchConfig config, bool CollectedInGame)
		{
			var mod = config.RewardModifiers.FirstOrDefault(m => m.Id == id && m.CollectedInsideGame == CollectedInGame);
			var mp = mod == null ? 1 : mod.Multiplier.AsDouble;
			return (int) Math.Floor(currentValue * mp);
		}

		public static bool TryGetRewardCurrencyGroupId(GameId rewardId, out GameId groupId)
		{
			if (rewardId.IsInGroup(GameIdGroup.NOOBTokens))
			{
				groupId = GameId.NOOB;
				return true;
			}
			
			groupId = rewardId;
			return false;
		}

		private bool IsDebug(SimulationMatchConfig matchConfig)
		{
			// This is checked on quantum plugin, rooms are not allowed to be created with this config
			return matchConfig.UniqueConfigId?.Contains("debug-") ?? false;
		}

		public List<SimulationMatchConfig> ValidMatchRewardConfigs()
		{
			var validConfigs = new List<SimulationMatchConfig>
			{
				GameLogic.ConfigsProvider.GetConfig<TutorialConfig>().SecondMatch
			};

			var configurableModes = GameLogic.RemoteConfigProvider
				.GetConfig<FixedGameModesConfig>().Select(fx => (IGameModeEntry) fx)
				.Concat(GameLogic.RemoteConfigProvider.GetConfig<EventGameModesConfig>().Select(a => (IGameModeEntry) a));
			var now = GameLogic.TimeService.DateTimeUtcNow;
			foreach (var gameModeEntry in configurableModes)
			{
				if (gameModeEntry is FixedGameModeEntry)
				{
					validConfigs.Add(gameModeEntry.MatchConfig);
					continue;
				}

				if (gameModeEntry is EventGameModeEntry ev)
				{
					foreach (var timedGameModeEntry in ev.Schedule)
					{
						// Lets add some time due to clock desyncs and matchs in progress
						var starts = timedGameModeEntry.GetStartsAtDateTime().AddMinutes(-EXTRA_GAME_MODE_CHECK_MINUTES);
						var endsAt = timedGameModeEntry.GetEndsAtDateTime().AddMinutes(EXTRA_GAME_MODE_CHECK_MINUTES);
						if (starts < now && endsAt > now)
						{
							validConfigs.Add(gameModeEntry.MatchConfig);
							break;
						}
					}
				}
			}

			return validConfigs;
		}

		private bool IsEventValid(RewardSource source, SimulationMatchConfig usedConfig)
		{
			// Config not valid someone probably trying to cheat
			if (usedConfig == null) return false;
			// Prevent sneaky players from sending different meta item drop overwrites from the config,
			// in this case the collected items from the simulation cannot be trusted
			if (source.MatchConfig.MetaItemDropOverwrites != null && !source.MatchConfig.MetaItemDropOverwrites.SequenceEqual(usedConfig.MetaItemDropOverwrites))
			{
				return false;
			}

			return true;
		}

		public List<ItemData> CalculateMatchRewards(RewardSource source, out int trophyChange)
		{
			var rewards = new List<ItemData>();
			var localMatchData = source.MatchData[source.ExecutingPlayer];
			trophyChange = 0;
			if (source.MatchConfig.MatchType == MatchType.Custom) return rewards;

			if (localMatchData.PlayerRank == 0)
			{
				throw new MatchDataEmptyLogicException();
			}

			var usedSimConfig = ValidMatchRewardConfigs().FirstOrDefault(valid => valid.UniqueConfigId == source.MatchConfig.UniqueConfigId);

			if ((!IsEventValid(source, usedSimConfig) && !IsDebug(source.MatchConfig)) || localMatchData.Data.KilledByBeingAFK)
			{
				return rewards;
			}

			if (IsDebug(source.MatchConfig))
			{
				usedSimConfig = source.MatchConfig;
			}

			var teamSize = Math.Max(1, usedSimConfig.TeamSize);
			var maxTeamsInMatch = source.GamePlayerCount / teamSize;

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
			var rankValue =
				(int) Math.Min(1 + Math.Floor(maxTeamsInMatch / (double) ((source.GamePlayerCount / teamSize) - 1) * (localMatchData.PlayerRank - 1)),
					maxTeamsInMatch);

			//clean this up
			foreach (var config in gameModeRewardConfigs)
			{
				if (teamSize == config.TeamSize && rankValue > config.Placement) break;
				if (config.Placement == rankValue && config.TeamSize == teamSize)
				{
					rewardConfig = config;
					break;
				}
			}

			foreach (var config in gameModeTrophyConfigs)
			{
				if (teamSize == config.TeamSize && rankValue > config.Placement) break;
				if (config.Placement == rankValue && config.TeamSize == teamSize)
				{
					trophyRewardConfig = config;
					break;
				}
			}

			// We dont give rewards for quitting, but players can loose trophies
			CalculateTrophiesReward(rewards, usedSimConfig, localMatchData, trophyRewardConfig, out trophyChange);
			if (source.DidPlayerQuit || source.GamePlayerCount == 1)
			{
				return rewards;
			}

			CalculateBPPReward(rewards, rewardConfig, usedSimConfig);
			CalculateXPReward(rewards, rewardConfig, usedSimConfig);
			CalculateCollectedRewards(rewards, source, usedSimConfig);
			return rewards;
		}

		private void CalculateCollectedRewards(List<ItemData> rewards, RewardSource source, SimulationMatchConfig simConfig)
		{
			if (source.CollectedItems == null || source.CollectedItems.Count == 0) return;

			var collected = new Dictionary<GameId, ushort>(source.CollectedItems);

			foreach (var reward in rewards)
			{
				if (collected.TryGetValue(reward.Id, out var collectedAmt))
				{
					var amount = GetModifiedReward(collectedAmt, reward.Id, simConfig, true);
					reward.GetMetadata<CurrencyMetadata>().Amount += amount;
					collected.Remove(reward.Id);
				}
			}

			foreach (var (id, amt) in collected)
			{
				var fixedAmount = GetModifiedReward(amt, id, simConfig, true);
				var rewardId = TryGetRewardCurrencyGroupId(id, out var groupId) ? groupId : id;
				
				rewards.Add(ItemFactory.Currency(rewardId, fixedAmount));
			}
		}

		public List<ItemData> GiveMatchRewards(RewardSource source, out int trophyChange)
		{
			var rewards = CalculateMatchRewards(source, out trophyChange);
			foreach (var reward in rewards)
			{
				var rewardData = reward;
				if (rewardData.Id.IsInGroup(GameIdGroup.ResourcePool) && rewardData.TryGetMetadata<CurrencyMetadata>(out var meta))
				{
					meta.Amount =
						(int) GameLogic.ResourceLogic.WithdrawFromResourcePool(reward.Id, (uint) meta.Amount);
				}
			}

			RewardToUnclaimedRewards(rewards);
			return rewards;
		}

		public ItemData ClaimUnclaimedReward(ItemData item)
		{
			if (!Data.UncollectedRewards.Remove(item)) throw new LogicException($"Could not claim reward {item}");
			return AddItemToPlayerInventory(item);
		}

		public List<ItemData> ClaimUnclaimedRewards()
		{
			var claimed = new List<ItemData>(Data.UncollectedRewards);
			foreach (var reward in claimed)
			{
				if (reward.Id == GameId.Random) Data.UncollectedRewards.Remove(reward);
				else ClaimUnclaimedReward(reward);
			}

			return claimed.Where(r => r.Id != GameId.Random).ToList();
		}

		public void RewardToUnclaimedRewards(IEnumerable<ItemData> items)
		{
			foreach (var item in items)
			{
				_unclaimedRewards.Add(item);
			}
		}

		public void Reward(IEnumerable<ItemData> items)
		{
			foreach (var item in items) AddItemToPlayerInventory(item);
		}

		public IEnumerable<ItemData> GetRewardsFromTutorial(TutorialSection section)
		{
			var tutorialRewardsCfg = GameLogic.ConfigsProvider.GetConfig<TutorialConfig>();
			var tutorialRewardsCount = tutorialRewardsCfg.Rewards.Count(c => c.Section == section);
			if (tutorialRewardsCount == 0) return Array.Empty<ItemData>();
			var rewardsCfg = GameLogic.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var rewardsConfigs = rewardsCfg.Where(c => tutorialRewardsCfg.Rewards.First(c => c.Section == section).RewardIds.Contains((uint) c.Id));
			var rewardItems = CreateItemsFromConfigs(rewardsConfigs);
			return rewardItems;
		}

		public ItemData CreateItemFromConfig(EquipmentRewardConfig config)
		{
			if (config.GameId.IsInGroup(GameIdGroup.Collection))
			{
				return ItemFactory.Collection(config.GameId);
			}

			if (config.GameId.IsInGroup(GameIdGroup.Core) || config.GameId.IsInGroup(GameIdGroup.Equipment))
			{
				// Cores/Equipments don't exist anymore so give some blastbucks instead
				return ItemFactory.Currency(GameId.BlastBuck, 5);
			}

			return ItemFactory.Currency(config.GameId, config.Amount);
		}

		public IReadOnlyCollection<ItemData> CreateItemsFromConfigs(IEnumerable<EquipmentRewardConfig> rewardConfigs)
		{
			var items = new List<ItemData>();
			foreach (var reward in rewardConfigs) items.Add(CreateItemFromConfig(reward));
			return items;
		}

		private ItemData OpenCoreById(GameId id)
		{
			var config = GameLogic.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>()
				.First(cfg => cfg.GameId == id);
			return CreateItemFromConfig(config);
		}

		private void CalculateBPPReward(ICollection<ItemData> rewards, MatchRewardConfig rewardConfig, SimulationMatchConfig simulationMatchConfig)
		{
			if (rewardConfig.Rewards.TryGetValue(GameId.BPP, out var amount))
			{
				amount = GetModifiedReward(amount, GameId.BPP, simulationMatchConfig, false);

				var info = GameLogic.ResourceLogic.GetResourcePoolInfo(GameId.BPP);
				var withdrawn = (int) Math.Min(info.CurrentAmount, amount);
				var remainingPoints = (int) GameLogic.BattlePassLogic.GetRemainingPointsOfBp();
				withdrawn = Math.Min(withdrawn, remainingPoints);
				if (withdrawn > 0)
				{
					rewards.Add(ItemFactory.Currency(GameId.BPP, withdrawn));
				}
			}
		}

		private void CalculateXPReward(ICollection<ItemData> rewards, MatchRewardConfig rewardConfig, SimulationMatchConfig usedSimConfig)
		{
			if (rewardConfig.Rewards.TryGetValue(GameId.XP, out var amount))
			{
				amount = GetModifiedReward(amount, GameId.XP, usedSimConfig, false);
				rewards.Add(ItemFactory.Currency(GameId.XP, amount));
			}
		}

		private void CalculateTrophiesReward(ICollection<ItemData> rewards,
											 SimulationMatchConfig simulationMatchConfig,
											 QuantumPlayerMatchData localPlayerData,
											 TrophyRewardConfig rewardConfig,
											 out int trophyChangeOut)
		{
			trophyChangeOut = 0;

			var playerTrophies = localPlayerData.Data.PlayerTrophies;
			var bracket = 0;
			var maxBracket = rewardConfig.BracketReward.Keys.Max();
			if (playerTrophies > maxBracket)
			{
				bracket = maxBracket;
			}
			else
			{
				bracket = rewardConfig.BracketReward.FirstOrDefault(kp => playerTrophies <= kp.Key).Key;
			}

			if (rewardConfig.BracketReward.TryGetValue(bracket, out var amount))
			{
				var gameConfig = GameLogic.ConfigsProvider.GetConfig<QuantumGameConfig>();

				var killsMade = (int) localPlayerData.Data.PlayersKilledCount;
				var finalTrophyChange = amount + (int) Math.Floor(gameConfig.TrophiesPerKill.AsDouble * killsMade);

				if (finalTrophyChange < 0 && Math.Abs(finalTrophyChange) > Data.Trophies)
				{
					finalTrophyChange = (int) -Data.Trophies;
				}

				finalTrophyChange = GetModifiedReward(finalTrophyChange, GameId.Trophies, simulationMatchConfig, false);
				trophyChangeOut = finalTrophyChange;
				rewards.Add(ItemFactory.Currency(GameId.Trophies, finalTrophyChange));
			}
		}

		// TODO: implement adapters
		private ItemData AddItemToPlayerInventory(ItemData reward)
		{
			GameLogic.MessageBrokerService.Publish(new ItemRewardedMessage(reward));
			if (reward.TryGetMetadata<EquipmentMetadata>(out var eqMeta))
			{
				GameLogic.EquipmentLogic.AddToInventory(eqMeta.Equipment);
			}
			else if (reward.TryGetMetadata<UnlockMetadata>(out var unlockMeta))
			{
				// unlocks dont need to do anything
			}
			else if (reward.Id.IsInGroup(GameIdGroup.Core)) // Cores auto-opens when added to inventory
			{
				var generated = OpenCoreById(reward.Id);
				AddItemToPlayerInventory(generated);
				GameLogic.MessageBrokerService.Publish(new OpenedCoreMessage()
				{
					Core = reward,
					Results = new[] {generated}
				});
				return generated;
			}
			else if (reward.Id.IsInGroup(GameIdGroup.Collection))
			{
				GameLogic.CollectionLogic.UnlockCollectionItem(reward);
			}
			else if (reward.TryGetMetadata<CurrencyMetadata>(out var currency))
			{
				if (reward.Id == GameId.XP)
				{
					GameLogic.PlayerLogic.AddXP((uint) currency.Amount);
				}
				else if (reward.Id == GameId.BPP)
				{
					GameLogic.BattlePassLogic.AddBPP((uint) currency.Amount);
				}
				else if (reward.Id == GameId.Trophies)
				{
					GameLogic.PlayerLogic.UpdateTrophies(currency.Amount);
				}
				else if (reward.Id.IsInGroup(GameIdGroup.Currency))
				{
					GameLogic.CurrencyLogic.AddCurrency(reward.Id, (uint) currency.Amount);
				}
				else throw new LogicException($"Unknown currency '{reward.Id.ToString()}'");
			}
			else throw new LogicException($"Unknown reward {reward}");

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