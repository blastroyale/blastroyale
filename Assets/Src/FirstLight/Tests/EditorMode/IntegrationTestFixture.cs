using System;
using System.IO;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
using NUnit.Framework;
using Src.FirstLight.Server.ServerServices;
using UnityEngine;

namespace FirstLight.Tests.EditorMode
{
	/// <summary>
	/// Minimal test fixture which include services, current configuration and initial data provider.
	/// It will use configs from the backend folder to ensure they are valid.
	/// </summary>
	public abstract class IntegrationTestFixture
	{
		private static string _backendPath => $"{Application.dataPath}/../Backend";
		
		protected string TestPlayerId;
		protected IGameServices TestServices;
		protected GameLogic TestLogic;
		protected IDataProvider TestData;
		protected IConfigsProvider TestConfigs;

		[SetUp]
		public void SetupPerTest()
		{
			var messageBroker = new MessageBrokerService();
			var timeService = new TimeService();
			
			var uiService = new GameUiService(new UiAssetLoader());
			var networkService = new GameNetworkService(TestConfigs);
			var assetResolver = new AssetResolverService();
			var genericDialogService = new GenericDialogService(uiService);
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();
			var playerInputService = new PlayerInputService();
			
			TestData = SetupPlayer(TestConfigs);
			TestLogic = new GameLogic(messageBroker, timeService, TestData, TestConfigs,
				audioFxService);
			TestServices = new StubGameServices(networkService, messageBroker, timeService, TestData as IDataSaver, 
				TestConfigs, TestLogic, TestData, genericDialogService, 
				assetResolver, vfxService, audioFxService, playerInputService);
		
			TestLogic.Init();
			
			MainInstaller.Bind<IGameDataProvider>(TestLogic);
			MainInstaller.Bind<IGameServices>(TestServices);
		}

		[TearDown]
		public void TearDown()
		{
			MainInstaller.Clean<IGameDataProvider>();
			MainInstaller.Clean<IGameServices>();
		}
		
		[OneTimeSetUp]
		public void Setup()
		{
			var serializedConfig = File.ReadAllText($"{_backendPath}/Backend/gameConfig.json");
			TestConfigs =  new ConfigsSerializer().Deserialize<ConfigsProvider>(serializedConfig);
		}
		
		private IDataProvider SetupPlayer(IConfigsProvider configs)
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
	}
}