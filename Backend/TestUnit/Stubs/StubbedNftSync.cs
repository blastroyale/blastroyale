using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using JetBrains.Annotations;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using NSubstitute;
using NSubstitute.Core;
using Tests.Stubs;

/// <summary>
/// Stubbed NFT sync where instead of getting external indexed nfts and last updates
/// we can set them in-memory, for testing purposes.
/// </summary>
public class StubbedNftSync : BlockchainApi
{
	public List<RemoteCollectionItem> Owned => ((StubbedCollectionSync)CollectionsSync).Owned;

	public ulong LastUpdate => 1;

	public StubbedNftSync(PluginContext ctx) : base(null, null, ctx)
	{
		CollectionsSync = new StubbedCollectionSync(this);
	}
}