using FirstLight.Game.Data;
using FirstLight.Game.Utils;
using NUnit.Framework;
using ServerSDK.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerSDK.Modules;

namespace IntegrationTests
{
	public class TestPlayfabIntegration
	{
		private TestServer _server = null!;
		private IServerStateService _stateService = null!;

		[SetUp]
		public void Setup()
		{
			_server = new TestServer();
			_stateService = _server.GetService<IServerStateService>()!;
		}

		[Test]
		public void TestOnlyUpdatingSingleKey()
		{
			var playerId = _server.GetTestPlayerID();
			var data = _stateService.GetPlayerState(playerId).Result;

			var playerData = data.DeserializeModel<PlayerData>();
			playerData.Xp = 12345;
			var equipData = new EquipmentData() { LastUpdateTimestamp = 1234 };

			data.UpdateModel(playerData);
			ModelSerializer.SerializeToData(data, equipData); // bypassing update serializing directly

			_stateService.UpdatePlayerState(playerId, data).Wait();

			var secondReadData = _stateService.GetPlayerState(playerId).Result;
			var secondPlayer = secondReadData.DeserializeModel<PlayerData>();
			var secondEquipment = secondReadData.DeserializeModel<EquipmentData>();
			
			Assert.AreEqual(playerData.Xp, secondPlayer.Xp);
			Assert.AreNotEqual(equipData.LastUpdateTimestamp, secondEquipment.LastUpdateTimestamp);
		}
	}
}
