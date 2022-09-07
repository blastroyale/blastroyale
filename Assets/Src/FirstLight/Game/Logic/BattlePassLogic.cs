using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Services;

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
		/// Tells you if there are any points to redeem for levels and rewards.
		/// </summary>
		bool IsRedeemable();
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
		void AddLevels(uint amount, out List<BattlePassRewardConfig> rewards, out uint newLevel);

		/// <summary>
		/// Converts the BattlePass Points to levels and rewards. Returns true if there was a level increase.
		/// </summary>
		bool RedeemBPP(out List<BattlePassRewardConfig> rewards, out uint newLevel);
	}

	public class BattlePassLogic : AbstractBaseLogic<PlayerData>, IBattlePassLogic, IGameLogicInitializer
	{
		private IObservableField<uint> _currentLevel;
		private IObservableField<uint> _currentPoints;

		public IObservableFieldReader<uint> CurrentLevel => _currentLevel;
		public IObservableFieldReader<uint> CurrentPoints => _currentPoints;

		public BattlePassLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		public void Init()
		{
			_currentLevel = new ObservableResolverField<uint>(() => Data.BPLevel, val => Data.BPLevel = val);
			_currentPoints = new ObservableResolverField<uint>(() => Data.BPPoints, val => Data.BPPoints = val);
		}

		public bool IsRedeemable()
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();
			return config.PointsPerLevel < _currentPoints.Value;
		}

		public void AddBPP(uint amount)
		{
			_currentPoints.Value += amount;
		}

		public void AddLevels(uint amount, out List<BattlePassRewardConfig> rewards, out uint newLevel)
		{
			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();

			AddBPP(config.PointsPerLevel * amount);
			RedeemBPP(out rewards, out newLevel);
		}

		public bool RedeemBPP(out List<BattlePassRewardConfig> rewards, out uint newLevel)
		{
			var level = _currentLevel.Value;
			var points = _currentPoints.Value;

			var config = GameLogic.ConfigsProvider.GetConfig<BattlePassConfig>();

			var levels = new List<BattlePassRewardConfig>();

			while (points > config.PointsPerLevel)
			{
				points -= config.PointsPerLevel;
				level++;

				var rewardConfig =
					GameLogic.ConfigsProvider
					         .GetConfig<BattlePassRewardConfig>(config.Levels[(int) level - 1].RewardId);

				levels.Add(rewardConfig);
			}

			_currentLevel.Value = level;
			_currentPoints.Value = points;

			rewards = levels;
			newLevel = level;

			RedeemBPRewards(levels);

			return levels.Count > 0;
		}

		private void RedeemBPRewards(List<BattlePassRewardConfig> rewards)
		{
			foreach (var reward in rewards)
			{
				GameLogic.EquipmentLogic.AddToInventory(reward.Reward);
			}
		}
	}
}