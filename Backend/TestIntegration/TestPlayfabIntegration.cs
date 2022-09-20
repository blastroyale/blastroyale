using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Data;
using NUnit.Framework;
using FirstLight.Server.SDK.Services;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using IntegrationTests.Setups;
using Quantum;
using Assert = NUnit.Framework.Assert;

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
			Assert.AreEqual(await fabConfig.GetRemoteVersion(), currentConfig.Version);
		}

		[Test]
		public async Task TestPlayfabConfigUpdate()
		{
			var configs = _server.GetService<IConfigsProvider>() as PlayfabConfigurationProvider;

			var levelConfigs = configs.GetAllConfigs()[typeof(PlayerLevelConfig)] as Dictionary<int, PlayerLevelConfig>;
			
			var currentVersion = configs.Version;
			var deprecatedVersion = currentVersion - 1;
			
			// simulating outdated configs
			var levelConfig = levelConfigs[1];
			var oldValue = levelConfig.LevelUpXP;
			levelConfig.LevelUpXP = oldValue / 2;
			levelConfigs[1] = levelConfig;
			configs.UpdateTo(deprecatedVersion, configs.GetAllConfigs());

			var levelConfigsAfter = configs.GetAllConfigs()[typeof(PlayerLevelConfig)] as Dictionary<int, PlayerLevelConfig>;
			var firstConfig = levelConfigsAfter[1];
			Assert.That(firstConfig.LevelUpXP != oldValue);
			Assert.That(configs.Version == deprecatedVersion);
			
			// Sending a command should trigger the update
			_server.SendTestCommand(new UpdatePlayerSkinCommand()
			{
				SkinId = GameId.Female01Avatar
			});
			
			firstConfig = configs.GetConfigsList<PlayerLevelConfig>().First();
			Assert.That(firstConfig.LevelUpXP == oldValue);
			Assert.That(configs.Version == currentVersion);
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
