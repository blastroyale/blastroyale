using System.Collections.Generic;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using JetBrains.Annotations;
using FirstLight.Server.SDK;

/// <summary>
/// Stubbed NFT sync where instead of getting external indexed nfts and last updates
/// we can set them in-memory, for testing purposes.
/// </summary>
public class StubbedNftSync : NftSynchronizer
{
	public List<PolygonNFTMetadata> Indexed = new ();

	public ulong LastUpdate { get; set; } = 1;

	protected override async Task<IEnumerable<PolygonNFTMetadata>?> RequestBlockchainIndexedNfts(string playerId)
	{
		return Indexed;
	}

	protected override async Task<ulong> RequestBlockchainLastUpdate(string playerId)
	{
		return LastUpdate;
	}
	
	public StubbedNftSync([NotNull] PluginContext ctx) : base(null, null, ctx)
	{
		
	}
}