using System;
using System.Collections.Generic;
using System.Linq;
using BlastRoyaleNFTPlugin.Data;
using BlastRoyaleNFTPlugin.Parsers;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Server.SDK.Models;
using Quantum;

namespace BlastRoyaleNFTPlugin.Collections;

public class SkinCollectionSync :  ICollectionSync
{
	public void Sync(CollectionData playersCollectionData, NFTCollectionSyncConfigModel collectionSyncConfig, List<RemoteCollectionItem> collectionOwnedNFTs)
	{
		//There are cases where a collection has more than one skin/item configured (i.e. Corpos) so we must loop at each item to sync/validate each of them individually
		foreach (var collectionItem in collectionSyncConfig.CollectionItems)
		{
			var hasCollectionItemSkin = false;
			var hasCollectionItemNFT = HasCollectionNFT(collectionItem, collectionOwnedNFTs);

			var playerSkins = playersCollectionData.OwnedCollectibles.TryGetValue(CollectionCategories.PLAYER_SKINS, out var skinsLoaded)
				? skinsLoaded : new List<ItemData>();
		
			//Loop through current skins to remove NFT skin if player doesn't owns the NFT anymore.
			for (var skinIndex = 0; skinIndex < playerSkins.Count; skinIndex++)
			{
				var skin = playerSkins[skinIndex];
			
				var isNFTCollectionMetadata = skin.TryGetMetadata<CollectionMetadata>(out var meta)
					&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection)
					&& collection == collectionSyncConfig.CollectionName;

				if (skin.Id == collectionItem.Key)
				{

					hasCollectionItemSkin = true;

					if (!hasCollectionItemNFT && isNFTCollectionMetadata)
					{
						RemoveEquippedSkin(skin, playersCollectionData);
						playerSkins.RemoveAt(skinIndex);
					}

				}
			}
			
			//Add the skin to players inventory
			if (!hasCollectionItemSkin && hasCollectionItemNFT)
			{
				playerSkins.Add(ItemFactory.Collection(collectionItem.Key,
					new CollectionTrait(CollectionTraits.NFT_COLLECTION, collectionSyncConfig.CollectionName)
				));
			}

			playersCollectionData.OwnedCollectibles[CollectionCategories.PLAYER_SKINS] = playerSkins;
		}
	}

	private bool HasCollectionNFT(KeyValuePair<GameId, Dictionary<string, string>> collectionItemTraitRules, List<RemoteCollectionItem> collectionOwnedNFTs)
	{
		//This NFT doesn't contain any specific rule to check if the Player has or has not the NFT
		if (collectionItemTraitRules.Value == null || collectionItemTraitRules.Value.Count == 0)
		{
			return collectionOwnedNFTs.Count > 0;
		}
		
		//When collectionItemTraitRules Dictionary is not null and the size is not 0 we must check the NFT metadata to validate if player has or has not the NFT
		//i.e. In our Corpos collection, we must check the "body" attribute to check which skin are we going to add/remove to players collection data.
		// Check each owned NFT to see if any trait matches the collection rules.
		return collectionOwnedNFTs.Any(ownedNFT =>
		{
			var parsedTrait = new FlgTraitTypeAttributeParser(ownedNFT);

			// Check if any trait rule matches the parsed trait values.
			return collectionItemTraitRules.Value
				.Any(traitValueRule =>
					parsedTrait.Traits.TryGetValue(traitValueRule.Key, out var parsedTraitValue) &&
					parsedTraitValue.Equals(traitValueRule.Value, StringComparison.OrdinalIgnoreCase));
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