using System;
using System.Collections.Generic;
using System.Linq;
using BlastRoyaleNFTPlugin.Data;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace BlastRoyaleNFTPlugin.Collections;

public class NFTProfilePictureCollectionSync : ICollectionSync
{
	public void Sync(CollectionData playersCollectionData, NFTCollectionSyncConfiguration collectionSyncConfiguration, List<RemoteCollectionItem> ownedNFTs)
	{
		var currentOwnedTokensForCollection = ownedNFTs.Where(n => n.Name != null && n.Name.StartsWith(collectionSyncConfiguration.NFTImagePrefix, StringComparison.InvariantCulture)).Select(nft => nft.TokenId).ToList();
		var pfpListToAdd = new List<string>(currentOwnedTokensForCollection);
		
		var profilePictureList = playersCollectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PROFILE_PICTURE, out var profilePictureLoaded)
			? profilePictureLoaded : new List<ItemData>();
	
		//Loop through current profilePicture List to remove NFT profilePicture if player doesn't owns the NFT anymore.
		for (var profilePictureIndex = profilePictureList.Count - 1; profilePictureIndex >= 0; profilePictureIndex--)
		{
			var profilePicture = profilePictureList[profilePictureIndex];

			if (profilePicture.Id != GameId.AvatarNFTCollection || !profilePicture.HasMetadata<CollectionMetadata>())
			{
				continue;
			}
			
			var metadata = profilePicture.GetMetadata<CollectionMetadata>();

			if (!metadata.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection) ||
				!metadata.TryGetTrait(CollectionTraits.TOKEN_ID, out var lastSyncToken))
			{
				continue;
			}

			if (collection != collectionSyncConfiguration.CollectionName)
			{
				continue;
			}

			if (!currentOwnedTokensForCollection.Contains(lastSyncToken))
			{
				RemoveEquippedProfilePicture(profilePicture, playersCollectionData);
				profilePictureList.RemoveAt(profilePictureIndex);
				continue;
			}
			
			pfpListToAdd.Remove(lastSyncToken);
		}
	
		foreach (var tokenId in pfpListToAdd)
		{
			var item = ItemFactory.Collection(GameId.AvatarNFTCollection,
				new CollectionTrait(CollectionTraits.NFT_COLLECTION, collectionSyncConfiguration.CollectionName),
						   new CollectionTrait(CollectionTraits.TOKEN_ID, tokenId));
			
			profilePictureList.Add(item);

		}
		
		playersCollectionData.OwnedCollectibles[CollectionCategories.PROFILE_PICTURE] = profilePictureList;
	}
	
	private void RemoveEquippedProfilePicture(ItemData item, CollectionData data)
	{
		foreach (var collectionType in data.Equipped.Keys)
		{
			var equipped = data.Equipped[collectionType];
			if (equipped.Equals(item))
			{
				data.Equipped[collectionType] = data.OwnedCollectibles[collectionType].FirstOrDefault();
			}
		}
	}
}