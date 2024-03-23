using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Analytics;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using AnalyticsService = Unity.Services.Analytics.AnalyticsService;

#pragma warning disable CS1998

namespace FirstLight.Game.Views
{
	/// <summary>
	/// The first entry object that shows the splash screen and boots the <see cref="Main"/> scene reference
	/// </summary>
	public class BootSplashscreenView : MonoBehaviour
	{
		private const string _bootSceneName = "Boot";
		private const string _mainSceneName = "Main";
		private AppPermissions _permissions = new ();

		[SerializeField, Required] private AudioSource _audioSource;

		private void Awake()
		{
			// Hack to stop errors after build:
			// Unity: NullReferenceException: Object reference not set to an instance of an object.
			//     at UnityEngine.Rendering.DebugManager.UpdateActions () [0x00000] in <00000000000000000000000000000000>:0
			// at UnityEngine.Rendering.DebugUpdater.Update () [0x00000] in <00000000000000000000000000000000>:0
			//
			// Know more and follow the Unity issue in https://issuetracker.unity3d.com/issues/isdebugbuild-returns-false-in-the-editor-when-its-value-is-checked-after-a-build
			// Remove once Unity solves it and we have a patched version
			DebugManager.instance.enableRuntimeUI = false;
		}

		private void Start()
		{
			StartTask().Forget();
		}

		/// <summary>
		/// Because apple is garbage, if we request permissions on boot it does not work
		/// Requesitn on focus is also not guaranteed
		/// So this is to try to make it appear at least. This is ultra hacky but life is about
		/// what u have on the moment i guess
		/// </summary>
		private async UniTask PermissionRequestHack()
		{
			await UniTask.Delay(500);
			if (!_permissions.IsPermissionsAnswered()) _permissions.RequestPermissions();
			await UniTask.Delay(1500);
			if (!_permissions.IsPermissionsAnswered()) _permissions.RequestPermissions();
		}

		private async UniTask StartTask()
		{
			var asyncOperation = SceneManager.LoadSceneAsync(_mainSceneName, LoadSceneMode.Additive);

			asyncOperation.allowSceneActivation = false;

			InitializePlugins();

			Shader.SetGlobalVector(Shader.PropertyToID("_PhysicalScreenSize"),
				new Vector4(Screen.width / Screen.dpi, Screen.height / Screen.dpi, Screen.dpi, 69));

			_ = PermissionRequestHack();
			await _permissions.PermissionResponseAwaitTask();

			Debug.Log("initializing with analytics enabled = " + _permissions.IsTrackingAccepted());

			await InitUnityServices();

			await StartAnalytics();
			if (_permissions.IsTrackingAccepted())
			{
#if !UNITY_EDITOR
					SingularSDK.InitializeSingularSDK();
#endif
			}

			StartSplashScreen();
			await MergeScenes(asyncOperation);
		}

		private async UniTask InitUnityServices()
		{
			var initOpts = new InitializationOptions();

			initOpts.SetEnvironmentName(UnityCloudEnvironment.CURRENT);

			await UnityServices.InitializeAsync(initOpts).AsUniTask();
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (hasFocus) _permissions.RequestPermissions();
		}

		private async UniTask WaitForInstaller()
		{
			while (!MainInstaller.TryResolve<IGameServices>(out _))
			{
				await UniTask.Yield();
			}
		}

		private async UniTask StartAnalytics()
		{
			var dependencyStatus = FirebaseApp.CheckAndFixDependenciesAsync().AsUniTask();

			await dependencyStatus;

			if (dependencyStatus.Status != UniTaskStatus.Succeeded)
			{
				throw new InitializationException(InitResult.FailedMissingDependency,
					$"Firebase could not be initialized properly. Status: {dependencyStatus}");
			}

			FirebaseApp.Create();
			FirebaseAnalytics.SetAnalyticsCollectionEnabled(_permissions.IsTrackingAccepted());

			if (_permissions.IsTrackingAccepted())
			{
				AnalyticsService.Instance.StartDataCollection();
			}
		}

		private void StartSplashScreen()
		{
			var json = PlayerPrefs.GetString(nameof(AppData), "");
			var isSoundEnabled = string.IsNullOrEmpty(json) || JsonConvert.DeserializeObject<AppData>(json).SfxEnabled;

#if !UNITY_EDITOR_LINUX
			SplashScreen.Begin();
			SplashScreen.Draw();
#endif

			if (isSoundEnabled)
			{
				_audioSource.Play();
			}
		}

		private async UniTask MergeScenes(AsyncOperation asyncOperation)
		{
			while (!SplashScreen.isFinished || asyncOperation.progress < 0.9f || _audioSource.isPlaying)
			{
				await UniTask.Yield();
			}

			asyncOperation.allowSceneActivation = true;

			while (!asyncOperation.isDone)
			{
				await UniTask.Yield();
			}

			await WaitForInstaller();

			SceneManager.MergeScenes(SceneManager.GetSceneByName(_bootSceneName),
				SceneManager.GetSceneByName(_mainSceneName));
			Destroy(gameObject);
		}

		private void InitializePlugins()
		{
#if !DISABLE_SRDEBUGGER
			if (Debug.isDebugBuild)
			{
				SRDebug.Init();
			}
#endif
		}
	}
}
