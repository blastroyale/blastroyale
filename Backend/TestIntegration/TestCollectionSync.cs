using System.Collections.Generic;
using System.Threading.Tasks;
using BlastRoyaleNFTPlugin;
using FirstLight.Game.Data;
using NUnit.Framework;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.DependencyInjection;
using PlayFab;
using Quantum;
using ServerCommon.Cloudscript;
using Assert = NUnit.Framework.Assert;

namespace IntegrationTests
{
	public class TestCollectionSync
	{
		private TestLogicServer _server;
		private string _playerId;
		private BlockchainApi _nftSync;

		[SetUp]
		public void Setup()
		{
			_server = new TestLogicServer();
			_playerId = PlayFabClientAPI.LoginWithCustomIDAsync(new()
			{
				CustomId = "BACKEND_INTEGRATION_TEST", CreateAccount = false
			}).GetAwaiter().GetResult().Result.PlayFabId;
			var pluginLogger = _server.Services.GetService<IPluginLogger>();
			var eventManager = new PluginEventManager(pluginLogger);
			var pluginSetup = new PluginContext(eventManager, _server.Services);
			_nftSync = new BlockchainApi("***REMOVED***", "devkey", pluginSetup);
		}

		[Test]
		public async Task TestFetchingOwned()
		{
			var polygons = await _nftSync.CollectionsSync.RequestCollection<RemoteCollectionItem>(_playerId,
				CollectionsSync.COLLECTION_CORPOS_POLYGON);


			Assert.IsTrue(polygons != null);
		}
		
		[Test]
		public async Task TestSyncCollections()
		{
			var state = await _server.Services.GetRequiredService<IServerStateService>().GetPlayerState(_playerId);
			var suceess = await _nftSync.SyncData(state, _playerId);

			Assert.IsTrue(suceess);
		}
	}
}
