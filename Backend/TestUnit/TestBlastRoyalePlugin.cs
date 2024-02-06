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

public class TestNftSyncPlugin
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
	public async Task TestSyncSkipIfUpToDate()
	{
		var state = _app.ServerState.GetPlayerState("yolo").Result;
		var equips = state.DeserializeModel<EquipmentData>();
		equips.LastUpdateTimestamp = _nftSync.LastUpdate + 1;
		state.UpdateModel(equips);
		_app.ServerState.UpdatePlayerState("yolo", state).Wait();
		_state = state;
		await SyncData();

		var nftData = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		Assert.AreEqual(0, nftData.Inventory.Count());
		Assert.AreEqual(0, nftData.NftInventory.Count());
	}

	private async Task SyncData()
	{
		_state = await _app.ServerState.GetPlayerState("yolo");
		await _nftSync.SyncData(_state, "yolo");
		await _app.ServerState.UpdatePlayerState("yolo", _state.GetOnlyUpdatedState());
	}


	[Test]
	public void TestNftSyncTriggeringAddNftAnalytics()
	{
		_app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

		_events.CallEvent(new PlayerDataLoadEvent("yolo", null));

		var nftDataAfter = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
		var nftAddedEvents = _analytics.FiredEvents.Where(e => e.Name == "nft_add").ToList();

		Assert.That(nftAddedEvents.Count == nftDataAfter.NftInventory.Count);
	}


}