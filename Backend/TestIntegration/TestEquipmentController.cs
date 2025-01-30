using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using FirstLight.Game.Infos;
using FirstLight.Server.SDK.Modules;
using GameLogicService.Models;
using PlayFab;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace IntegrationTests
{
	public class TestEquipmentController
	{
		private TestLogicServer _server;
		
		[SetUp]
		public void Setup()
		{
			_server = new TestLogicServer();
		}

		[Test]
		public void TestGettingEquipmentStats()
		{
			var pistol = new Equipment()
			{
				Adjective = EquipmentAdjective.Magnificent,
				Edition = EquipmentEdition.Genesis,
				Faction = EquipmentFaction.Dark,
				GameId = GameId.ModPistol,
				Level = 2,
			};
			var response = _server.Post("/equipment/getstats?key=devkey", pistol);
			var responseStats = ModelSerializer.Deserialize<Dictionary<EquipmentStatType, float>>(response);
			
			Assert.Greater(responseStats[EquipmentStatType.Power], 0);
		}
		
		[Test]
		public async Task TestAddRemoveEquipment()
		{
			var playerId = PlayFabClientAPI.LoginWithCustomIDAsync(new()
			{
				CustomId = Guid.NewGuid().ToString(), CreateAccount = true
			}).GetAwaiter().GetResult().Result.PlayFabId;
			var token = Guid.NewGuid().ToString();
			var equip = new Equipment(GameId.ApoRifle);
			var uniqueId = _server.Post("/equipment/AddEquipment?key=devkey", new AddEquipmentRequest()
			{
				Equipment = equip ,
				PlayerId = playerId,
				TokenId = token
			});
			
			var equips = _server.Get("/equipment/GetEquipment?key=devkey&playerId="+playerId);
			Assert.That(equips.Contains(token));
		}
	}
}
