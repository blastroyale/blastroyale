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
		public readonly Dictionary<UniqueId, Equipment> Inventory = new();
		
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
		/// Items that have been transferred recently do not contribute to player's earning.
		/// This maps the unique id and the timestamp 
		/// </summary>
		public readonly Dictionary<UniqueId, ulong> CooldownTimestamps = new();

		/// <summary>
		/// Field that holds the timestamp when nfts were last updated
		/// </summary>
		public ulong LastUpdateTimestamp;
	}
}