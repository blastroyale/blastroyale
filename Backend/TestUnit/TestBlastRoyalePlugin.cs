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
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using Assert = NUnit.Framework.Assert;

public class TestNftSyncPlugin
{
	private BlastRoyalePlugin _plugin;
	private StubbedNftSync _nftSync;
	private PluginEventManager _events;
	private TestServer _app;

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
		_nftSync.Indexed.Add(new PolygonNFTMetadata()
		{
			token_id = "tokenid1",
			name = ((GameId)1).ToString(),
			faction = (long)EquipmentFaction.Chaos
		});
	}

	[Test]
	public void TestQuantumVector3Serialialization()
	{

		FP CU = FP.FromRaw(0);
		
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
	        name = ((GameId)1).ToString(),
	        faction = (long)EquipmentFaction.Dimensional
        });
        
        await _nftSync.SyncAllNfts("yolo");
        
        var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

        Assert.AreEqual(1, firstState.Inventory.Keys.Count);
        Assert.AreEqual(1, firstState.NftInventory.Keys.Count);
        Assert.AreEqual(2, secondState.Inventory.Keys.Count);
        Assert.AreEqual(2, secondState.NftInventory.Keys.Count);
        Assert.That(secondState.Inventory.Keys.Contains(firstStateUniqueId));
    }
    
    [Test]
    public async Task TestRemovingFromGameWhenRemovedFromBlockchain()
    {
	    await _nftSync.SyncAllNfts("yolo");
        var firstState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();
        
        _nftSync.Indexed.Clear();
        
        await _nftSync.SyncAllNfts("yolo");
        var secondState = _app.ServerState.GetPlayerState("yolo").Result.DeserializeModel<EquipmentData>();

        Assert.AreEqual(1, firstState.Inventory.Keys.Count);
        Assert.AreEqual(1, firstState.NftInventory.Keys.Count);
        Assert.AreEqual(0, secondState.Inventory.Keys.Count);
        Assert.AreEqual(0, secondState.NftInventory.Keys.Count);
    }

}