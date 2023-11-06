using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Messages;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// Defines the type of the battle pass
	/// </summary>
	public enum PassType
	{
		Free,
		Paid
	}

	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's battle pass points, levels, and rewards.
	/// </summary>
	public interface IBattlePassDataProvider
	{
		/// <summary>
		/// The current BP level.
		/// </summary>
		IObservableFieldReader<uint> CurrentLevel { get; }

		/// <summary>
		/// The current amount BP points the player has. These can be redeemed for levels.
		/// </summary>
		IObservableFieldReader<uint> CurrentPoints { get; }

		/// <summary>
		/// Gets the level and points that would be if current BPP was redeemed
		/// </summary>
		Tuple<uint, uint> GetPredictedLevelAndPoints(int pointOverride = -1);

		/// <summary>
		/// Gets the amount of points requried to complete the specified level
		/// </summary>
		uint GetRequiredPointsForLevel(int desiredLevel = -1);

		/// <summary>
		/// The maximum (highest) level of the BattlePass.
		/// </summary>
		uint MaxLevel { get; }

		/// <summary>
		/// Returns how many points the player is able to earn, before reaching max level (this takes
		/// into accounts the points they already have)
		/// </summary>
		uint GetRemainingPointsOfBp();

		/// <summary>
		/// Returns the rewards received for a particular level.
		/// </summary>
		EquipmentRewardConfig GetRewardForLevel(uint level, PassType type);

		/// <summary>
		/// Tells you if there are any points to redeem for levels and rewards, and gives you required points for
		/// the next level.
		/// </summary>
		bool HasUnclaimedRewards(int pointOverride = -1);

		/// <summary>
		/// Obtains the current desired battle pass config
		/// CAN BE NULL
		/// </summary>
		BattlePassConfig.BattlePassSeasonWrapper GetCurrentSeasonConfig();

		/// <summary>
		/// Gets all the available rewards to be claimed
		/// </summary>
		List<EquipmentRewardConfig> GetRewardConfigs(IEnumerable<uint> levels, PassType type);

		/// <summary>
		/// Gets a list of all levels that can be claimed
		/// </summary>
		List<uint> GetClaimableLevels(out uint points, PassType type);

		/// <summary>
		/// Checks if a specific BP season has been purchased. Use default / -1 for the current season.
		/// </summary>
		bool HasPurchasedSeason(int season = -1);

		/// <summary>
		/// Sets the last level the player claimed rewards for the given pass
		/// </summary>
		void SetLastLevelClaimed(uint lastLevel, PassType type);

		/// <summary>
		/// Checks if a given reward for a given pass type is claimable
		/// </summary>
		bool IsRewardClaimable(uint predictedLevel, uint rewardLevel, PassType passType);

		/// <summary>
		/// Checks if a given reward is claimed already
		/// </summary>
		bool IsRewardClaimed(uint rewardLevel, PassType passType);

		/// <summary>
		/// Claims all available battle pass points for the given pass.
		/// This will cause the battle pass to level up.
		/// Will return all available rewards for doing the claim but won't give those rewards.
		/// </summary>
		IReadOnlyCollection<EquipmentRewardConfig> ClaimBattlePassPoints(PassType type);

		/// <summary>
		/// Get all uncollected rewards from previous battle passes, this do NOT marks them as collected
		/// </summary>
		/// <returns></returns>
		IReadOnlyCollection<EquipmentRewardConfig> GetUncollectedRewardsFromPreviousSeasons();

		/// <summary>
		/// Check if the player has any uncollected rewards from previous battlepasses
		/// </summary>
		/// <returns></returns>
		bool HasUncollectedRewardsFromPreviousSeasons();

		/// <summary>
		/// Checks if the season is not initialized (aka do not have the key on the Seasons dictionary)
		/// </summary>
		/// <returns></returns>
		bool ShouldInitializeSeason();
	}

	/// <inheritdoc />
	public interface IBattlePassLogic : IBattlePassDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="amount"/> of BattlePass points to the Player.
		/// </summary>
		void AddBPP(uint amount);
		
		/// <summary>
		/// Advances battle pass level to the given level with the given remaining points
		/// </summary>
		void SetLevelAndPoints(uint level, uint points);
		
		/// <summary>
		/// Purchase the pro level of the current season of BattlePass.
		/// </summary>
		bool Purchase();

		/// <summary>
		/// Resets battle pass to original state
		/// </summary>
		void Reset();

		/// <summary>
		/// Mark all previous seasons as claimed
		/// </summary>
		void MarkRewardsFromPreviousSeasonsAsClaimed();

		/// <summary>
		/// Initialize season if needed
		/// </summary>
		void InitializeSeason();
	}

	public class BattlePassLogic : AbstractBaseLogic<BattlePassData>, IBattlePassLogic, IGameLogicInitializer
	{
		/// <summary>
		/// Cached local season number, this is to avoid mixing multiple seasons in the same execution of the app
		/// This may not be necessary but i'm afraid of behaviour on the change of seasons ( like seasons changing while the screen is open)
		/// This way we just display that the season ended and you need to restart the app or something else
		/// </summary>
		private BattlePassSeasonLogic _currentSeason;

		private IObservableField<uint> _currentLevel;
		private IObservableField<uint> _currentPoints;


		public IObservableFieldReader<uint> CurrentLevel => _currentLevel;

		public IObservableFieldReader<uint> CurrentPoints => _currentPoints;


		public uint MaxLevel => _currentSeason.MaxLevel;

		public BattlePassLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			SetCurrentSeasonToNow();
			_currentLevel = new ObservableResolverField<uint>(() => GetCurrentSeasonData().Level, val => GetCurrentSeasonData().Level = val);
			_currentPoints = new ObservableResolverField<uint>(() => GetCurrentSeasonData().Points, val => GetCurrentSeasonData().Points = val);
		}


		public void ReInit()
		{
			SetCurrentSeasonToNow();
			var listeners = _currentLevel.GetObservers();
			_currentPoints = new ObservableResolverField<uint>(() => GetCurrentSeasonData().Points, val => GetCurrentSeasonData().Points = val);
			_currentLevel.AddObservers(listeners);
			_currentLevel.InvokeUpdate();
			_currentPoints.InvokeUpdate();
		}

		private void SetCurrentSeasonToNow()
		{
			var cfg = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			var season = cfg.GetSeasonAt(DateTime.Now);
			if (season != null)
			{
				_currentSeason = new BattlePassSeasonLogic(this, season.Season.Number);
			}
		}

		private BattlePassSeasonData GetCurrentSeasonData()
		{
			return _currentSeason.GetData();
		}

		public BattlePassConfig.BattlePassSeasonWrapper GetCurrentSeasonConfig()
		{
			return _currentSeason.GetConfig();
		}

		public void MarkRewardsFromPreviousSeasonsAsClaimed()
		{
			foreach (var (season, data) in Data.Seasons)
			{
				if (season < _currentSeason.Season && !data.Claimed)
				{
					var wrapper = new BattlePassSeasonLogic(this, season);
					var (newLevel, newPoints) = wrapper.GetPredictedLevelAndPoints();
					data.Points = newPoints;
					data.Level = newLevel;
					data.LastLevelsClaimed[PassType.Free] = newLevel;
					data.LastLevelsClaimed[PassType.Paid] = newLevel;
					data.Claimed = true;
				}
			}
		}

		public void InitializeSeason()
		{
			if (ShouldInitializeSeason())
			{
				Data.Seasons.Add(_currentSeason.Season, new BattlePassSeasonData());
			}
		}

		public bool HasUncollectedRewardsFromPreviousSeasons()
		{
			return Data.Seasons.Where(kv => kv.Key < _currentSeason.Season)
				.Any(data => !data.Value.Claimed);
		}

		public bool ShouldInitializeSeason()
		{
			return !Data.Seasons.ContainsKey(_currentSeason.Season);
		}

		public IReadOnlyCollection<EquipmentRewardConfig> GetUncollectedRewardsFromPreviousSeasons()
		{
			var rewards = new List<EquipmentRewardConfig>();
			foreach (var (season, data) in Data.Seasons)
			{
				if (season < _currentSeason.Season && !data.Claimed)
				{
					var wrapper = new BattlePassSeasonLogic(this, season);
					var free = wrapper.GetClaimableRewards(PassType.Free);
					rewards.AddRange(free);

					if (data.Purchased)
					{
						var paid = wrapper.GetClaimableRewards(PassType.Paid);
						rewards.AddRange(paid);
					}
				}
			}

			return rewards;
		}

		public bool IsRewardClaimable(uint predictedLevel, uint rewardLevel, PassType passType)
		{
			return predictedLevel >= rewardLevel && GetCurrentSeasonData().LastLevelsClaimed[passType] < rewardLevel;
		}

		public bool IsRewardClaimed(uint rewardLevel, PassType passType)
		{
			return GetCurrentSeasonData().LastLevelsClaimed[passType] >= rewardLevel;
		}

		public void SetLevelAndPoints(uint level, uint points)
		{
			_currentLevel.Value = level;
			_currentPoints.Value = points;
		}

		public void SetLastLevelClaimed(uint lastLevel, PassType type)
		{
			GetCurrentSeasonData().LastLevelsClaimed[type] = lastLevel;
		}

		public IReadOnlyCollection<EquipmentRewardConfig> ClaimBattlePassPoints(PassType type)
		{
			var levelsCompleted = GetClaimableLevels(out var points, type);
			if (levelsCompleted.Count == 0) return Array.Empty<EquipmentRewardConfig>();
			var newLevel = levelsCompleted.Max();
			SetLevelAndPoints(newLevel, points);
			SetLastLevelClaimed(newLevel, type);
			return GetRewardConfigs(levelsCompleted, type);
		}


		public EquipmentRewardConfig GetRewardForLevel(uint level, PassType type)
		{
			return _currentSeason.GetRewardForLevel(level, type);
		}


		public uint GetRemainingPointsOfBp()
		{
			var predictedProgress = GetPredictedLevelAndPoints();
			var maxAvailablePoints = (uint) 0;
			var totalAccumulatedPoints = (uint) 0;
			for (int i = 0; i < MaxLevel; i++)
			{
				maxAvailablePoints += GetRequiredPointsForLevel(i);
			}

			for (int i = 0; i <= (int) predictedProgress.Item1; i++)
			{
				var ptsPerLevel = GetRequiredPointsForLevel(i);
				if (i < predictedProgress.Item1)
				{
					totalAccumulatedPoints += ptsPerLevel;
				}
				else
				{
					totalAccumulatedPoints += predictedProgress.Item2;
				}
			}

			return maxAvailablePoints - totalAccumulatedPoints;
		}

		public bool Purchase()
		{
			var config = GetCurrentSeasonConfig();
			var currentBB = GameLogic.CurrencyLogic.GetCurrencyAmount(GameId.BlastBuck);
			var data = GetCurrentSeasonData();
			if (data.Purchased || config.Season.Price > currentBB)
			{
				return false;
			}

			GameLogic.CurrencyLogic.DeductCurrency(GameId.BlastBuck, config.Season.Price);
			data.Purchased = true;
			GameLogic.MessageBrokerService.Publish(new BattlePassPurchasedMessage());
			return true;
		}

		public bool HasPurchasedSeason(int season = -1)
		{
			var config = GetCurrentSeasonConfig();
			var checkSeason = season < 0 ? config.Season.Number : (uint) season;
			var data = Data.GetSeason(checkSeason);
			return data.Purchased;
		}

		public void Reset()
		{
			_currentPoints.Value = 0;
			_currentLevel.Value = 0;
		}


		public bool HasUnclaimedRewards(int pointOverride = -1)
		{
			int points = pointOverride >= 0 ? pointOverride : (int) _currentPoints.Value;
			var wouldLevelUp = points >= GetRequiredPointsForLevel((int) _currentLevel.Value);
			var hasPaidRewards = _currentLevel.Value > GetCurrentSeasonData().LastLevelsClaimed[PassType.Paid];
			var hasFreeRewards = _currentLevel.Value > GetCurrentSeasonData().LastLevelsClaimed[PassType.Free];
			return (_currentLevel.Value < MaxLevel && wouldLevelUp) || (hasPaidRewards || hasFreeRewards);
		}

		public void AddBPP(uint amount)
		{
			amount = Math.Min(GetRemainingPointsOfBp(), amount);
			if (amount > 0) _currentPoints.Value += amount;
		}

		public List<uint> GetClaimableLevels(out uint points, PassType type)
		{
			return _currentSeason.GetClaimableLevels(out points, type);
		}


		public Tuple<uint, uint> GetPredictedLevelAndPoints(int pointOverride = -1)
		{
			return _currentSeason.GetPredictedLevelAndPoints(pointOverride);
		}

		public uint GetRequiredPointsForLevel(int desiredLevel = -1)
		{
			return _currentSeason.GetRequiredPointsForLevel(desiredLevel);
		}

		public List<EquipmentRewardConfig> GetRewardConfigs(IEnumerable<uint> levels, PassType type)
		{
			return _currentSeason.GetRewardConfigs(levels, type);
		}

		/// <summary>
		/// This class allows us to execute battlepass logic for past seasons, so we can give the rewards retroactively.
		/// </summary>
		public class BattlePassSeasonLogic
		{
			private readonly BattlePassLogic _logic;
			private readonly uint _season;

			public uint MaxLevel
			{
				get
				{
					var levelsCount = GetConfig()?.Levels.Count;
					if (levelsCount != null) return (uint) levelsCount;
					return 0;
				}
			}

			public uint Season => _season;

			public BattlePassSeasonLogic(BattlePassLogic battlePassLogic, uint seasonNumber)
			{
				_logic = battlePassLogic;
				_season = seasonNumber;
			}


			public BattlePassSeasonData GetData()
			{
				return _logic.Data.GetSeason(_season);
			}

			public List<EquipmentRewardConfig> GetRewardConfigs(IEnumerable<uint> levels, PassType type)
			{
				var rewards = new List<EquipmentRewardConfig>();

				foreach (var level in levels)
				{
					var levelRewards = GetRewardForLevel(level, type);
					if (!levelRewards.IsValid()) continue;
					rewards.Add(levelRewards);
				}

				return rewards;
			}


			public EquipmentRewardConfig GetRewardForLevel(uint level, PassType passType)
			{
				var config = GetConfig();
				var levelConfig = config.Levels[(int) level - 1];
				var rewardId = passType == PassType.Free ? levelConfig.RewardId : levelConfig.PremiumRewardId;
				if (rewardId < 0) return default;
				return _logic.GameLogic.ConfigsProvider.GetConfig<EquipmentRewardConfig>(rewardId);
			}

			public IReadOnlyCollection<EquipmentRewardConfig> GetClaimableRewards(PassType type)
			{
				var levelsCompleted = GetClaimableLevels(out var points, type);
				if (levelsCompleted.Count == 0) return Array.Empty<EquipmentRewardConfig>();
				return GetRewardConfigs(levelsCompleted, type);
			}

			public List<uint> GetClaimableLevels(out uint points, PassType type)
			{
				var prediction = GetPredictedLevelAndPoints();
				var predictedNewLevel = prediction.Item1;
				var lastClaimedLevel = GetData().LastLevelsClaimed[type];
				var unclaimedLevels = new List<uint>();
				for (var claimable = lastClaimedLevel + 1; claimable <= predictedNewLevel; claimable++)
				{
					unclaimedLevels.Add(claimable);
				}

				points = prediction.Item2;
				return unclaimedLevels;
			}

			/// <returns>Tuple with level,points</returns>
			public Tuple<uint, uint> GetPredictedLevelAndPoints(int pointOverride = -1)
			{
				var level = GetData().Level;
				var points = pointOverride >= 0 ? (uint) pointOverride : GetData().Points;
				var currentLevelPoints = GetRequiredPointsForLevel((int) level);
				while (points >= currentLevelPoints)
				{
					points -= currentLevelPoints;
					level++;
					if (level >= MaxLevel)
					{
						break;
					}

					currentLevelPoints = GetRequiredPointsForLevel((int) level);
				}

				return new Tuple<uint, uint>(level, points);
			}

			public uint GetRequiredPointsForLevel(int desiredLevel)
			{
				var config = GetConfig();
				if (desiredLevel >= MaxLevel)
				{
					return 0;
				}

				if (desiredLevel < 0)
				{
					desiredLevel = (int) GetData().Level;
				}

				var levelConfig = config.Levels[desiredLevel];
				//if the points for next is 0, then use default value, otherwise use custom level value
				return levelConfig.PointsForNextLevel == 0 ? config.Season.DefaultPointsPerLevel : levelConfig.PointsForNextLevel;
			}

			public BattlePassConfig.BattlePassSeasonWrapper GetConfig()
			{
				var cfg = _logic.GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
				return cfg.GetSeason(_season);
			}
		}
	}
}