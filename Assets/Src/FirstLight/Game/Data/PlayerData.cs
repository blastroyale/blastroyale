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
		public uint Level;
		public uint Xp;
		public uint Trophies;
		public GameId PlayerSkinId;
		public GameId DeathMarker;
		public readonly Dictionary<GameId, ulong> Currencies = new(new GameIdComparer());
		public readonly Dictionary<GameIdGroup, UniqueId> Equipped = new(new GameIdGroupComparer());
		public readonly Dictionary<GameId, ResourcePoolData> ResourcePools = new(new GameIdComparer());
		public readonly List<RewardData> UncollectedRewards = new();
	}
}