using System;
using System.Collections.Generic;
using System.Globalization;
using FirstLight.Game.Logic;
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

			public string Title;
			
			[Tooltip("The price of the Pro BP in BlastBucks")]
			public uint Price;

			[Tooltip("Buy level price, set 0 to disable functionality")]
			public uint BuyLevelPrice;

			public uint DefaultPointsPerLevel;

			/// <summary>
			/// Format dd/MM/yyyy
			/// </summary>
			public string StartsAt;

			public string EndsAt;
			public bool RemovePaid;
			public string EndGraphicImageClass;
			public string EndGraphicName;
			public string Highlighted;

			public DateTime GetStartsAtDateTime() => DateTime.ParseExact(StartsAt, "d/M/yyyy", CultureInfo.InvariantCulture);

			// Operations to get the last tick of the day
			public DateTime GetEndsAtDateTime() => DateTime.ParseExact(EndsAt, "d/M/yyyy", CultureInfo.InvariantCulture).Date.AddDays(1).AddTicks(-1);

			public List<(uint level, PassType passType)> GetHighlighted()
			{
				var highlighted = new List<(uint level, PassType passType)>();
				if (string.IsNullOrEmpty(Highlighted))
				{
					return highlighted;
				}

				foreach (var reward in Highlighted.Split(","))
				{
					var type = reward.Split(":");
					if (type.Length == 0) continue;

					if (!uint.TryParse(type[0], out var item))
					{
						continue;
					}

					var passType = type.Length == 2 && type[1].ToLowerInvariant() == "f" ? PassType.Free : PassType.Paid;
					highlighted.Add((item, passType));
				}

				return highlighted;
			}
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

		public BattlePassSeasonWrapper GetSeason(uint season)
		{
			foreach (var battlePassSeason in Seasons)
			{
				if (battlePassSeason.Number == season)
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