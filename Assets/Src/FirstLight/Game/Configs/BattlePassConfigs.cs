using System;
using System.Collections.Generic;
using System.Globalization;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct BattlePassConfig
	{
		public List<BattlePassSeason> Seasons;
		public List<BattlePassLevel> Levels;

		
		[Serializable]
		public struct BattlePassSeason
		{
			public uint Number;
			[Tooltip("The price of the Pro BP in BlastBucks")]
			public uint Price;
			public uint DefaultPointsPerLevel;
			/// <summary>
			/// Format dd/MM/yyyy
			/// </summary>
			public string StartsAt;
			public string EndsAt;
			public DateTime GetStartsAtDateTime() => DateTime.ParseExact(StartsAt, "d/M/yyyy", CultureInfo.InvariantCulture);
			public DateTime GetEndsAtDateTime() => DateTime.ParseExact(EndsAt, "d/M/yyyy", CultureInfo.InvariantCulture);
		}
		[Serializable]
		public struct BattlePassLevel
		{
			public int RewardId;
			public int PremiumRewardId;
			public uint PointsForNextLevel;
			public uint Season;
		}
		
		
		public class BattlePassSeasonWrapper
		{
			public BattlePassSeason Season;
			public List<BattlePassLevel> Levels;
		}

		public BattlePassSeasonWrapper GetSeasonAt(DateTime dateTime)
		{
			foreach (var battlePassSeason in Seasons)
			{
				if (battlePassSeason.GetStartsAtDateTime() < dateTime && battlePassSeason.GetEndsAtDateTime() > dateTime)
				{
					return new BattlePassSeasonWrapper()
					{
						Season = battlePassSeason,
						Levels = Levels.FindAll(lvl => lvl.Season == battlePassSeason.Number)
					};
				}
			}

			return null;
		}
		
		
		
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="BattlePassConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "BattlePassConfigs", menuName = "ScriptableObjects/Configs/BattlePassConfigs")]
	public class BattlePassConfigs : ScriptableObject, ISingleConfigContainer<BattlePassConfig>
	{
		[SerializeField] private BattlePassConfig _config;

		public BattlePassConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}