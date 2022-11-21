using System.Collections.Generic;
using NUnit.Framework;
using FirstLight.Game.Infos;
using FirstLight.Server.SDK.Modules;
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
	}
}
