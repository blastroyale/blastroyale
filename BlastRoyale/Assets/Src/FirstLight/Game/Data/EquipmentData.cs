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
			return e.GetServerHashCode();
		}
	}
}