using FirstLight.Game.Configs;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Data.DataTypes.Helpers
{
	public static class AvatarHelpers
	{
		private static readonly string NFT_COLLECTION_FORMAT = "https://mainnetprodflghubstorage.blob.core.windows.net/collections/{0}/{1}.png";

		public static string GetAvatarUrl(ItemData item, AvatarCollectableConfig config)
		{
			string avatarUrl;
			if (item.Id == GameId.AvatarRemote)
			{
				if (item.TryGetMetadata<CollectionMetadata>(out var metadata)
					&& metadata.TryGetTrait(CollectionTraits.URL, out var url))
				{
					return url;
				}
			}

			if (item.Id == GameId.AvatarNFTCollection)
			{
				if (item.TryGetMetadata<CollectionMetadata>(out var metadata)
					&& metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection)
					&& metadata.TryGetTrait(CollectionTraits.TOKEN_ID, out var token))
				{
					return string.Format(NFT_COLLECTION_FORMAT, collection, token);
				}
			}

			return config.GameIdUrlDictionary[item.Id];
		}
	}
}