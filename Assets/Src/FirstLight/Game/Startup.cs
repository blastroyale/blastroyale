using System;
using System.Reflection;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using FirstLight.FLogger;
using FirstLight.Game.Data;
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
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Advertisement.IosSupport;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem.EnhancedTouch;
using AnalyticsService = Unity.Services.Analytics.AnalyticsService;
using Debug = UnityEngine.Debug;
using GameObject = UnityEngine.GameObject;

namespace FirstLight.Game
{
	/// <summary>
	/// Contains the complete startup / init of our game
	/// </summary>
	public class Startup : MonoBehaviour
	{
		[SerializeField, Required] private AudioSource _audioSource;

		private void Start()
		{
			OhYeah();

			Screen.sleepTimeout = SleepTimeout.NeverSleep; // Should be somewhere else

			StartTask().Forget();
		}

		private async UniTask StartTask()
		{
			FLGTestRunner.Instance.CheckFirebaseRun();
			FLGTestRunner.Instance.CheckAutomations();

			InitTaskLogging();
			InitPlugins();
			InitGlobalShaderData();

			await RequestATTPermission();
			await InitUnityServices();
			await InitAnalytics();

			TouchSimulation.Enable();
			EnhancedTouchSupport.Enable();

			FLGCustomSerializers.RegisterAOT();
			FLGCustomSerializers.RegisterSerializers();
			FeatureFlags.ParseLocalFeatureFlags();

			await VersionUtils.LoadVersionDataAsync();
			var services = await InitFLGServices();
			await services.UIService.OpenScreen<LoadingScreenPresenter>();

			InitAppEventsListener();

			StartGameStateMachine();

			FLGTestRunner.Instance.OnGameAwaken();

			Destroy(gameObject);
		}

		private void InitAppEventsListener()
		{
			var go = new GameObject("AppEventsListener");
			go.AddComponent<AppEventsListener>();
			DontDestroyOnLoad(go);
		}

		private void InitTaskLogging()
		{
			TaskScheduler.UnobservedTaskException += (obj, e) =>
			{
				try
				{
					if (obj is Task task)
					{
						var convert = Convert(task);
						var objName = convert.Exception?.ToString();

						FLog.Error("Async task Exception happened " + objName);
					}
					else
					{
						FLog.Error("Try Exception Logging called");
					}
				}
				catch (Exception ex)
				{
					Debug.LogException(ex);
				}

				Debug.LogException(e.Exception);
				return;

				static async Task<object> Convert(Task task)
				{
					await task;
					var voidTaskType = typeof(Task<>).MakeGenericType(Type.GetType("System.Threading.Tasks.VoidTaskResult"));
					if (voidTaskType.IsInstanceOfType(task))
						throw new InvalidOperationException("Task does not have a return value (" + task.GetType() + ")");
					var property = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
					if (property == null)
						throw new InvalidOperationException("Task does not have a return value (" + task.GetType() + ")");
					return property.GetValue(task);
				}
			};
		}

		private void StartGameStateMachine()
		{
			MainInstaller.Resolve<IGameStateMachine>().Run();
		}

		private async UniTask<IGameServices> InitFLGServices()
		{
			var messageBroker = new InMemoryMessageBrokerService();
			var timeService = new TimeService();
			var dataService = new DataService();
			dataService.LoadData<AppData>();
			var assetResolver = new AssetResolverService();
			var audioFxService = new GameAudioFxService(assetResolver);
			var configsProvider = new ConfigsProvider();
			var networkService = new GameNetworkService(configsProvider);

			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider, audioFxService);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService, configsProvider, gameLogic, assetResolver,
				audioFxService);

			networkService.StartNetworking(gameLogic, gameServices);
			networkService.EnableQuantumPingCheck(true);

			MainInstaller.Bind<IWeb3Service>(new NoWeb3());
			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);
			MainInstaller.Bind<IGameStateMachine>(new GameStateMachine(gameLogic, gameServices, networkService, assetResolver));

			await StartupLoadingHelper.LoadConfigs(gameServices, assetResolver, configsProvider); // This should probably be done sooner

			return gameServices;
		}

#pragma warning disable CS1998
		private static async UniTask RequestATTPermission()
#pragma warning restore CS1998
		{
#if UNITY_IOS && !UNITY_EDITOR
			await UniTask.NextFrame(); // We wait for one frame because we had problems with the request in the past apparently
			ATTrackingStatusBinding.RequestAuthorizationTracking();

			await UniTask.WaitUntil(() =>
				ATTrackingStatusBinding.GetAuthorizationTrackingStatus() != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED);
#endif
		}

		private async UniTask InitUnityServices()
		{
			var initOpts = new InitializationOptions();

			initOpts.SetEnvironmentName(UnityCloudEnvironment.CURRENT);
			RemoteConfigService.Instance.SetEnvironmentID(UnityCloudEnvironment.CURRENT);

			await UnityServices.InitializeAsync(initOpts).AsUniTask();
			await Addressables.InitializeAsync().Task.AsUniTask();
		}

		private async UniTask InitAnalytics()
		{
			var trackingAllowed = ATTrackingStatusBinding.GetAuthorizationTrackingStatus() ==
				ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED;

			var dependencyStatus = FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();

			await dependencyStatus;

			if (dependencyStatus.Status != UniTaskStatus.Succeeded)
			{
				throw new InitializationException(InitResult.FailedMissingDependency,
					$"Firebase could not be initialized properly. Status: {dependencyStatus}");
			}

			FirebaseApp.Create();
			FirebaseAnalytics.SetAnalyticsCollectionEnabled(trackingAllowed);

			if (trackingAllowed)
			{
				AnalyticsService.Instance.StartDataCollection();
				SingularSDK.InitializeSingularSDK();
			}
		}

		private void OhYeah()
		{
			// Disgusting hack to play the sound only if it's enabled
			var json = PlayerPrefs.GetString(nameof(AppData), "");
			var isSoundEnabled = string.IsNullOrEmpty(json) || JsonConvert.DeserializeObject<AppData>(json).SfxEnabled;
			_audioSource.Play();
			if (isSoundEnabled)
			{
				//_audioSource.Play();
			}
		}

		private void InitPlugins()
		{
#if !DISABLE_SRDEBUGGER
			if (Debug.isDebugBuild)
			{
				SRDebug.Init();
			}
#endif

			FLog.Init();
		}

		private void InitGlobalShaderData()
		{
			// Used for DPI-based scaling in shaders
			Shader.SetGlobalVector(Shader.PropertyToID("_PhysicalScreenSize"),
				new Vector4(Screen.width / Screen.dpi, Screen.height / Screen.dpi, Screen.dpi, 69));
		}
	}
}