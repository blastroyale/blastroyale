using System.Collections.Generic;
using BlastRoyaleNFTPlugin.Data;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Models;

namespace BlastRoyaleNFTPlugin.Collections;

public interface ICollectionSync
{
	void Sync(CollectionData playersCollectionData, NFTCollectionSyncConfiguration collectionSyncConfiguration,
			  CollectionFetchResponse remoteOwnedCollectionsNFTsResult);

}