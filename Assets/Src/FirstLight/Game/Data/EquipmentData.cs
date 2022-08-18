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
	}
}