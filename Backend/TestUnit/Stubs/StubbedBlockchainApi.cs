using System.Collections.Generic;
using BlastRoyaleNFTPlugin;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using Tests.Stubs;

/// <summary>
/// Stubbed NFT sync where instead of getting external indexed nfts and last updates
/// we can set them in-memory, for testing purposes.
/// </summary>
public class StubbedBlockchainApi : BlockchainApi
{
	public ulong LastUpdate => 1;

	public StubbedBlockchainApi() : base(null, null)
	{

	}
}