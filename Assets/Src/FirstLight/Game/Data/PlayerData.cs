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
		public GameId PlayerSkinId;
		public int CurrentCrateCycleIndex;
		public readonly Dictionary<GameId, ulong> Currencies = new Dictionary<GameId, ulong>(new GameIdComparer());
		public readonly Dictionary<GameIdGroup, UniqueId> EquippedItems = new Dictionary<GameIdGroup, UniqueId>(new GameIdGroupComparer());
		public readonly List<EquipmentData> Inventory = new List<EquipmentData>();
		public readonly List<RewardData> UncollectedRewards = new List<RewardData>();
		public readonly List<uint> LevelRewardsCollected = new List<uint>();
		public readonly List<TimedBoxData> TimedBoxes = new List<TimedBoxData>();
		public readonly List<LootBoxData> CoreBoxes = new List<LootBoxData>();
		public readonly List<GameId> Emoji = new List<GameId>();
	}
}