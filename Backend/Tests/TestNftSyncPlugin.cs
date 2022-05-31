using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Quantum;
using ServerSDK;
using ServerSDK.Events;
using ServerSDK.Services;
using Tests.Stubs;
using Assert = NUnit.Framework.Assert;

namespace Tests;

public class TestNftSyncPlugin
{
	private BlastRoyaleNftPlugin _plugin;
	private StubbedNftSync _nftSync;
	private PluginEventManager _events;
	private TestServer _app;

	[SetUp]
	public void Setup()
	{
		_app = new TestServer();
		_app.UpdateDependencies(services =>
		{
			services.RemoveAll(typeof(IServerStateService));
			services.AddSingleton<IServerStateService, InMemoryPlayerState>();
		});
		var log = _app.Services.GetService<ILogger>();
		_events = new PluginEventManager(log);
		var pluginCtx = new PluginContext(_events, _app.Services);
		_nftSync = new StubbedNftSync(pluginCtx);
		_plugin = new BlastRoyaleNftPlugin();
		_plugin.OnEnable(pluginCtx);
		_plugin.NftSync = _nftSync;
		_nftSync.Indexed.Add(new PolygonNFTMetadata()
		{
			token_id = "tokenid1",
			name = "Assault Rifle",
			faction = (long)EquipmentFaction.Chaos
		});

	}

	[Test]
	public void TestEventTriggersSync()
	{
		var nftDataBefore = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();
		
		_events.CallEvent(new PlayerDataLoadEvent("yolo"));
		
		var nftDataAfter = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();
		
		Assert.AreEqual(0, nftDataBefore.Inventory.Keys.Count);
		Assert.AreEqual(1, nftDataAfter.Inventory.Keys.Count);
	}
	
	[Test]
    public async Task TestInventorySync()
    {
	    await _nftSync.SyncAllNfts("yolo");
        
        var nftData = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();
        var equip = nftData.Inventory.Values.First();
        
        Assert.AreEqual(equip.GameId, GameId.AssaultRifle);
        Assert.AreEqual(equip.Faction, EquipmentFaction.Chaos);
    }
    
    [Test]
    public async Task TestSyncSkipIfUpToDate()
    {
        var state = _app.ServerState.GetPlayerState("yolo");
        var equips = state.DeserializeModel<NftEquipmentData>();
        equips.LastUpdateTimestamp = _nftSync.LastUpdate + 1;
        state.SetModel(equips);
        _app.ServerState.UpdatePlayerState("yolo", state);
     
        await _nftSync.SyncAllNfts("yolo");
        
        var nftData = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();
        
        Assert.AreEqual(0, nftData.Inventory.Count());
    }
    
    [Test]
    public async Task TestNotDuplicatingAlreadyOwned()
    {
	 
	    await _nftSync.SyncAllNfts("yolo");
        var firstState = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();
        var firstStateUniqueId = firstState.Inventory.Keys.First();
        
        _nftSync.Indexed.Add(new PolygonNFTMetadata()
        {
	        token_id = "tokenid2",
	        name = "RPG",
	        faction = (long)EquipmentFaction.Dimensional
        });
        
        await _nftSync.SyncAllNfts("yolo");
        
        var secondState = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();

        Assert.AreEqual(1, firstState.Inventory.Keys.Count);
        Assert.AreEqual(2, secondState.Inventory.Keys.Count);
        Assert.That(secondState.Inventory.Keys.Contains(firstStateUniqueId));
    }
    
    [Test]
    public async Task TestRemovingFromGameWhenRemovedFromBlockchain()
    {
	    await _nftSync.SyncAllNfts("yolo");
        var firstState = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();
        
        _nftSync.Indexed.Clear();
        
        await _nftSync.SyncAllNfts("yolo");
        var secondState = _app.ServerState.GetPlayerState("yolo").DeserializeModel<NftEquipmentData>();

        Assert.AreEqual(1, firstState.Inventory.Keys.Count);
        Assert.AreEqual(0, secondState.Inventory.Keys.Count);
    }

}