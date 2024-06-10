using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using BlastRoyaleNFTPlugin.NftSyncs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using Assert = NUnit.Framework.Assert;

public class TestNftSyncPlugin
{
	private BlastRoyalePlugin _plugin;
	private StubbedNftSync _nftSync;
	private PluginEventManager _events;
	private TestServer _app;
	private InMemoryAnalytics _analytics;
	private ServerState _state;
	private string _playerID;

	[SetUp]
	public void Setup()
	{
		_app = new TestServer();
		_app.SetupInMemoryServer();
		_playerID = _app.GetTestPlayerID();
		var log = _app.GetService<IPluginLogger>();
		_events = new PluginEventManager(log);
		var pluginCtx = new PluginContext(_events, _app.Services);
		_nftSync = new StubbedNftSync(pluginCtx);
		_plugin = new BlastRoyalePlugin();
		_plugin.OnEnable(pluginCtx);
		_analytics = _app.GetService<IServerAnalytics>() as InMemoryAnalytics;
		var state = _app.Services.GetService<IPlayerSetupService>()!.GetInitialState(_playerID);
		_app.ServerState.UpdatePlayerState(_playerID, state).GetAwaiter().GetResult();
		_state = state;
	}

	[Test]
	public void TestQuantumVector3Serialialization()
	{
		var v = new FPVector3(FP._0_01, FP._0_02, FP._0_03);

		var serialized = ModelSerializer.Serialize(v).Value;
		var deserialized = ModelSerializer.Deserialize<FPVector3>(serialized);

		Assert.AreEqual(v.X.RawValue, deserialized.X.RawValue);
		Assert.AreEqual(v.Y.RawValue, deserialized.Y.RawValue);
		Assert.AreEqual(v.Z.RawValue, deserialized.Z.RawValue);
	}

	[Test]
	public async Task TestCollectionSync()
	{
		var parser = new FlgTraitParser(new RemoteCollectionItem()
		{
			TokenId = "1",

		});
		parser.AddAttribute("body", "Masculine");
		_nftSync.Owned.Add(parser.Nft);

		var state = await _app.GetService<IServerStateService>()!.GetPlayerState(_playerID);
		await _nftSync.SyncData(state, _playerID);
		
		var collections = state.DeserializeModel<CollectionData>();
		var skins = collections.OwnedCollectibles[CollectionCategories.PLAYER_SKINS];
		var corpos = skins.Select(s => s.TryGetMetadata<CollectionMetadata>(out var meta)
			&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection)
			&& collection == CollectionsSync.COLLECTION_CORPOS_ETH);
		
		Assert.AreEqual(1, corpos.Count());
	}
	
	[Test]
	public async Task TestMasculine()
	{
		var parser = new FlgTraitParser(new RemoteCollectionItem()
		{
			TokenId = "1",

		});
		parser.AddAttribute("body", "Masculine");
		_nftSync.Owned.Add(parser.Nft);

	
		await _nftSync.SyncData(_state, _playerID);

		var corpos = GetOwnedCorpos();
		
		Assert.AreEqual(GameId.MaleCorpos, corpos.First().Id);
	}
	
	[Test]
	public async Task TestFeminine()
	{
		var parser = new FlgTraitParser(new RemoteCollectionItem()
		{
			TokenId = "1",
		});
		parser.AddAttribute("body", "Feminine");
		_nftSync.Owned.Add(parser.Nft);
	
		await _nftSync.SyncData(_state, _playerID);

		var corpos = GetOwnedCorpos();
		
		Assert.AreEqual(GameId.FemaleCorpos, corpos.First().Id);
	}
	
	[Test]
	public async Task TestRemovingOwnedCorpo()
	{
		var parser = new FlgTraitParser(new RemoteCollectionItem()
		{
			TokenId = "1",
		});
		parser.AddAttribute("body", "Feminine");
		_nftSync.Owned.Add(parser.Nft);
	
		// Add to player
		await _nftSync.SyncData(_state, _playerID);

		// Remove from player
		_nftSync.Owned.Remove(parser.Nft);
		await _nftSync.SyncData(_state, _playerID);
		
		var corpos = GetOwnedCorpos();
		
		Assert.AreEqual(0, corpos.Count());
	}
	
	private IEnumerable<ItemData> GetOwnedCorpos()
	{
		var collections = _state.DeserializeModel<CollectionData>();
		var skins = collections.OwnedCollectibles[CollectionCategories.PLAYER_SKINS];
		var corpos = skins.Where(s => s.TryGetMetadata<CollectionMetadata>(out var meta)
			&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collection)
			&& collection == CollectionsSync.COLLECTION_CORPOS_ETH);
		return corpos;
	}
}