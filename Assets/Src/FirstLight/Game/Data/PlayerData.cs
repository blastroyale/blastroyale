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
		public readonly Dictionary<GameId, ulong> Currencies = new(new GameIdComparer());
		public readonly Dictionary<GameIdGroup, UniqueId> EquippedItems = new(new GameIdGroupComparer());
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
		public readonly List<RewardData> UncollectedRewards = new();
		public readonly List<uint> LevelRewardsCollected = new();
		public readonly List<TimedBoxData> TimedBoxes = new();
		public readonly List<LootBoxData> CoreBoxes = new();
		public readonly List<GameId> Emoji = new();
	}
}