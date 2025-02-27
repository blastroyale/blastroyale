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
	public void Sync(CollectionData playersCollectionData, NFTCollectionSyncConfiguration collectionSyncConfiguration, List<RemoteCollectionItem> OwnedNFTsFetchedRemotely)
	{
		foreach (var itemSyncConfig in collectionSyncConfiguration.ItemSyncConfiguration)
		{
			
			var hasInGameItemSynced = false;
			var hasNFTToSync = HasNFTToSyncForItemSyncConfig(itemSyncConfig, OwnedNFTsFetchedRemotely);
		
			var inGameCollectionItem = playersCollectionData.OwnedCollectibles.TryGetValue(itemSyncConfig.ItemCollectionCategory, out var inGameItems)
		 		? inGameItems : new List<ItemData>();
		
			//Loop through current skins to remove NFT skin if player doesn't owns the NFT anymore.
			for (var itemIndex = 0; itemIndex < inGameCollectionItem.Count; itemIndex++)
			{
		 		var item = inGameCollectionItem[itemIndex];
			
		 		var isNFTCollectionMetadata = item.TryGetMetadata<CollectionMetadata>(out var meta)
		 			&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection)
		 			&& collection == collectionSyncConfiguration.CollectionName;
		
				if (itemSyncConfig.InGameRewards.Contains(item.Id))
				{
		
					hasInGameItemSynced = true;
		
					if (!hasNFTToSync && isNFTCollectionMetadata)
					{
						RemoveEquippedSkin(item, playersCollectionData);
						inGameCollectionItem.RemoveAt(itemIndex);
					}
		
				}
			}

			// Add the skin to players inventory
			if (!hasInGameItemSynced && hasNFTToSync)
			{
			 inGameCollectionItem.AddRange(itemSyncConfig.InGameRewards.Select(rewardId => ItemFactory.Collection(rewardId, new CollectionTrait(CollectionTraits.NFT_COLLECTION, collectionSyncConfiguration.CollectionName))));
			}

			playersCollectionData.OwnedCollectibles[itemSyncConfig.ItemCollectionCategory] = inGameCollectionItem; 
		}
	}

	
	private bool HasNFTToSyncForItemSyncConfig(InItemSyncConfiguration syncConfiguration, List<RemoteCollectionItem> ownedNFTsFetchedRemotely)
	{
		if (!syncConfiguration.TraitRequired)
		{
			return ownedNFTsFetchedRemotely.Count > 0;
		}

		return ownedNFTsFetchedRemotely.Any(ownedNFT =>
		{
			var parsedTraits = new FlgTraitTypeAttributeParser(ownedNFT);

			return parsedTraits.Traits.TryGetValue(syncConfiguration.TraitName, out var traitValue) && 
				   string.Equals(traitValue, syncConfiguration.TraitValue, StringComparison.OrdinalIgnoreCase);
		});
	}
	
	
	
	private void RemoveEquippedSkin(ItemData item, CollectionData data)
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