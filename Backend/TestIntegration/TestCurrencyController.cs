using System;
using System.Net;
using System.Threading.Tasks;
using ServerCommon.Cloudscript.Models;
using FirstLight.Game.Data;
using NUnit.Framework;
using FirstLight.Server.SDK.Services;
using PlayFab;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace IntegrationTests
{
	public class TestCurrencyController
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
		public async Task TestGettingCurrencyAmount()
		{
			var csAmmount = 1234ul;
			var stateService = _server.Services.GetService(typeof(IServerStateService)) as IServerStateService;
			var state = await stateService.GetPlayerState(_playerId);
			var playerData = state.DeserializeModel<PlayerData>();
			playerData.Currencies[GameId.CS] = csAmmount;
			state.UpdateModel(playerData);
			await stateService.UpdatePlayerState(_playerId, state);

			var response = _server.Get($"/currency/getcurrency?key=devkey&playerId={_playerId}&currencyId={(int) GameId.CS}");

			Assert.AreEqual(Int32.Parse(response), csAmmount);
		}

		[Test]
		public async Task TestSettingCurrencyAmount()
		{
			var initialCS = 1000ul;
			var csDelta = -10;
			var stateService = _server.Services.GetService(typeof(IServerStateService)) as IServerStateService;
			var state = await stateService.GetPlayerState(_playerId);
			var playerData = state.DeserializeModel<PlayerData>();
			playerData.Currencies[GameId.CS] = initialCS;
			state.UpdateModel(playerData);
			await stateService.UpdatePlayerState(_playerId, state);

			var result = _server.Post("/currency/modifycurrency?key=devkey", new CurrencyUpdateRequest()
			{
				Delta = csDelta,
				CurrencyId = (int) GameId.CS,
				PlayerId = _playerId
			});

			state = await stateService.GetPlayerState(_playerId);
			playerData = state.DeserializeModel<PlayerData>();

			var expected = Convert.ToInt64(initialCS) + csDelta;
			Assert.AreEqual(expected, Int32.Parse(result));
			Assert.AreEqual(expected, playerData.Currencies[GameId.CS]);
		}

		[Test]
		public async Task TestInsufficientFunds()
		{
			var initialCS = 1000ul;
			var csDelta = -1550;
			var stateService = _server.Services.GetService(typeof(IServerStateService)) as IServerStateService;
			var state = await stateService.GetPlayerState(_playerId);
			var playerData = state.DeserializeModel<PlayerData>();
			playerData.Currencies[GameId.CS] = initialCS;
			state.UpdateModel(playerData);
			await stateService.UpdatePlayerState(_playerId, state);

			var result = _server.PostAndGetResponse("/currency/modifycurrency?key=devkey", new CurrencyUpdateRequest()
			{
				Delta = csDelta,
				CurrencyId = (int) GameId.CS,
				PlayerId = _playerId
			});

			Assert.That(result.StatusCode == HttpStatusCode.Conflict);
		}
	}
}