namespace FirstLight.Game.Data.DataTypes
{
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
	}
}