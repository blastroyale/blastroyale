using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Server.SDK.Models;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using static Cinemachine.DocumentationSortingAttribute;

namespace FirstLight.Game.Logic
{
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
		uint GetRequiredPointsForLevel(int desiredLevel);

		/// <summary>
		/// The maximum (highest) level of the BattlePass.
		/// </summary>
		uint MaxLevel { get; }

		/// <summary>
		/// Returns how many points the player is able to earn, before reaching max level (this takes
		/// into accounts the points they already have)
		/// </summary>
		uint GetRemainingPoints();

		/// <summary>
		/// Returns the rewards received for a particular level.
		/// </summary>
		EquipmentRewardConfig GetRewardForLevel(uint level);

		/// <summary>
		/// Tells you if there are any points to redeem for levels and rewards, and gives you required points for
		/// the next level.
		/// </summary>
		bool IsRedeemable(int pointOverride = -1);

		/// <summary>
		/// Obtains the current desired battle pass config
		/// </summary>
		BattlePassConfig GetBattlePassConfig();

		/// <summary>
		/// Returns true if the given battle pass is the tutorial one
		/// </summary>
		bool IsTutorial();
	}

	/// <inheritdoc />
	public interface IBattlePassLogic : IBattlePassDataProvider
	{
		/// <summary>
		/// Adds the given <paramref name="amount"/> of BattlePass points to the Player.
		/// </summary>
		void AddBPP(uint amount);

		/// <summary>
		/// Adds <paramref name="amount"/> of levels to the player's current level, and redeems rewards for it.
		/// </summary>
		void AddLevels(uint amount, out List<KeyValuePair<UniqueId,Equipment>> rewards, out uint newLevel);

		/// <summary>
		/// Converts the BattlePass Points to levels and rewards. Returns true if there was a level increase.
		/// </summary>
		/// TODO: Use the reward logic & commands to award the blast rewards
		bool RedeemBPP(out List<KeyValuePair<UniqueId,Equipment>> rewards, out uint newLevel);

		/// <summary>
		/// Resets battle pass to original state
		/// </summary>
		void Reset();
	}

	public class BattlePassLogic : AbstractBaseLogic<PlayerData>, IBattlePassLogic, IGameLogicInitializer
	{
		private IObservableField<uint> _currentLevel;
		private IObservableField<uint> _currentPoints;
		
		public IObservableFieldReader<uint> CurrentLevel => _currentLevel;
		
		public IObservableFieldReader<uint> CurrentPoints => _currentPoints;

		public uint MaxLevel => (uint)GetBattlePassConfig().Levels.Count;
		
		public BattlePassLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}
		
		public void Init()
		{
			_currentLevel = new ObservableResolverField<uint>(() => Data.BPLevel, val => Data.BPLevel = val);
			_currentPoints = new ObservableResolverField<uint>(() => Data.BPPoints, val => Data.BPPoints = val);
		}

		public Tuple<uint, uint> GetPredictedLevelAndPoints(int pointOverride = -1)
		{
			var level = _currentLevel.Value;
			var points = pointOverride >= 0 ? (uint) pointOverride : _currentPoints.Value;
			var currentLevelPoints = GetRequiredPointsForLevel((int)level);

			while (points >= currentLevelPoints)
			{
				points -= currentLevelPoints;
				level++;

				if (level >= MaxLevel)
				{
					break;
				}
				
				currentLevelPoints = GetRequiredPointsForLevel((int)level);
			}

			return new Tuple<uint, uint>(level,points);
		}

		public uint GetRemainingPoints()
		{
			var predictedProgress = GetPredictedLevelAndPoints();
			var maxAvailablePoints = (uint)0;
			var totalAccumulatedPoints = (uint)0;
			
			for (int i = 0; i < MaxLevel; i++)
			{
				maxAvailablePoints += GetRequiredPointsForLevel(i);
			}

			for (int i = 0; i <= (int)predictedProgress.Item1; i++)
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
		
		public BattlePassConfig GetBattlePassConfig()
		{
			if (!GameLogic.PlayerDataProvider.HasTutorialSection(TutorialSection.TUTORIAL_BP))
			{
				return GameLogic.ConfigsProvider.GetConfig<TutorialBattlePassConfig>().ToBattlePassConfig();
			}
			else
			{
				return GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			}
		}

		public void Reset()
		{
			_currentPoints.Value = 0;
			_currentLevel.Value = 0;
		}

		public EquipmentRewardConfig GetRewardForLevel(uint level)
		{
			var config = GetBattlePassConfig();
			var levelConfig = config.Levels[(int) level - 1];

			return GameLogic.ConfigsProvider.GetConfig<EquipmentRewardConfig>(levelConfig.RewardId);
		}

		public bool IsRedeemable(int pointOverride = -1)
		{
			int points = pointOverride >= 0 ? pointOverride : (int) _currentPoints.Value;
			return _currentLevel.Value < MaxLevel && points >= GetRequiredPointsForLevel((int)_currentLevel.Value);
		}

		public void AddBPP(uint amount)
		{
			amount = Math.Min(GetRemainingPoints(), amount);

			if (amount > 0)
			{
				_currentPoints.Value += amount;
			}
		}

		public void AddLevels(uint amount, out List<KeyValuePair<UniqueId,Equipment>> rewards, out uint newLevel)
		{
			AddBPP(GetRequiredPointsForLevel((int)_currentLevel.Value) * amount);
			RedeemBPP(out rewards, out newLevel);
		}
		
		public bool RedeemBPP(out List<KeyValuePair<UniqueId,Equipment>> rewards, out uint newLevel)
		{
			var level = _currentLevel.Value;
			var points = _currentPoints.Value;
			var config = GetBattlePassConfig();
			var levels = new List<EquipmentRewardConfig>();

			var currentPointsPerLevel = GetRequiredPointsForLevel((int)_currentLevel.Value);

			while (points >= currentPointsPerLevel && level < MaxLevel)
			{
				points -= currentPointsPerLevel;
				level++;

				var rewardConfig = GameLogic.ConfigsProvider.GetConfig<EquipmentRewardConfig>(config.Levels[(int) level - 1].RewardId);

				levels.Add(rewardConfig);

				currentPointsPerLevel = GetRequiredPointsForLevel((int)level);
			}

			_currentLevel.Value = level;
			_currentPoints.Value = points;
			
			newLevel = level;

			RedeemBPRewards(levels, out rewards);

			return levels.Count > 0;
		}

		private void RedeemBPRewards(List<EquipmentRewardConfig> rewardConfigs, out List<KeyValuePair<UniqueId,Equipment>> rewards)
		{
			rewards = new List<KeyValuePair<UniqueId,Equipment>>();
			
			foreach (var reward in rewardConfigs)
			{
				var generatedEquipment = GameLogic.EquipmentLogic.GenerateEquipmentFromConfig(reward);
				var uniqueId = GameLogic.EquipmentLogic.AddToInventory(generatedEquipment);
				rewards.Add(new KeyValuePair<UniqueId, Equipment>(uniqueId, generatedEquipment));
			}
		}

		public bool IsTutorial()
		{
			return !GameLogic.PlayerDataProvider.HasTutorialSection(TutorialSection.TUTORIAL_BP);
		}

		public uint GetRequiredPointsForLevel(int desiredLevel)
		{
			var config = GetBattlePassConfig();

			if (desiredLevel >= MaxLevel)
			{
				return 0;
			}
			
			var levelConfig = config.Levels[desiredLevel];

			//if the points for next is 0, then use default value, otherwise use custom level value
			return levelConfig.PointsForNextLevel == 0 ?
				config.DefaultPointsPerLevel : levelConfig.PointsForNextLevel;
		}
	}
}
