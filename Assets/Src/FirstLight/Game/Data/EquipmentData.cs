using System;
using System.Collections.Generic;
using System.Numerics;
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
		/// <summary>
		/// Maps each equipment <see cref="UniqueId"/> to it's <see cref="Equipment"/> data
		/// </summary>
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
		
		/// <summary>
		/// Maps each NFT equipment <see cref="UniqueId"/> to it's <see cref="NftEquipmentData"/>
		/// </summary>
		public readonly Dictionary<UniqueId, NftEquipmentData> NftInventory = new();

		/// <summary>
		/// Field that holds the timestamp when nfts were last updated
		/// </summary>
		public ulong LastUpdateTimestamp;
	}
}