using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Server.SDK.Models;

namespace Tests.Stubs
{
	public class StubbedCollectionSync : CollectionsSync
	{
		private readonly BlockchainApi _blockchainApi;
		public readonly List<RemoteCollectionItem> Owned = new List<RemoteCollectionItem>();
		
		
		public StubbedCollectionSync(BlockchainApi blockchainApi) : base(blockchainApi)
		{
			_blockchainApi = blockchainApi;
		}

		public override async Task<IEnumerable<RemoteCollectionItem>> FetchAllCollections(string id)
		{
			return Owned;
		}

		public bool CanSyncCollection(string collectionName)
		{
			var syncEnabledConfig = Environment.GetEnvironmentVariable(string.Concat(collectionName.ToUpperInvariant(), "_SYNC_ENABLED"));

			bool.TryParse(syncEnabledConfig, out var canSync);
			return canSync;
		}
	}
}