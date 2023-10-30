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
	public class BattlePassData
	{
		public uint BPLevel = 0;
		public uint BPPoints = 0;
		public readonly HashSet<uint> PurchasedBPSeasons = new();

		public Dictionary<PassType, uint> LastLevelsClaimed = new ()
		{
			{ PassType.Free, 0 },
			{ PassType.Paid, 0 }
		};
	
		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + BPLevel.GetHashCode();
			hash = hash * 23 + BPPoints.GetHashCode();
			foreach (var e in PurchasedBPSeasons)
				hash = hash * 23 + e.GetHashCode();
			return hash;
		}
	}
}