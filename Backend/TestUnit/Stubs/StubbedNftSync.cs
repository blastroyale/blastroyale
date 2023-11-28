using System.Collections.Generic;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using JetBrains.Annotations;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using NSubstitute;
using NSubstitute.Core;

/// <summary>
/// Stubbed NFT sync where instead of getting external indexed nfts and last updates
/// we can set them in-memory, for testing purposes.
/// </summary>
public class StubbedNftSync : NftSynchronizer
{
	public List<PolygonNFTMetadata> Indexed = new();

	public ulong LastUpdate { get; set; } = 1;


	protected override async Task<ulong> RequestBlockchainLastUpdate(string playerId)
	{
		return LastUpdate;
	}

	public StubbedNftSync([NotNull] PluginContext ctx) : base(null, null, ctx)
	{
		_corpoSync = Substitute.ForPartsOf<CorpoSync>(this);
		_equipmentSync = Substitute.ForPartsOf<EquipmentSync>(this);
		_equipmentSync.WhenForAnyArgs(x => x.RequestBlockchainIndexedNfts(default)).DoNotCallBase();
		_corpoSync.WhenForAnyArgs(x => x.RequestCollection(default, default)).DoNotCallBase();
		_equipmentSync.RequestBlockchainIndexedNfts(default).ReturnsForAnyArgs(Indexed);
		_corpoSync.RequestCollection(default, default).ReturnsForAnyArgs(new CollectionFetchResponse()
		{
			Owned = new List<RemoteCollectionItem>(),
		});
	}
}