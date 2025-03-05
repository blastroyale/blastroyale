using System;
using System.Collections.Generic;
using System.Linq;
using BlastRoyaleNFTPlugin.Data;
using BlastRoyaleNFTPlugin.Parsers;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK.Models;

namespace BlastRoyaleNFTPlugin.Collections;

//
// InGame Items are Items that contains a GameID inside BlastRoyale
//
public class InGameItemsCollectionSync :  ICollectionSync
{
	public void Sync(CollectionData playersCollectionData, NFTCollectionSyncConfiguration collectionSyncConfiguration,
					 CollectionFetchResponse remoteOwnedCollectionsNFTsResult)
	{
		foreach (var categorySyncConfig in collectionSyncConfiguration.CategorySyncConfiguration)
		{
		
			var inGameCollectionItem = playersCollectionData.OwnedCollectibles.TryGetValue(categorySyncConfig.ItemCollectionCategory, out var inGameItems) ? inGameItems : new List<ItemData>();
			
			CleanUpCollectionInGameItemsForCollectionCategory(collectionSyncConfiguration.CollectionName, inGameCollectionItem);

			if (!remoteOwnedCollectionsNFTsResult.CollectionNFTsOwnedDict.TryGetValue(collectionSyncConfiguration.CollectionName, out var collectionOwnedNFTs))
			{
				TryUnequipRemovedItemForCategory(categorySyncConfig.ItemCollectionCategory, playersCollectionData, inGameCollectionItem);
				continue;
			}

			foreach (var itemTraitRewardsConfig in categorySyncConfig.ItemTraitRewardsConfigurations)
			{
				if (HasRemoteNFTForItemSyncConfig(itemTraitRewardsConfig, collectionOwnedNFTs))
				{
					var rewardsToSync = itemTraitRewardsConfig.InGameRewards.Except(inGameCollectionItem.Select(i => i.Id)).ToList();
				
					inGameCollectionItem.AddRange(rewardsToSync.Select(rewardId => ItemFactory.Collection(rewardId, new CollectionTrait(CollectionTraits.NFT_COLLECTION, collectionSyncConfiguration.CollectionName))));
				}	
			}
			
			TryUnequipRemovedItemForCategory(categorySyncConfig.ItemCollectionCategory, playersCollectionData, inGameCollectionItem);
			
			playersCollectionData.OwnedCollectibles[categorySyncConfig.ItemCollectionCategory] = inGameCollectionItem;
		}
	}

	private void CleanUpCollectionInGameItemsForCollectionCategory(string collectionName, List<ItemData> currentPlayersItem)
	{
		currentPlayersItem.RemoveAll(i => i.TryGetMetadata<CollectionMetadata>(out var meta)
			&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection) && collection == collectionName);
	}


	private bool HasRemoteNFTForItemSyncConfig(ItemTraitRewardSyncConfiguration itemTraitRewardConfig, List<RemoteCollectionItem> ownedNFTsFetchedRemotely)
	{
		if (!itemTraitRewardConfig.TraitRequired)
		{
			return ownedNFTsFetchedRemotely.Count > 0;
		}

		return ownedNFTsFetchedRemotely.Any(ownedNFT =>
		{
			var parsedTraits = new FlgTraitTypeAttributeParser(ownedNFT);

			return parsedTraits.Traits.TryGetValue(itemTraitRewardConfig.TraitName, out var traitValue) && 
				   string.Equals(traitValue, itemTraitRewardConfig.TraitValue, StringComparison.OrdinalIgnoreCase);
		});
	}
	
	
	
	private void TryUnequipRemovedItemForCategory(CollectionCategory collectionCategory, CollectionData collectionData,
												  List<ItemData> inGameCollectionItem)
	{
		if(collectionData.Equipped.TryGetValue(collectionCategory, out var categoryEquippedItem)) {
			
			if (!inGameCollectionItem.Contains(categoryEquippedItem))
			{
				collectionData.Equipped[collectionCategory] = collectionData.OwnedCollectibles[collectionCategory].FirstOrDefault();
			}
		}
		
	}
}