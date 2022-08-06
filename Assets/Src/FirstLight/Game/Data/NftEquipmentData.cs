using System;
using System.Collections.Generic;
using System.Numerics;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Holds NFT metadata for the weapons a given player have. 
	/// </summary>
	[Serializable]
	public class NftEquipmentData
	{
		/// <summary>
		/// Maps each equipment <see cref="UniqueId"/> to it's <see cref="Equipment"/> data
		/// </summary>
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
		
		//TODO: Do we need all the data separate below? Can we just create some sort of NftEquipment and aggregate all data?
		/// <summary>
		/// Maps player specific ids to blockchain token ids
		/// </summary>
		public readonly Dictionary<UniqueId, string> TokenIds = new();
		
		/// <summary>
		/// Some items might expire, due to being rented for instance.
		/// This maps the unique id and the timestamp 
		/// </summary>
		public readonly Dictionary<UniqueId, ulong> ExpireTimestamps = new();
		
		/// <summary>
		/// Dictionary of timestamps of when items were synced (added) into the player's inventory
		/// </summary>
		public readonly Dictionary<UniqueId, long> InsertionTimestamps = new();

		/// <summary>
		/// Refer to the image urls for the inventory items
		/// </summary>
		public readonly Dictionary<UniqueId, string> ImageUrls = new();

		/// <summary>
		/// Field that holds the timestamp when nfts were last updated
		/// </summary>
		public ulong LastUpdateTimestamp;
	}
}