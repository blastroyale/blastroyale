using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Serializers;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.TestCases;
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

		public delegate void OnGameLoadAwakeEvent();

		/// <summary>
		/// Event handler that notifies when the game is loaded
		/// </summary>
		public static event OnGameLoadAwakeEvent OnGameLoadAwake;

		

		private void Awake()
		{
			FLog.Init();
			FLGTestRunner.Instance.CheckFirebaseRun();
			FLGTestRunner.Instance.CheckAutomations();
			
			var messageBroker = new InMemoryMessageBrokerService();
			var timeService = new TimeService();
			var dataService = new DataService();
			var uiService = new GameUiService(new UiAssetLoader());
			var assetResolver = new AssetResolverService();
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();
			var configsProvider = new ConfigsProvider();
			var networkService = new GameNetworkService(configsProvider);
			var tutorialService = new TutorialService(uiService);

			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider, audioFxService);
			var genericDialogService = new GenericDialogService(uiService, gameLogic.CurrencyDataProvider);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService,
				configsProvider, gameLogic, genericDialogService, assetResolver, tutorialService, vfxService,
				audioFxService, uiService);

			networkService.StartNetworking(gameLogic, gameServices);
			networkService.EnableQuantumPingCheck(true);
			tutorialService.BindServicesAndData(gameLogic, gameServices);

			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);
			FLog.Verbose($"Initialized client version {VersionUtils.VersionExternal}");

			_notificationState = new NotificationStateMachine(gameLogic, gameServices);
			_gameState = new GameStateMachine(gameLogic, gameServices, uiService, networkService,
				tutorialService,
				configsProvider, assetResolver, dataService, vfxService);

			OnGameLoadAwake?.Invoke();
		}
		
		private void EnableLowLevelTraces()
		{
			Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.Full);
			Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.Full);
		}


		private void Start()
		{
#if DEVELOPMENT_BUILD || UNITY_EDITOR
			EnableLowLevelTraces();
#endif
			TrySetLocalServer();
			FlgCustomSerializers.RegisterAOT();
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