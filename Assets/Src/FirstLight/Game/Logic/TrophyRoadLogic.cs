using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Services;
using Quantum;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This logic provides the necessary behaviour to manage the player's trophy road progress
	/// </summary>
	public interface ITrophyRoadDataProvider
	{
		/// <summary>
		/// Requests the current player's level <see cref="TrophyRoadRewardInfo"/>
		/// </summary>
		TrophyRoadRewardInfo CurrentLevelInfo { get; }

		/// <summary>
		/// Requests the <see cref="TrophyRoadRewardInfo"/> representing the given trophy road <paramref name="level"/>
		/// </summary>
		TrophyRoadRewardInfo GetInfo(uint level);
		
		/// <summary>
		/// Requests all the  <see cref="TrophyRoadRewardInfo"/> of the trophy road's progression until the given
		/// player's <paramref name="level"/>
		/// </summary>
		List<TrophyRoadRewardInfo> GetAllInfos(uint level);
		
		/// <summary>
		/// Requests all the  <see cref="TrophyRoadRewardInfo"/> of the player's trophy road progression
		/// </summary>
		List<TrophyRoadRewardInfo> GetAllInfos();
	}

	/// <inheritdoc />
	public interface ITrophyRoadLogic : ITrophyRoadDataProvider
	{
		/// <summary>
		/// Collects the player reward for the given <paramref name="level"/> and returns the <see cref="TrophyRoadRewardInfo"/>
		/// representation of the reward collected
		/// </summary>
		TrophyRoadRewardInfo CollectReward(uint level);
	}

	/// <inheritdoc cref="IRewardLogic"/>
	public class TrophyRoadLogic : AbstractBaseLogic<PlayerData>, ITrophyRoadLogic
	{
		/// <inheritdoc />
		public TrophyRoadRewardInfo CurrentLevelInfo => GetInfo(GameLogic.PlayerDataProvider.Level.Value);
		
		public TrophyRoadLogic(IGameLogic gameLogic, IDataProvider dataProvider) : base(gameLogic, dataProvider)
		{
		}

		/// <inheritdoc />
		public TrophyRoadRewardInfo GetInfo(uint level)
		{
			var configs = GameLogic.ConfigsProvider.GetConfigsDictionary<PlayerLevelConfig>();
			var config = configs[(int) level];
			var isCollected = Data.LevelRewardsCollected.Contains(level);
			var totalXp = 0u;

			for (var i = 1; i <= level; i++)
			{
				totalXp += configs[i].LevelUpXP;
			}
			
			var info = new TrophyRoadRewardInfo
			{
				Level = level,
				XpNeeded = totalXp,
				UnlockedSystems = config.Systems,
				IsCollected = isCollected,
				IsReadyToCollect = !isCollected && GameLogic.PlayerLogic.Level.Value > level,
				Reward = new RewardData(config.RewardId, config.RewardValue)
			};

			return info;
		}

		/// <inheritdoc />
		public List<TrophyRoadRewardInfo> GetAllInfos(uint level)
		{
			var infos = new List<TrophyRoadRewardInfo>();

			for (var i = 1u; i <= level; i++)
			{
				infos.Add(GetInfo(i));
			}

			return infos;
		}

		/// <inheritdoc />
		public List<TrophyRoadRewardInfo> GetAllInfos()
		{
			return GetAllInfos(GameLogic.PlayerLogic.CurrentLevelInfo.MaxLevel - 1);
		}

		/// <inheritdoc />
		public TrophyRoadRewardInfo CollectReward(uint level)
		{
			var info = GetInfo(level);

			if (info.IsCollected)
			{
				throw new LogicException($"The reward for the given level: {level} has already been collected");
			}
			
			Data.LevelRewardsCollected.Add(level);

			info.IsCollected = true;

			return info;
		}
	}
}