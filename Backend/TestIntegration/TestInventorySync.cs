using System.Threading.Tasks;
using FirstLight.Game.Data;
using NUnit.Framework;
using FirstLight.Game.Logic.RPC;
using FirstLight.Server.SDK.Services;
using Microsoft.Extensions.DependencyInjection;
using PlayFab;
using Quantum;
using ServerCommon.Cloudscript;
using Assert = NUnit.Framework.Assert;

namespace IntegrationTests
{
	public class TestInventorySync
	{
		private TestLogicServer _server;
		private string _playerId;

		[SetUp]
		public void Setup()
		{
			_server = new TestLogicServer();
			_playerId = PlayFabClientAPI.LoginWithCustomIDAsync(new()
			{
				CustomId = "integration_test", CreateAccount = true
			}).GetAwaiter().GetResult().Result.PlayFabId;
		}

		[Test]
		public async Task TestSyncPlayfabInventory()
		{
			var invResponse = await PlayFabServerAPI.GetUserInventoryAsync(new()
			{
				PlayFabId = _playerId
			});
			Assert.IsNull(invResponse.Error);

			invResponse.Result.VirtualCurrency.TryGetValue("CS", out var currentCs);
			var newCs = currentCs + 10;
			var result = await PlayFabServerAPI.AddUserVirtualCurrencyAsync(new()
			{
				Amount = newCs, PlayFabId = _playerId, VirtualCurrency = "CS"
			});
			Assert.IsNull(result.Error);

			var request = new CloudscriptRequest<LogicRequest>(_playerId);
			var stateBefore = await _server.Services.GetService<IServerStateService>()!.GetPlayerState(_playerId);
			var playerDataBefore = stateBefore.DeserializeModel<PlayerData>();
			playerDataBefore.Currencies.TryGetValue(GameId.CS, out var expectedCs);
			expectedCs += (ulong)newCs;
			var response = _server.PostAndGetResponse("/CloudScript/SyncPlayfabInventory?key=devkey", request);
			
			Assert.IsTrue(response.IsSuccessStatusCode, response.ReasonPhrase);

			var state = await _server.Services.GetService<IServerStateService>()!.GetPlayerState(_playerId);
			var playerData = state.DeserializeModel<PlayerData>();
			
			Assert.AreEqual(expectedCs, playerData.Currencies[GameId.CS]);
		}
	}
}
