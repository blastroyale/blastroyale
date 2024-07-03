using System;
using FirstLight.Game.Configs;
using FirstLight.Server.SDK.Modules;
using Quantum;

namespace FirstLight.Game.Data.DataTypes.Helpers
{
	public static class AvatarHelpers
	{
		private static readonly string NFT_COLLECTION_FORMAT = "https://mainnetprodflghubstorage.blob.core.windows.net/collections/{0}/{1}.png";

		public static string GetAvatarUrl(ItemData item, AvatarCollectableConfig config)
		{
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
					return string.Format(NFT_COLLECTION_FORMAT, collection.ToLowerInvariant(), token);
				}
			}

			if(config.GameIdUrlDictionary.TryGetValue(item.Id, out var configUrl)) return configUrl;
			throw new Exception($"Could not find source of URL in neither configs metadata for avatar {ModelSerializer.Serialize(item).Value}");
		}
	}
}