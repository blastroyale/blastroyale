using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	[Flags]
	public enum PlayerFlags : byte
	{
		None = 0,
		QA = 1 << 1,
		Admin = 1 << 2,
		Deleted = 1 << 3,
		FLGOfficial = 1 << 4
	}
	
	/// <summary>
	/// Contains all the data in the scope of the Player 
	/// </summary>
	[Serializable]
	public class PlayerData
	{
		public PlayerFlags Flags;
		public uint Level = 1;
		public uint Xp = 0;
		public uint Trophies = 1000;
		public uint BPLevel = 0;
		public uint BPPoints = 0;
		public GameId DeathMarker = GameId.Tombstone;
		public readonly List<RewardData> UncollectedRewards = new();
		public readonly Dictionary<GameIdGroup, UniqueId> Equipped = new(new GameIdGroupComparer());
		public readonly Dictionary<GameId, ResourcePoolData> ResourcePools = new(new GameIdComparer())
		{
			{ GameId.CS, new ResourcePoolData(GameId.CS, 0, DateTime.MinValue) },
			{ GameId.BPP, new ResourcePoolData(GameId.BPP, 0, DateTime.MinValue) },
		};
		public readonly Dictionary<GameId, ulong> Currencies = new (new GameIdComparer())
		{
			{ GameId.CS, 0 },
			{ GameId.BLST, 0 },
			{ GameId.COIN, 0 }
		};

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + Flags.GetHashCode();
			hash = hash * 23 + Level.GetHashCode();
			hash = hash * 23 + Xp.GetHashCode();
			hash = hash * 23 + Trophies.GetHashCode();
			hash = hash * 23 + BPLevel.GetHashCode();
			hash = hash * 23 + BPPoints.GetHashCode();
			hash = hash * 23 + DeathMarker.GetHashCode();

			foreach (var e in UncollectedRewards)
				hash = hash * 23 + (int)e.RewardId + e.Value.GetHashCode();
			
			foreach (var e in Equipped.OrderBy(entry => (int)entry.Key))
				hash = hash * 23 + (int)e.Key + e.Value.GetHashCode();

			foreach (var e in ResourcePools)
				hash = hash * 23 + (int)e.Key + e.Value.GetHashCode();
			
			foreach (var e in Currencies)
				hash = hash * 23 + (int)e.Key + e.Value.GetHashCode();
			
			return hash;
		}
	}
	
}