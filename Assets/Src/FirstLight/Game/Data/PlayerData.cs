using System;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Contains all the data in the scope of the Player 
	/// </summary>
	[Serializable]
	public class PlayerData
	{
		public uint Level = 1;
		public uint Xp = 0;
		public uint Trophies = 1000;
		public GameId PlayerSkinId = GameId.Female01Avatar;
		public GameId DeathMarker = GameId.Tombstone;
		public readonly Dictionary<GameIdGroup, UniqueId> Equipped = new(new GameIdGroupComparer());
		public readonly Dictionary<GameId, ResourcePoolData> ResourcePools = new(new GameIdComparer());
		public readonly List<RewardData> UncollectedRewards = new();
		public readonly Dictionary<GameId, ulong> Currencies = new (new GameIdComparer())
		{
			{ GameId.CS, 0 },
			{ GameId.BLST, 0 }
		};
	}
}