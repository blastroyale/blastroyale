using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
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
		_plugin.NftSync = _nftSync;
		_analytics = _app.GetService<IServerAnalytics>() as InMemoryAnalytics;
		_nftSync.Indexed.Add(new PolygonNFTMetadata()
		{
			token_id = "tokenid1",
			subCategory = (int) GameId.ModPistol,
			faction = (long) EquipmentFaction.Chaos
		});
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
	public void TestEventTriggersSync()
	{
		var nftDataBefore = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_events.CallEvent(new PlayerDataLoadEvent("yolo"));

		var nftDataAfter = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		Assert.AreEqual(0, nftDataBefore.Inventory.Keys.Count);
		Assert.AreEqual(0, nftDataBefore.NftInventory.Keys.Count);
		Assert.AreEqual(1, nftDataAfter.Inventory.Keys.Count);
		Assert.AreEqual(1, nftDataAfter.NftInventory.Keys.Count);
	}

	[Test]
	public async Task TestInventorySync()
	{
		await _nftSync.SyncAllNfts("yolo");

		var nftData = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		var equip = nftData.Inventory.Values.First();

		Assert.AreEqual(equip.Faction, EquipmentFaction.Chaos);
	}
	
	[Test]
	public async Task TestAlreadyInSync()
	{
		Assert.IsTrue(await _nftSync.SyncAllNfts("yolo"));
		Assert.IsFalse(await _nftSync.SyncAllNfts("yolo"));
	}

	[Test]
	public async Task TestSyncSkipIfUpToDate()
	{
		var state = _app.ServerState.GetPlayerState("yolo").Result;
		var equips = state.DeserializeModel<EquipmentData>();
		equips.LastUpdateTimestamp = _nftSync.LastUpdate + 1;
		state.UpdateModel(equips);
		_app.ServerState.UpdatePlayerState("yolo", state).Wait();

		await _nftSync.SyncAllNfts("yolo");

		var nftData = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		Assert.AreEqual(0, nftData.Inventory.Count());
		Assert.AreEqual(0, nftData.NftInventory.Count());
	}

	[Test]
	public async Task TestNotDuplicatingAlreadyOwned()
	{
		await _nftSync.SyncAllNfts("yolo");
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		var firstStateUniqueId = firstState.Inventory.Keys.First();

		_nftSync.Indexed.Add(new PolygonNFTMetadata()
		{
			token_id = "tokenid2",
			subCategory = (int)GameId.ModRifle,
			faction = (long) EquipmentFaction.Dimensional
		});
		_nftSync.LastUpdate++;

		await _nftSync.SyncAllNfts("yolo");

		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		Assert.AreEqual(1, firstState.Inventory.Keys.Count);
		Assert.AreEqual(1, firstState.NftInventory.Keys.Count);
		Assert.AreEqual(2, secondState.Inventory.Keys.Count);
		Assert.AreEqual(2, secondState.NftInventory.Keys.Count);
		Assert.That(secondState.Inventory.Keys.Contains(firstStateUniqueId));
	}

	[Test]
	public async Task TestUpgradingTriggersSync()
	{
		await _nftSync.SyncAllNfts("yolo");
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var newLevel = 10;
		var nftMetadata = _nftSync.Indexed.First();
		nftMetadata.level = newLevel;
		_nftSync.LastUpdate++;
		
		await _nftSync.SyncAllNfts("yolo");
		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var itemBefore = firstState.Inventory.Values.First();
		var itemAfter = secondState.Inventory.Values.First();

		Assert.AreNotEqual(newLevel, itemBefore.Level);
		Assert.AreEqual(newLevel, itemAfter.Level);
	}

	[Test]
	public async Task TestRepairingTriggersSync()
	{
		await _nftSync.SyncAllNfts("yolo");
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var newRepairTime = 100;
		var nftMetadata = _nftSync.Indexed.First();
		nftMetadata.lastRepairTime = newRepairTime;
		_nftSync.LastUpdate++;
	
		await _nftSync.SyncAllNfts("yolo");
		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var nftDataBefore = firstState.Inventory.Values.First();
		var nftDataAfter = secondState.Inventory.Values.First();

		Assert.AreNotEqual(newRepairTime, nftDataBefore.LastRepairTimestamp);
		Assert.AreEqual(newRepairTime, nftDataAfter.LastRepairTimestamp);
	}

	[Test]
	public async Task TestRemovingFromGameWhenRemovedFromBlockchain()
	{
		await _nftSync.SyncAllNfts("yolo");
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_nftSync.Indexed.Clear();
		_nftSync.LastUpdate++;
		
		await _nftSync.SyncAllNfts("yolo");
		var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		Assert.AreEqual(1, firstState.Inventory.Keys.Count);
		Assert.AreEqual(1, firstState.NftInventory.Keys.Count);
		Assert.AreEqual(0, secondState.Inventory.Keys.Count);
		Assert.AreEqual(0, secondState.NftInventory.Keys.Count);
	}

	[Test]
	public void TestNftSyncTriggeringAddNftAnalytics()
	{
		_app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_events.CallEvent(new PlayerDataLoadEvent("yolo"));

		var nftDataAfter = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		var nftAddedEvents = _analytics.FiredEvents.Where(e => e.Name == "nft_add").ToList();

		Assert.That(nftAddedEvents.Count == nftDataAfter.NftInventory.Count);
	}

	[Test]
	public async Task TestNftSyncTriggeringRemoveNftAnalytics()
	{
		await _nftSync.SyncAllNfts("yolo");
		var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_nftSync.Indexed.Clear();
		_nftSync.LastUpdate++;

		await _nftSync.SyncAllNfts("yolo");
		_app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		var nftRemovedEvents = _analytics.FiredEvents.Where(e => e.Name == "nft_remove").ToList();

		Assert.That(nftRemovedEvents.Count == firstState.NftInventory.Count);
	}
}