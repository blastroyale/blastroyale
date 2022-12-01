using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
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
	}

	public class BattlePassLogic : AbstractBaseLogic<PlayerData>, IBattlePassLogic, IGameLogicInitializer
	{
		private IObservableField<uint> _currentLevel;
		private IObservableField<uint> _currentPoints;
		
		public IObservableFieldReader<uint> CurrentLevel => _currentLevel;
		
		public IObservableFieldReader<uint> CurrentPoints => _currentPoints;
		
		public uint MaxLevel { get; private set; }
		
		public BattlePassLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}
		
		public void Init()
		{
			MaxLevel = (uint) GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>().Levels.Count;
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
				currentLevelPoints = GetRequiredPointsForLevel((int)level);
			}

			return new Tuple<uint, uint>(level,points);
		}

		public uint GetRemainingPoints()
		{
			uint points = 0;	
			for (uint i = MaxLevel - 1; i > _currentLevel.Value; i--)
			{
				points += GetRequiredPointsForLevel((int)i);
			}

			return points;
		}

		public EquipmentRewardConfig GetRewardForLevel(uint level)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			var levelConfig = config.Levels[(int) level - 1];

			return GameLogic.ConfigsProvider.GetConfig<EquipmentRewardConfig>(levelConfig.RewardId);
		}

		public bool IsRedeemable(int pointOverride = -1)
		{
			int points = pointOverride >= 0 ? pointOverride : (int) _currentPoints.Value;
			return points >= GetRequiredPointsForLevel((int)_currentLevel.Value);
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
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			var levels = new List<EquipmentRewardConfig>();

			var currentPointsPerLevel = GetRequiredPointsForLevel((int)_currentLevel.Value);

			while (points >= currentPointsPerLevel)
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

		public uint GetRequiredPointsForLevel(int desiredLevel)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			var levelConfig = config.Levels[desiredLevel];

			//if the points for next is 0, then use default value, otherwise use custom level value
			return levelConfig.PointsForNextLevel == 0 ?
				config.DefaultPointsPerLevel : levelConfig.PointsForNextLevel;
		}
	}
}
