/*
using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data;
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

// TESTS DISABLED UNTIL WE RE-ENABLE NFT SYNC FOR NOW

public class TestNftEquipmentSync
{
	private BlastRoyalePlugin _plugin;
	private StubbedNftSync _nftSync;
	private PluginEventManager _events;
	private TestServer _app;
	private InMemoryAnalytics _analytics;
	private ServerState _state;

	[SetUp]
	public void Setup()
	{
		_app = new TestServer();
		_app.SetupInMemoryServer();
		var log = _app.GetService<IPluginLogger>();
		_events = new PluginEventManager(log);
		var pluginCtx = new PluginContext(_events, _app.Services);
		_nftSync = new StubbedNftSync(pluginCtx);
		_plugin = new BlastRoyalePlugin();
		_plugin.OnEnable(pluginCtx);
		_analytics = _app.GetService<IServerAnalytics>() as InMemoryAnalytics;
		_nftSync.Indexed.Add(new PolygonNFTMetadata()
		{
			token_id = "tokenid1",
			subCategory = (int) GameId.ModPistol,
			faction = (long) EquipmentFaction.Chaos
		});
		var state = _app.Services.GetService<IPlayerSetupService>().GetInitialState("yolo");
		_app.ServerState.UpdatePlayerState("yolo", state).GetAwaiter().GetResult();
		_state = state;
	}
	
	[Test]
	public async Task TestInventorySync()
	{
		await SyncData();
		
		var nftData = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		var equip = nftData.Inventory.Values.First();

		Assert.AreEqual(equip.Faction, EquipmentFaction.Chaos);
	}

	private async Task SyncData()
	{
		_state = await _app.ServerState.GetPlayerState("yolo");
		await _nftSync.SyncData(_state, "yolo");
		await _app.ServerState.UpdatePlayerState("yolo", _state.GetOnlyUpdatedState());
	}
	
	
	[Test]
	public async Task TestNotDuplicatingAlreadyOwned()
	{
		await SyncData();
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		var firstStateUniqueId = firstState.Inventory.Keys.First();

		_nftSync.Indexed.Add(new PolygonNFTMetadata()
		{
			token_id = "tokenid2",
			subCategory = (int) GameId.ModRifle,
			faction = (long) EquipmentFaction.Dimensional
		});
		_nftSync.LastUpdate++;

		await SyncData();

		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		Assert.AreEqual(1, firstState.Inventory.Keys.Count);
		Assert.AreEqual(1, firstState.NftInventory.Keys.Count);
		Assert.AreEqual(2, secondState.Inventory.Keys.Count);
		Assert.AreEqual(2, secondState.NftInventory.Keys.Count);
		Assert.That(secondState.Inventory.Keys.Contains(firstStateUniqueId));
	}
	
	[Test]
	public async Task TestNftSyncTriggeringRemoveNftAnalytics()
	{
		await SyncData();
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_nftSync.Indexed.Clear();
		_nftSync.LastUpdate++;

		await SyncData();
		_app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var nftRemovedEvents = _analytics.FiredEvents.Where(e => e.Name == "nft_remove").ToList();

		Assert.That(nftRemovedEvents.Count == firstState.NftInventory.Count);
	}
	
	
	[Test]
	public async Task TestRemovingFromGameWhenRemovedFromBlockchain()
	{
		await SyncData();

		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_nftSync.Indexed.Clear();
		_nftSync.LastUpdate++;

		await SyncData();

		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		Assert.AreEqual(1, firstState.Inventory.Keys.Count);
		Assert.AreEqual(1, firstState.NftInventory.Keys.Count);
		Assert.AreEqual(0, secondState.Inventory.Keys.Count);
		Assert.AreEqual(0, secondState.NftInventory.Keys.Count);
	}
	
	
	[Test]
	public async Task TestRepairingTriggersSync()
	{
		await SyncData();
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var newRepairTime = 100;
		var nftMetadata = _nftSync.Indexed.First();
		nftMetadata.lastRepairTime = newRepairTime;
		_nftSync.LastUpdate++;

		await SyncData();
		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var nftDataBefore = firstState.Inventory.Values.First();
		var nftDataAfter = secondState.Inventory.Values.First();

		Assert.AreNotEqual(newRepairTime, nftDataBefore.LastRepairTimestamp);
		Assert.AreEqual(newRepairTime, nftDataAfter.LastRepairTimestamp);
	}

	
	[Test]
	public async Task TestUpgradingTriggersSync()
	{
		await SyncData();
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var newLevel = 10;
		var nftMetadata = _nftSync.Indexed.First();
		nftMetadata.level = newLevel;
		_nftSync.LastUpdate++;

		await SyncData();

		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var itemBefore = firstState.Inventory.Values.First();
		var itemAfter = secondState.Inventory.Values.First();

		Assert.AreNotEqual(newLevel, itemBefore.Level);
		Assert.AreEqual(newLevel, itemAfter.Level);
	}
}
*/