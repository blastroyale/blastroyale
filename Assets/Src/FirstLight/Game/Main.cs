using System.Collections;
using Facebook.Unity;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.StateMachines;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.UiService;
using PlayFab;
using UnityEditor;
using UnityEngine;

namespace FirstLight.Game
{
	/// <summary>
	/// The Main entry point of the game
	/// </summary>
	public class Main : MonoBehaviour
	{
		public IGameUiServiceInit UiService;

		private GameStateMachine _gameStateMachine;
		private NotificationStateMachine _notificationStateMachine;
		private IGameServices _services;
		private IGameLogic _gameLogic;
		private Coroutine _pauseCoroutine;

		private void Awake()
		{
			Application.targetFrameRate = 30;
			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			FLog.Init();

			var messageBroker = new MessageBrokerService();
			var timeService = new TimeService();
			var dataService = new DataService();
			var configsProvider = new ConfigsProvider();
			var uiService = new GameUiService(new UiAssetLoader());
			var networkService = new GameNetworkService(configsProvider);
			var assetResolver = new AssetResolverService();
			var genericDialogService = new GenericDialogService(uiService);
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();
			var threadService = new ThreadService();
			var gameFlowService = new GameFlowService();
			
			
			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider,
			                              audioFxService);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService,
			                                    configsProvider,
			                                    gameLogic, dataService, genericDialogService, assetResolver,
			                                    vfxService, audioFxService, threadService, gameFlowService);

			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);

			UiService = uiService;

			_gameLogic = gameLogic;
			_services = gameServices;
			_notificationStateMachine = new NotificationStateMachine(gameLogic, gameServices);
			_gameStateMachine = new GameStateMachine(gameLogic, gameServices, uiService, networkService,
			                                         configsProvider,
			                                         assetResolver, dataService, vfxService);

#if UNITY_EDITOR
			if (!EditorPrefs.HasKey(GameConstants.Editor.PREFS_ENABLE_STATE_MACHINE_DEBUG_KEY))
			{
				EditorPrefs.SetBool(GameConstants.Editor.PREFS_ENABLE_STATE_MACHINE_DEBUG_KEY, false);
			}

			if (EditorPrefs.HasKey(GameConstants.Editor.PREFS_ENABLE_STATE_MACHINE_DEBUG_KEY))
			{
				_gameStateMachine.LogsEnabled =
					EditorPrefs.GetBool(GameConstants.Editor.PREFS_ENABLE_STATE_MACHINE_DEBUG_KEY);
			}
#endif
		}

		private void Start()
		{
			FB.Init(FacebookInit);
			_notificationStateMachine.Run();
			_gameStateMachine.Run();
			TrySetLocalServer();

			StartCoroutine(HeartbeatCoroutine());
		}

		private void OnApplicationPause(bool isPaused)
		{
			if (isPaused)
			{
				_services.DataSaver.SaveAllData();
				_pauseCoroutine = StartCoroutine(EndAppCoroutine());
			}
			else if (_pauseCoroutine != null)
			{
				StopCoroutine(_pauseCoroutine);

				_pauseCoroutine = null;
			}

			_services?.MessageBrokerService.Publish(new ApplicationPausedMessage {IsPaused = isPaused});
		}

		private void OnApplicationQuit()
		{
			_services?.DataSaver?.SaveAllData();

			var quitReason = _services?.GameFlowService.QuitReason;
			
			_services?.AnalyticsService.SessionCalls.SessionEnd(quitReason);
		}

		private static void FacebookInit()
		{
			if (!FB.IsInitialized)
			{
				Debug.LogException(new UnityException("Facebook failed to initialized"));
				return;
			}

			FB.ActivateApp();
		}

		private void TrySetLocalServer()
		{
#if UNITY_EDITOR
			if (!EditorPrefs.HasKey(GameConstants.Editor.PREFS_USE_LOCAL_SERVER_KEY))
			{
				EditorPrefs.SetBool(GameConstants.Editor.PREFS_USE_LOCAL_SERVER_KEY, false);
			}

			if (EditorPrefs.GetBool(GameConstants.Editor.PREFS_USE_LOCAL_SERVER_KEY))
			{
				PlayFabSettings.LocalApiServer = "http://localhost:7274";
			}

			Debug.Log("Using local server? -" + EditorPrefs.GetBool(GameConstants.Editor.PREFS_USE_LOCAL_SERVER_KEY));
#endif
		}

		private IEnumerator EndAppCoroutine()
		{
			// The app is closed after 30 sec of being unused
			yield return new WaitForSeconds(30);

			_services?.GameFlowService.QuitGame("App closed after 30 sec of being unused");
		}

		private IEnumerator HeartbeatCoroutine()
		{
			while (true)
			{
				yield return new WaitForSeconds(30);
				_services?.AnalyticsService.SessionCalls.Heartbeat();
			}
		}
	}
}
