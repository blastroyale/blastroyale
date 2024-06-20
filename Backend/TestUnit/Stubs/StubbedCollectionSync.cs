using System.Collections.Generic;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Server.SDK.Models;

namespace Tests.Stubs
{
	public class StubbedCollectionSync : CollectionsSync
	{
		public List<RemoteCollectionItem> Owned = new List<RemoteCollectionItem>();
		
		public StubbedCollectionSync(BlockchainApi blockchainApi) : base(blockchainApi)
		{
		}

		public override async Task<IEnumerable<RemoteCollectionItem>> FetchAllCollections(string id)
		{
			return Owned;
		}
	}
}