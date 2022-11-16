using System;
using System.Collections;
using System.Threading.Tasks;
using FirstLight.FLogger;
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
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException += TaskExceptionLogging;
			Screen.sleepTimeout = SleepTimeout.NeverSleep;
			
			FLog.Init();
		}

		private void OnDestroy()
		{
			System.Threading.Tasks.TaskScheduler.UnobservedTaskException -= TaskExceptionLogging;
		}


		private void Start()
		{
			var messageBroker = new InMemoryMessageBrokerService();
			var timeService = new TimeService();
			var dataService = new DataService();
			var configsProvider = new ConfigsProvider();
			var uiService = new GameUiService(new UiAssetLoader());
			var networkService = new GameNetworkService(configsProvider);
			var assetResolver = new AssetResolverService();
			var genericDialogService = new GenericDialogService(uiService);
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();

			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider, audioFxService);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService,
				configsProvider, gameLogic, genericDialogService,
				assetResolver, vfxService, audioFxService);

			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);

			UiService = uiService;
			_gameLogic = gameLogic;
			_services = gameServices;
			_notificationStateMachine = new NotificationStateMachine(gameLogic, gameServices);
			_gameStateMachine = new GameStateMachine(gameLogic, gameServices, uiService, networkService,
				configsProvider,
				assetResolver, dataService, vfxService);

			FLog.Verbose($"Initialized client version {VersionUtils.VersionExternal}");


			_notificationStateMachine.Run();
			_gameStateMachine.Run();
			TrySetLocalServer();

			StartCoroutine(HeartbeatCoroutine());
		}

		private void OnApplicationPause(bool isPaused)
		{
			if (isPaused)
			{
				_services?.DataSaver?.SaveAllData();
				
				_pauseCoroutine = StartCoroutine(EndAppCoroutine());
			}
			else if (_pauseCoroutine != null)
			{
				StopCoroutine(_pauseCoroutine);

				_pauseCoroutine = null;
			}

			_services?.MessageBrokerService?.Publish(new ApplicationPausedMessage {IsPaused = isPaused});
		}

		private void OnApplicationQuit()
		{
			_services?.DataSaver?.SaveAllData();
			_services?.AnalyticsService?.SessionCalls?.SessionEnd(_services?.QuitReason);
		}

		private void TrySetLocalServer()
		{
#if UNITY_EDITOR
			FeatureFlags.ParseLocalFeatureFlags();
			Debug.Log("Using local server? -" + FeatureFlags.GetLocalConfiguration().UseLocalServer);
#endif
		}

		private IEnumerator EndAppCoroutine()
		{
			// The app is closed after 30 sec of being unused
			yield return new WaitForSeconds(30);

			_services?.QuitGame("App closed after 30 sec of being unused");
		}

		private IEnumerator HeartbeatCoroutine()
		{
			var waitForSeconds = new WaitForSeconds(30);
			
			while (true)
			{
				yield return waitForSeconds;
				_services?.AnalyticsService.SessionCalls.Heartbeat();
			}
		}

		private void TaskExceptionLogging(object sender, UnobservedTaskExceptionEventArgs e)
		{
			if (sender.GetType().GetGenericTypeDefinition() == typeof(Task<>))
			{
				var task = sender as Task<object>;
				var objName = task.Result is UnityEngine.Object ? ((UnityEngine.Object)task.Result).name : task.Result.ToString();
				
				Debug.LogError($"Task exception sent by the object {objName}");
			}
			
			Debug.LogException(e.Exception);
		}
	}
}