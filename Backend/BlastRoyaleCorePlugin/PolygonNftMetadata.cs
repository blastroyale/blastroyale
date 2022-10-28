using System;

namespace BlastRoyaleNFTPlugin
{
	/// <summary>
	/// Represents a Blast Royale NFT Weapon with metadata embedded in.
	/// Property names do not follow .net standards because it was not created in .NET
	/// But we require to keep the names matching the column names in Moralis.
	/// This object name has to reflect moralis table name.
	/// </summary>
	[Serializable]
	public class PolygonNFTMetadata
	{
		public string objectId { get; set; }
		public string token_id { get; set; }
		public string token_address { get; set; }
		public string ACL { get; set; }
		public string name { get; set; }
		public DateTime updatedAt { get; set; }
		public DateTime createdAt { get; set; }
		public string image { get; set; }
		public string description { get; set; }
		public long level { get; set; }
		public long replicationCount { get; set; }
		public long maxLevel { get; set; }
		public long material { get; set; }
		public long adjective { get; set; }
		public long rarity { get; set; }
		public long durabilityRemaining { get; set; }
		public long repairCount { get; set; }

		public long maxDurability { get; set; }
		public long manufacturer { get; set; }
		public long initialReplicationCounter { get; set; }
		public long lastRepairTime { get; set; }
		public long tuning { get; set; }
		public long grade { get; set; }
		public long generation { get; set; }
		public long faction { get; set; }
		public long category { get; set; }
		public long edition { get; set; }
		public long subCategory { get; set; }
	}
}