using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using ServerCommon.Cloudscript.Models;
using FirstLight.Game.Data;
using NUnit.Framework;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;
using GameLogicService.Game;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ServerModels;
using Quantum;
using ServerCommon.Cloudscript;
using Assert = NUnit.Framework.Assert;

namespace IntegrationTests
{
	public class TestCloudscriptController
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

		public CloudscriptRequest<LogicRequest> MockRequest(LogicRequest request)
		{
			return new CloudscriptRequest<LogicRequest>()
			{
				FunctionArgument = request,
				CallerEntityProfile = new ()
				{
					Entity = new ()
					{
						Id = _playerId
					},
					Lineage = new ()
					{
						TitlePlayerAccountId = _playerId,
						MasterPlayerAccountId = _playerId
					}
				}
			};
		}

		[Test]
		public async Task TestGetPublicProfile()
		{
			var r = await PlayFabServerAPI.UpdatePlayerStatisticsAsync(new()
			{
				PlayFabId = _playerId,
				Statistics = new List<StatisticUpdate>()
				{
					new() {StatisticName = GameConstants.Stats.LEADERBOARD_LADDER_NAME, Value = 666}
				}
			});
			Assert.IsNull(r.Error);

			var response = _server.Post("/cloudscript/GetPublicProfile?key=devkey", MockRequest(new LogicRequest()
			{
				Command = _playerId
			}));
			
			var result = ModelSerializer.Deserialize<PlayFabResult<BackendLogicResult>>(response);
			var profile = ModelSerializer.DeserializeFromData<PublicPlayerProfile>(result.Result.Data);
			var rankedStat = profile.Statistics.First(s => s.Name == GameConstants.Stats.LEADERBOARD_LADDER_NAME);
			
			Assert.AreEqual(666, rankedStat.Value);
		}
	}
}