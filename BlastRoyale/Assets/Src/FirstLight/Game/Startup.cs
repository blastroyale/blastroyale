using System;
using System.Collections.Generic;
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
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.PushNotifications;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
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
			StartTask().Forget();
		}

		private async UniTask StartTask()
		{
			InitTaskLogging();
			InitPlugins();

			InitGlobalShaderData();

			await ATTrackingUtils.RequestATTPermission();
			await InitUnityServices();
			await InitAnalytics();

			FLGTestRunner.Instance.CheckAutomations();
			FLGTestRunner.Instance.CheckFirebaseRun();

			TouchSimulation.Enable();
			EnhancedTouchSupport.Enable();

			FLGCustomSerializers.RegisterAOT();
			FLGCustomSerializers.RegisterSerializers();
			FeatureFlags.ParseLocalFeatureFlags();

			await VersionUtils.LoadVersionDataAsync();
			// This uglyness is here because we need to show the loading screen before loading configs, which need this tuple
			var (services, assetResolver, configsProvider) = InitFLGServices();
			OhYeah();

			await StartupLoadingHelper.LoadConfigs(services, assetResolver, configsProvider);

			InitSettings();
			InitAppEventsListener();
			await InitPushNotifications();

			StartGameStateMachine();

			FLGTestRunner.Instance.AfterGameAwaken();

			Destroy(gameObject);
		}

		private static void InitAppEventsListener()
		{
			var go = new GameObject("AppEventsListener");
			go.AddComponent<AppEventsListener>();
			DontDestroyOnLoad(go);
		}

		private static async UniTask InitPushNotifications()
		{
			if (Application.isEditor) return;

			PushNotificationsService.Instance.OnRemoteNotificationReceived += PushNotificationReceived;

			try
			{
				var token = await PushNotificationsService.Instance.RegisterForPushNotificationsAsync().AsUniTask();
				FLog.Info($"Registered for push notifications with token: {token}");
			}
			catch (Exception e)
			{
				FLog.Warn("Failed to register for push notifications: ", e);
			}

			return;

			// Only for testing for now
			void PushNotificationReceived(Dictionary<string, object> notificationData)
			{
				FLog.Info("Notification received!");
				foreach (var (key, value) in notificationData)
				{
					FLog.Info($"Notification data item: {key} - {value}");
				}
			}
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

		private static void StartGameStateMachine()
		{
			MainInstaller.Resolve<IGameStateMachine>().Run();
		}

		// TODO: This should not return the tuple, but we need UI Service before we await config loading
		private static (IGameServices, IAssetAdderService, IConfigsAdder) InitFLGServices()
		{
			var messageBroker = new InMemoryMessageBrokerService();
			var timeService = new TimeService();
			var dataService = new DataService();
			dataService.LoadData<AppData>();
			var assetResolver = new AssetResolverService();
			var configsProvider = new ConfigsProvider();
			var networkService = new GameNetworkService(configsProvider);

			var gameLogic = new GameLogic(messageBroker, timeService, dataService, configsProvider);
			var gameServices = new GameServices(networkService, messageBroker, timeService, dataService, configsProvider, gameLogic, assetResolver);

			networkService.StartNetworking(gameLogic, gameServices);
			networkService.EnableQuantumPingCheck(true);

			MainInstaller.Bind<IWeb3Service>(new NoWeb3());
			MainInstaller.Bind<IGameDataProvider>(gameLogic);
			MainInstaller.Bind<IGameServices>(gameServices);
			MainInstaller.Bind<IGameStateMachine>(new GameStateMachine(gameLogic, gameServices, networkService, assetResolver));

			return (gameServices, assetResolver, configsProvider);
		}

		private static async UniTask InitUnityServices()
		{
			var initOpts = new InitializationOptions();

			initOpts.SetEnvironmentName(FLEnvironment.Current.UCSEnvironmentName);
			RemoteConfigService.Instance.SetEnvironmentID(FLEnvironment.Current.UCSEnvironmentID);

			await UnityServices.InitializeAsync(initOpts).AsUniTask();
			await Addressables.InitializeAsync().Task.AsUniTask();
			
			
#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				AuthenticationService.Instance.SwitchProfile("_clone_" + ParrelSync.ClonesManager.GetArgument());
			}
#endif
		}

		private static async UniTask InitAnalytics()
		{
			var trackingAllowed = ATTrackingUtils.IsTrackingAllowed();

			var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();

			if (dependencyStatus == DependencyStatus.Available)
			{
				FirebaseApp.Create();
				FirebaseAnalytics.SetAnalyticsCollectionEnabled(trackingAllowed);
			}
			else
			{
				FLog.Warn($"Firebase could not be initialized properly. Status: {dependencyStatus}");
			}

			if (trackingAllowed)
			{
				AnalyticsService.Instance.StartDataCollection();
				SingularSDK.InitializeSingularSDK();
			}
		}

		private void OhYeah()
		{
			if (MainInstaller.ResolveServices().LocalPrefsService.IsSFXEnabled)
			{
				_audioSource.Play();
			}
		}

		private static void InitPlugins()
		{
#if !DISABLE_SRDEBUGGER
			SRDebug.Init();
#endif
			Debug.developerConsoleEnabled = false;
			DebugManager.instance.enableRuntimeUI = false;
			FLog.Init();
		}

		private static void InitGlobalShaderData()
		{
			// Used for DPI-based scaling in shaders
			Shader.SetGlobalVector(Shader.PropertyToID("_PhysicalScreenSize"),
				new Vector4(Screen.width / Screen.dpi, Screen.height / Screen.dpi, Screen.dpi, 69));
		}

		private static void InitSettings()
		{
			Screen.sleepTimeout = SleepTimeout.NeverSleep;

			// Probably not the best place for this
			MainInstaller.ResolveServices().LocalPrefsService.IsFPSLimitEnabled.InvokeObserve((_, limitEnabled) =>
			{
				Application.targetFrameRate = limitEnabled ? 30 : 60;
			});
		}
	}
}