using System;
using System.Linq;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data.DataTypes;
using NUnit.Framework;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using Microsoft.Extensions.DependencyInjection;
using PlayFab;
using Assert = NUnit.Framework.Assert;

namespace IntegrationTests
{
	public class TestCollectionSync
	{
		private TestLogicServer _server;

		protected string PlayerId;
		protected BlockchainApi NftSync;
		protected BlastRoyalePlugin blastRoyalePlugin;

		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			_server = new TestLogicServer();

			PlayerId = PlayFabClientAPI.LoginWithCustomIDAsync(new()
			{
				CustomId = "NFTSYNC_INTEGRATION_TEST", CreateAccount = false
			}).GetAwaiter().GetResult().Result.PlayFabId;

			var pluginLogger = _server.Services.GetService<IPluginLogger>();
			var eventManager = new PluginEventManager(pluginLogger);
			var pluginSetup = new PluginContext(eventManager, _server.Services);
			blastRoyalePlugin = new BlastRoyalePlugin(_server.Services.GetService<IStoreService >(), _server.Services.GetService<IInventorySyncService<ItemData>>());
			blastRoyalePlugin.OnEnable(pluginSetup);
			NftSync = new BlockchainApi("***REMOVED***", "devkey");
		}


		[Test]
		public async Task TestSyncCollections_Success()
		{
			var state = await _server.Services.GetRequiredService<IServerStateService>().GetPlayerState(PlayerId);
			var success = await blastRoyalePlugin.SyncCollections(state, PlayerId);

			Assert.IsTrue(success);
		}

		// 	[Test]
		// 	public async Task TestSyncCollections_Success()
		// 	{
		// 		var state = await _server.Services.GetRequiredService<IServerStateService>().GetPlayerState(PlayerId);
		// 		var success = await NftSync.SyncData(state, PlayerId);
		//
		// 		Assert.IsTrue(success);
		// 	}
		// 	
		// 	[Test]
		// 	public async Task TestFetchingAnyCollectionOwned_EmptyPlayerId_Fail()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(string.Empty,
		// 			CollectionsSyncService.COLLECTION_CORPOS_POLYGON);
		// 	
		// 		Assert.IsTrue(collection != null);
		// 		Assert.AreEqual(collection!.Owned.Count(), 0);
		// 	}
		// 	
		// 	[Test]
		// 	public async Task TestFetchingAnyCollectionOwned_InvalidPlayerId_Fail()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>("InvalidPlayerId",
		// 			CollectionsSyncService.COLLECTION_CORPOS_POLYGON);
		// 	
		// 		Assert.IsTrue(collection != null);
		// 		Assert.AreEqual(collection!.Owned.Count(), 0);
		// 	}
		// 	
		// 	[Test]
		// 	public async Task TestFetchingAnyCollectionOwned_EmptyCollectionName_Fail()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			string.Empty);
		// 	
		// 		Assert.IsTrue(collection != null);
		// 		Assert.AreEqual(collection!.Owned.Count(), 0);
		// 	}
		// 	
		// 	[Test]
		// 	public async Task TestFetchingAnyCollectionOwned_InvalidCollectionName_Fail()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			string.Empty);
		// 	
		// 		Assert.IsTrue(collection != null);
		// 		Assert.AreEqual(collection!.Owned.Count(), 0);
		// 	}
		// 	
		// 	[Test]
		// 	public async Task TestFetchingOwnedCorposPolygon_Success()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			CollectionsSyncService.COLLECTION_CORPOS_POLYGON, true);
		//
		//
		// 		Assert.IsTrue(collection != null);
		// 		Assert.Greater(collection!.Owned.Count(), 0);
		// 	}
		//
		// 	[Test]
		// 	public async Task TestFetchingOwnedCorposEth_Success()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			CollectionsSyncService.COLLECTION_CORPOS_ETH, true);
		//
		//
		// 		Assert.IsTrue(collection != null);
		// 		Assert.Greater(collection!.Owned.Count(), 0);
		// 	}
		//
		// 	[Test]
		// 	public async Task TestFetchingOwnedGamesGGGamersEth_Success()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			CollectionsSyncService.COLLECTION_GAMESGG_GAMERS_ETH);
		//
		//
		// 		Assert.IsTrue(collection != null);
		// 		// Assert.Greater(collection!.Owned.Count(), 0); The Logic works but this assertion can be trick since
		// 		// this contract is Deployed on mainnet and the Token can be transfered to another wallet
		// 	}
		// 	
		// 	[Test]
		// 	public async Task TestFetchingOwnedPlagueDoctorImx_Success()
		// 	{
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			CollectionsSyncService.COLLECTION_PLAGUE_DOCTOR_IMX, NftSync.CanSyncCollection(CollectionsSyncService.COLLECTION_PLAGUE_DOCTOR_IMX));
		//
		//
		// 		Assert.IsTrue(collection != null);
		// 		// Assert.Greater(collection!.Owned.Count(), 0); The Logic works but this assertion can be trick since
		// 		// this contract is Deployed on mainnet and the Token can be transfered to another wallet
		// 	}
		// 	
		// 	
		// 	[Test]
		// 	public async Task TestSyncDisabledToCorposEth_Success()
		// 	{
		// 		Environment.SetEnvironmentVariable($"{CollectionsSyncService.COLLECTION_CORPOS_ETH.ToUpperInvariant()}_SYNC_ENABLED", "false");
		// 		
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			CollectionsSyncService.COLLECTION_CORPOS_ETH, NftSync.CanSyncCollection(CollectionsSyncService.COLLECTION_CORPOS_ETH));
		//
		//
		// 		Assert.IsTrue(collection != null);
		// 		Assert.AreEqual(collection!.Owned.Count(), 0);
		// 	}
		//
		// 	[Test]
		// 	public async Task TestSyncEnabledToCorposEth_Success()
		// 	{
		// 		Environment.SetEnvironmentVariable($"{CollectionsSyncService.COLLECTION_CORPOS_ETH.ToUpperInvariant()}_SYNC_ENABLED", "true");
		// 		
		// 		var collection = await NftSync.CollectionsSyncService.RequestCollection<RemoteCollectionItem>(PlayerId,
		// 			CollectionsSyncService.COLLECTION_CORPOS_ETH, NftSync.CanSyncCollection(CollectionsSyncService.COLLECTION_CORPOS_ETH));
		//
		//
		// 		Assert.IsTrue(collection != null);
		// 		Assert.Greater(collection!.Owned.Count(), 0);
		// 	}
	}
}