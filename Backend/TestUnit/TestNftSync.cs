using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using BlastRoyaleNFTPlugin.Parsers;
using BlastRoyaleNFTPlugin.Services;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Models.Collection;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Quantum;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using Assert = NUnit.Framework.Assert;

public class TestNftSync
{
	private BlastRoyalePlugin _plugin;
	private StubbedBlockchainApi _blockchainApi;
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
		_plugin = new BlastRoyalePlugin(_app.GetService<IStoreService>(), _app.GetService<IUserMutex>(),
			_app.GetService<IInventorySyncService<ItemData>>());
		_plugin.OnEnable(pluginCtx);
		_blockchainApi = new StubbedBlockchainApi(pluginCtx, _plugin);
		_analytics = _app.GetService<IServerAnalytics>() as InMemoryAnalytics;
		var state = _app.Services.GetService<IPlayerSetupService>()!.GetInitialState(_playerID);
		_app.ServerState.UpdatePlayerState(_playerID, state).GetAwaiter().GetResult();
		_state = state;
	}


	// [Test]
	// public async Task TestMasculineCorposCollectionSync_ETH_Success()
	// {
	// 	_blockchainApi.Owned.Add(CreateMasculineCorposNft());
	// 	
	// 	var state = await _app.GetService<IServerStateService>()!.GetPlayerState(_playerID);
	// 	await _blockchainApi.SyncData(state, _playerID);
	//
	// 	var ownedCorpos = GetOwnedCollection(state, CollectionsSyncService.COLLECTION_CORPOS_ETH);
	// 	
	// 	Assert.AreEqual(1, ownedCorpos.Count());
	// 	Assert.AreEqual(GameId.MaleCorpos, ownedCorpos.First().Id);
	// }
	//
	// [Test]
	// public async Task TestFeminineCorposCollectionSync_ETH_Success()
	// {
	// 	_blockchainApi.Owned.Add(CreateFeminineCorposNft());
	// 	
	// 	var state = await _app.GetService<IServerStateService>()!.GetPlayerState(_playerID);
	// 	await _blockchainApi.SyncData(state, _playerID);
	//
	// 	var ownedCorpos = GetOwnedCollection(state, CollectionsSyncService.COLLECTION_CORPOS_ETH);
	// 	
	// 	Assert.AreEqual(1, ownedCorpos.Count());
	// 	Assert.AreEqual(GameId.FemaleCorpos, ownedCorpos.First().Id);
	// }
	//
	// 	
	// [Test]
	// public async Task TestGamesggGamersCollectionSync_ETH_Success()
	// {
	// 	_blockchainApi.Owned.Add(CreateGamesggGamersNFt());
	// 	
	// 	var state = await _app.GetService<IServerStateService>()!.GetPlayerState(_playerID);
	// 	await _blockchainApi.SyncData(state, _playerID);
	//
	// 	var ownedGamesggGamers = GetOwnedCollection(state, CollectionsSyncService.COLLECTION_GAMESGG_GAMERS_ETH);
	// 	
	// 	Assert.AreEqual(1, ownedGamesggGamers.Count());
	// 	Assert.AreEqual(GameId.PlayerSkinGamer, ownedGamesggGamers.First().Id);
	// }
	//
	// 	
	// [Test]
	// public async Task TestPlagueDoctorCollectionSync_IMX_Success()
	// {
	// 	_blockchainApi.Owned.Add(CreatePlagueDoctorNFt());
	// 	
	// 	var state = await _app.GetService<IServerStateService>()!.GetPlayerState(_playerID);
	// 	await _blockchainApi.SyncData(state, _playerID);
	//
	// 	var ownedPlagueDoctors = GetOwnedCollection(state, CollectionsSyncService.COLLECTION_PLAGUE_DOCTOR_IMX);
	// 	
	// 	Assert.AreEqual(1, ownedPlagueDoctors.Count());
	// 	Assert.AreEqual(GameId.PlayerSkinPlagueDoctor, ownedPlagueDoctors.First().Id);
	// }
	//

	private IEnumerable<ItemData> GetOwnedCollection(ServerState state, string collectionName)
	{
		var collections = state.DeserializeModel<CollectionData>();
		var skins = collections.OwnedCollectibles[CollectionCategories.PLAYER_SKINS];

		var ownedItems = skins.Where(s => s.TryGetMetadata<CollectionMetadata>(out var meta)
			&& meta.TryGetTrait(CollectionTraits.NFT_COLLECTION, out var collectionFound)
			&& collectionFound == collectionName);

		return ownedItems;
	}


	private Corpos CreateMasculineCorposNft() =>
		CreateNft<Corpos>(new Dictionary<string, string>() { { "body", "Masculine" } });

	private Corpos CreateFeminineCorposNft() =>
		CreateNft<Corpos>(new Dictionary<string, string>() { { "body", "Feminine" } });

	private GamesGGGamers CreateGamesggGamersNFt() => CreateNft<GamesGGGamers>();
	private PlagueDoctor CreatePlagueDoctorNFt() => CreateNft<PlagueDoctor>();

	private T CreateNft<T>(Dictionary<string, string>? nftAttributes = null) where T : RemoteCollectionItem, new()
	{
		var parser = new FlgTraitTypeAttributeParser(new T
		{
			TokenId = "1"
		});

		if (nftAttributes?.Keys != null)
			foreach (var key in nftAttributes.Keys)
			{
				parser.AddAttribute(key, nftAttributes[key]);
			}

		return parser.Nft as T;
	}
}