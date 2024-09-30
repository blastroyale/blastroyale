namespace FirstLight.Game.Data.DataTypes.Helpers
{
	public static class ItemDataExtensions
	{
		public static bool IsNft(this ItemData itemData)
		{
			return itemData.TryGetMetadata<CollectionMetadata>(out var metadata) 
				&& metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out _);
		}
	}
}