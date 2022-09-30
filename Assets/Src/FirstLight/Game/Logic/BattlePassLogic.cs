using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Services;
using Quantum;

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
		Tuple<uint, uint> GetPredictedLevelAndPoints();

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
		BattlePassRewardConfig GetRewardForLevel(uint level);

		/// <summary>
		/// Tells you if there are any points to redeem for levels and rewards, and gives you required points for
		/// the next level.
		/// </summary>
		bool IsRedeemable(out uint nextLevelPoints);
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
		void AddLevels(uint amount, out List<Equipment> rewards, out uint newLevel);

		/// <summary>
		/// Converts the BattlePass Points to levels and rewards. Returns true if there was a level increase.
		/// </summary>
		bool RedeemBPP(out List<Equipment> rewards, out uint newLevel);

		/// <summary>
		/// Resets battlepass level and points back to 0
		/// </summary>
		void ResetBattlePass();
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

		public Tuple<uint, uint> GetPredictedLevelAndPoints()
		{
			var level = _currentLevel.Value;
			var points = _currentPoints.Value;

			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();

			while (points >= config.PointsPerLevel)
			{
				points -= config.PointsPerLevel;
				level++;
			}

			return new Tuple<uint, uint>(level,points);
		}

		public uint GetRemainingPoints()
		{
			var levelsTillMax = MaxLevel - _currentLevel.Value;
			var ppl = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>().PointsPerLevel;

			var points = (int) levelsTillMax * (int) ppl - (int) _currentPoints.Value;

			return (uint) Math.Max(0, points);
		}

		public BattlePassRewardConfig GetRewardForLevel(uint level)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			var levelConfig = config.Levels[(int) level - 1];

			return GameLogic.ConfigsProvider.GetConfig<BattlePassRewardConfig>(levelConfig.RewardId);
		}

		public bool IsRedeemable(out uint nextLevelPoints)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();

			nextLevelPoints = config.PointsPerLevel;

			return config.PointsPerLevel <= _currentPoints.Value;
		}

		public void AddBPP(uint amount)
		{
			amount = Math.Min(GetRemainingPoints(), amount);

			if (amount > 0)
			{
				_currentPoints.Value += amount;
			}
		}

		public void AddLevels(uint amount, out List<Equipment> rewards, out uint newLevel)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();

			AddBPP(config.PointsPerLevel * amount);
			RedeemBPP(out rewards, out newLevel);
		}

		public bool RedeemBPP(out List<Equipment> rewards, out uint newLevel)
		{
			var level = _currentLevel.Value;
			var points = _currentPoints.Value;

			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();

			var levels = new List<BattlePassRewardConfig>();

			while (points >= config.PointsPerLevel)
			{
				points -= config.PointsPerLevel;
				level++;

				var rewardConfig = GameLogic.ConfigsProvider.GetConfig<BattlePassRewardConfig>(config.Levels[(int) level - 1].RewardId);

				levels.Add(rewardConfig);
			}

			_currentLevel.Value = level;
			_currentPoints.Value = points;
			
			newLevel = level;

			RedeemBPRewards(levels, out rewards);

			return levels.Count > 0;
		}

		public void ResetBattlePass()
		{
			_currentLevel.Value = 0;
			_currentPoints.Value = 0;
		}

		private void RedeemBPRewards(List<BattlePassRewardConfig> rewardConfigs, out List<Equipment> rewards)
		{
			rewards = new List<Equipment>();
			
			foreach (var reward in rewardConfigs)
			{
				var generatedEquipment = GameLogic.EquipmentLogic.GenerateEquipmentFromBattlePassReward(reward);
				GameLogic.EquipmentLogic.AddToInventory(generatedEquipment);
				rewards.Add(generatedEquipment);
			}
		}
	}
}