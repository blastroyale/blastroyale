using System;
using System.IO;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
using FirstLight.UIService;
using NUnit.Framework;
using PlayFab;
using Src.FirstLight.Server.ServerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FirstLight.Tests.EditorMode
{
	/// <summary>
	/// Minimal test fixture which include services, current configuration and initial data provider.
	/// It will use serialized configs from the backend folder to ensure they are valid.
	/// This means it will use only the configs available to backend.
	/// </summary>
	public abstract class IntegrationTestFixture
	{
		private static string _serverResourcesPath => $"{Application.dataPath}/../Backend/ServerCommon/Resources";

		protected string TestPlayerId;
		protected IGameServices TestServices;
		protected ITimeManipulator TimeService;
		protected GameLogic TestLogic;
		protected DataService TestData;
		protected ConfigsProvider TestConfigs;
		protected GameStateMachine TestStates;
		protected GameNetworkService TestNetwork;
		protected TutorialService TestTutorial;
		protected AssetResolverService TestAssetResolver = new ();
		protected VfxService<VfxId> TestVfx;

		[OneTimeSetUp]
		public void SetupOnce()
		{
			var serializedConfig = File.ReadAllText($"{_serverResourcesPath}/gameConfig.json");
			TestConfigs = new ConfigsSerializer().Deserialize<ConfigsProvider>(serializedConfig);

			// TODO: Fix async issue with asset resolver on NUnit
			// TestStates.Run();      // Not working due to async asset loading
			WorkaroundForStateRun(); // Workaround for now
		}

		[SetUp]
		public void Setup()
		{
			var messageBroker = new InMemoryMessageBrokerService();
			TimeService = new TimeService();
			TestNetwork = new GameNetworkService(TestConfigs);
			var audioFxService = new GameAudioFxService(TestAssetResolver);
			
			TestVfx = new VfxService<VfxId>();

			TestData = SetupPlayer(TestConfigs);
			TestLogic = new GameLogic(messageBroker, TimeService, TestData, TestConfigs,
				audioFxService);

			TestServices = new StubGameServices(TestNetwork, messageBroker, TimeService, TestData,
			                                    TestConfigs, TestLogic, TestData,
			                                    TestAssetResolver, TestTutorial, TestVfx, audioFxService);
			TestNetwork.StartNetworking(TestLogic, TestServices);
			TestLogic.Init();

			TestStates = new GameStateMachine(TestLogic, TestServices, TestNetwork, TestAssetResolver);
#if DEVELOPMENT_BUILD
			Statechart.Statechart.OnStateTimed = null;
#endif

			FLog.Init();

			var integrationAppData = TestData.GetData<AccountData>();
			integrationAppData.DeviceId = "integration_test";
			//TestData.SaveData<AppData>();
			PlayFabSettings.staticSettings.TitleId = "***REMOVED***";

			MainInstaller.Bind<IGameDataProvider>(TestLogic);
			MainInstaller.Bind<IGameServices>(TestServices);
		}

		[TearDown]
		public void TearDown()
		{
			MainInstaller.Clean<IGameDataProvider>();
			MainInstaller.Clean<IGameServices>(); 
		}

		protected async Task WaitFor(Func<bool> condition, int timeoutMillis = 10000)
		{
			var timeouts = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMillis);
			while (DateTime.UtcNow < timeouts && !condition())
			{
				await Task.Delay(TimeSpan.FromMilliseconds(100));
			}
		}

		private DataService SetupPlayer(IConfigsProvider configs)
		{
			TestPlayerId = Guid.NewGuid().ToString();
			var playerSetup = new BlastRoyalePlayerSetup(configs);
			var initialServerState = playerSetup.GetInitialState(TestPlayerId);
			var data = new DataService();
			data.AddData(initialServerState.DeserializeModel<PlayerData>());
			data.AddData(initialServerState.DeserializeModel<EquipmentData>());
			data.AddData(initialServerState.DeserializeModel<RngData>());
			data.AddData(initialServerState.DeserializeModel<IdData>());
			data.AddData(new AppData());
			return data;
		}
		
		private void WorkaroundForStateRun()
		{
#pragma warning disable CS0612
			var quantumAddress = AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address;
			var quantumAsset = Addressables.LoadAsset<QuantumRunnerConfigs>(quantumAddress).WaitForCompletion();
			TestConfigs.AddSingletonConfig(quantumAsset);
#pragma warning restore CS0612
		}
	}
}