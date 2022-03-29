using System;
using System.Collections.Generic;
using Backend.Data.DataTypes;

namespace Backend.Data
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
		public string PlayerSkinId;
		public readonly Dictionary<string, uint> Currencies = new Dictionary<string, uint>();
		public readonly Dictionary<string, UniqueId> EquippedItems = new Dictionary<string, UniqueId>();
		public readonly List<EquipmentData> Inventory = new List<EquipmentData>();
		public readonly List<RewardData> UncollectedRewards = new List<RewardData>();
		public readonly List<uint> LevelRewardsCollected = new List<uint>();
		public readonly List<TimedBoxData> TimedBoxes = new List<TimedBoxData>();
		public readonly List<LootBoxData> CoreBoxes = new List<LootBoxData>();
		public readonly List<string> Emoji = new List<string>();
		public readonly List<AdventureData> AdventureProgress = new List<AdventureData>();
	}
}