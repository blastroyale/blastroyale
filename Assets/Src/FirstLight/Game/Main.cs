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
			var analyticsService = new AnalyticsService();
			var timeService = new TimeService();
			var dataService = new DataService();
			var configsProvider = new ConfigsProvider();
			var uiService = new GameUiService(new UiAssetLoader());
			var networkService = new GameNetworkService();
			var assetResolver = new AssetResolverService();
			var genericDialogService = new GenericDialogService(uiService);
			var audioFxService = new GameAudioFxService(assetResolver);
			var vfxService = new VfxService<VfxId>();
			var gameLogic = new GameLogic(messageBroker, timeService, dataService, analyticsService, configsProvider, audioFxService);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService, configsProvider,
			                                    gameLogic, dataService, genericDialogService, assetResolver, analyticsService, 
			                                    vfxService, audioFxService);
			
			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);

			UiService = uiService;
			
			_gameLogic = gameLogic;
			_services = gameServices;
			_notificationStateMachine = new NotificationStateMachine(gameLogic, gameServices);
			
			_gameStateMachine = new GameStateMachine(gameLogic, gameServices, uiService, networkService, configsProvider, 
			                                         assetResolver, dataService, vfxService);
			_gameStateMachine.LogsEnabled = true;
		}

		private void Start()
		{
			FB.Init(FacebookInit);
			_notificationStateMachine.Run();
			_gameStateMachine.Run();
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
			
			_services.MessageBrokerService.Publish(new ApplicationPausedMessage{ IsPaused = isPaused });
		}
		
		private void OnApplicationQuit()
		{
			_services?.DataSaver?.SaveAllData();
			_services?.AnalyticsService.SessionEnd();
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

		private IEnumerator EndAppCoroutine()
		{
			// The app is closed after 30 sec of being unused
			yield return new WaitForSeconds(30);
			
			_services.AnalyticsService.SessionEnd();
			Application.Quit();
		}
	}
}
