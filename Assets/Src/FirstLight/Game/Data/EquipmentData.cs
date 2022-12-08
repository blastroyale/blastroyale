using System;
using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Holds NFT metadata for the weapons a given player have. 
	/// </summary>
	[Serializable]
	public class EquipmentData
	{
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
		public readonly Dictionary<UniqueId, NftEquipmentData> NftInventory = new();
		public ulong LastUpdateTimestamp;

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + LastUpdateTimestamp.GetHashCode();
			foreach (var item in Inventory.Values)
			{
				hash = hash * 23 + GetItemHash(item);
			}

			foreach (var item in NftInventory.Values)
			{
				hash = hash * 23 + item.GetHashCode();
			}

			return hash;
		}

		public int GetItemHash(Equipment e)
		{
			return (((int)((((int)((int)((((int)(((int)((int)((int)((int)(281 * 31 + e.Adjective) * 31 + e.Edition) * 31 + e.Faction) * 31 + e.GameId) * 31 + e.Generation.GetHashCode()) * 31 + e.Grade) * 31 + e.InitialReplicationCounter.GetHashCode()) * 31 + e.Level.GetHashCode()) * 31 + e.Manufacturer) * 31 + e.Material) * 31 + e.MaxDurability.GetHashCode()) * 31 + e.MaxLevel.GetHashCode()) * 31 + e.Rarity) * 31 + e.ReplicationCounter.GetHashCode()) * 31 + e.TotalRestoredDurability.GetHashCode()) * 31 + e.Tuning.GetHashCode();
		}
	}
}