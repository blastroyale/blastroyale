using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using BlastRoyaleNFTPlugin.Services;
using FirstLight.Server.SDK.Models;

namespace Tests.Stubs
{
	public class StubbedCollectionSyncService : CollectionsSyncService
	{
		public readonly BlastRoyalePlugin Plugin;
		public readonly List<RemoteCollectionItem> Owned = new List<RemoteCollectionItem>();
		
		
		public StubbedCollectionSyncService(BlastRoyalePlugin plugin) : base(plugin)
		{
			Plugin = plugin;
		}

		public async Task<IEnumerable<RemoteCollectionItem>> FetchAllCollections(string id)
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