using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Holds NFT metada for the weapons a given player have. 
	/// </summary>
	[Serializable]
	public class NftEquipmentData
	{
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
	}
}