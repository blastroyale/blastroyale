using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using Quantum;

namespace FirstLight.Game.Data
{
	[Serializable]
	public class BattlePassSeasonData
	{
		public bool Purchased = false;

		/// <summary>
		/// Claimed all items after battlepass finisehd
		/// </summary>
		public bool Claimed = false;

		public uint Level = 0;
		public uint Points = 0;
		public bool SeenBanner;

		public Dictionary<PassType, uint> LastLevelsClaimed = new ()
		{
			{PassType.Free, 0},
			{PassType.Paid, 0}
		};
	}

	[Serializable]
	public class BattlePassData
	{
		public Dictionary<uint, BattlePassSeasonData> Seasons = new ();

		public BattlePassSeasonData GetSeason(uint season)
		{
			if (Seasons.TryGetValue(season, out var data))
			{
				return data;
			}

			throw new Exception("Season " + season + " not found in player data!");
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;

				foreach (var season in Seasons)
				{
					hash = hash * 23 + season.Key.GetHashCode();
					hash = hash * 23 + season.Value.Level.GetHashCode();
					hash = hash * 23 + season.Value.Level.GetHashCode();
					hash = hash * 23 + season.Value.Points.GetHashCode();
					hash = hash * 23 + season.Value.Claimed.GetHashCode();
					hash = hash * 23 + season.Value.Purchased.GetHashCode();
				}

				return hash;
			}
		}
	}
}