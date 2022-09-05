using FirstLight.Game.Data;
using NUnit.Framework;
using FirstLight.Server.SDK.Services;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using IntegrationTests.Setups;

namespace IntegrationTests
{
	public class TestPlayfabIntegration
	{
		private TestServer _server = null!;
		private IServerStateService _stateService = null!;

		[SetUp]
		public void Setup()
		{
			_server = new TestServer(IntegrationSetup.GetIntegrationConfiguration());
			_stateService = _server.GetService<IServerStateService>()!;
			var playfab = _server.GetService<IPlayfabServer>();
			Assert.NotNull(playfab);
		}

		[Test]
		public async Task TestFetchingConfigFromPlayfab()
		{
			var currentConfig = _server.GetService<IConfigsProvider>();

			var fabConfig = new PlayfabConfigurationBackendService();
			Assert.AreEqual(await fabConfig.GetVersion(), currentConfig.Version);
		}

		/// <summary>
		/// Test for playfab delta tracking.
		/// The server should only update keys on playfab that were updated by logic.
		/// </summary>
		[Test]
		public void TestOnlyUpdatingSingleKeyThatUpdated()
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
