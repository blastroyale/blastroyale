using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Serializers;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;

namespace FirstLight.Game
{
	/// <summary>
	/// Game boot
	/// </summary>
	public class Load : MonoBehaviour
	{
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
			var assetResolver = new AssetResolverService();
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();
			var configsProvider = new ConfigsProvider();
			var networkService = new GameNetworkService(configsProvider);
			var tutorialService = new TutorialService();
			var uiService2 = new UIService.UIService();
			uiService2.OpenScreen<LoadingScreenPresenter>().Forget();

			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider, audioFxService);
			var genericDialogService = new GenericDialogService(uiService2, gameLogic.CurrencyDataProvider);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService,
				configsProvider, gameLogic, genericDialogService, assetResolver, tutorialService, vfxService,
				audioFxService, uiService2);

			networkService.StartNetworking(gameLogic, gameServices);
			networkService.EnableQuantumPingCheck(true);
			tutorialService.BindServicesAndData(gameLogic, gameServices);

			MainInstaller.Bind<IWeb3Service>(new NoWeb3());
			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);

			FLog.Verbose($"Initialized client version {VersionUtils.VersionExternal}");

			_gameState = new GameStateMachine(gameLogic, gameServices, networkService,
				tutorialService,
				configsProvider, assetResolver, dataService, vfxService);

			MainInstaller.Bind<IGameStateMachine>(_gameState);
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