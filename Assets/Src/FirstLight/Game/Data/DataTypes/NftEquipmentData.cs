using System;
using FirstLightServerSDK.Modules;

namespace FirstLight.Game.Data.DataTypes
{
	[Serializable]
	public struct NftEquipmentData
	{
		public string TokenId;
		public long InsertionTimestamp;
		public string ImageUrl;

		/// <summary>
		/// Check if this NFT Data is valid or not by it's TokenId signature.
		/// Empty TokenId signature means an invalid NFT Data
		/// </summary>
		public bool IsValid => !string.IsNullOrWhiteSpace(TokenId);

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + TokenId.GetDeterministicHashCode();
			hash = hash * 23 + InsertionTimestamp.GetHashCode();
			hash = hash * 23 + ImageUrl.GetDeterministicHashCode();
			return hash;
		}
	}
}