using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
using PlayFab;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace FirstLight.Game
{
	/// <summary>
	/// Game boot
	/// </summary>
	public class Load : MonoBehaviour
	{

		private NotificationStateMachine _notificationState;
		private GameStateMachine _gameState;
		
		private void Awake()
		{
			FLog.Init();
			var messageBroker = new InMemoryMessageBrokerService();
			var timeService = new TimeService();
			var dataService = new DataService();
			var uiService = new GameUiService(new UiAssetLoader());
			var assetResolver = new AssetResolverService();
			var genericDialogService = new GenericDialogService(uiService);
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();
			var configsProvider = new ConfigsProvider();
			var networkService = new GameNetworkService(configsProvider);
			var tutorialService = new TutorialService(uiService);

			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider, audioFxService);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService,
				configsProvider, gameLogic, genericDialogService, assetResolver, tutorialService, vfxService,
				audioFxService, uiService);

			networkService.BindServicesAndData(gameLogic, gameServices);
			networkService.EnableClientUpdate(true);
			networkService.EnableQuantumPingCheck(true);
			tutorialService.BindServicesAndData(gameLogic, gameServices);

			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);
			
			FLog.Verbose($"Initialized client version {VersionUtils.VersionExternal}");

			_notificationState = new NotificationStateMachine(gameLogic, gameServices);
			_gameState = new GameStateMachine(gameLogic, gameServices, uiService, networkService,
				tutorialService,
				configsProvider, assetResolver, dataService, vfxService);
		}

		private void Start()
		{
			TrySetLocalServer();
			FlgCustomSerializers.RegisterSerializers();
			TouchSimulation.Enable();
			EnhancedTouchSupport.Enable();
			
			_notificationState.Run();
			_gameState.Run();
		}

		private void TrySetLocalServer()
		{
#if UNITY_EDITOR
			FeatureFlags.ParseLocalFeatureFlags();
			Debug.Log("Using local server? -" + FeatureFlags.GetLocalConfiguration().UseLocalServer);
#endif
		}
	}
}