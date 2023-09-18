using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Analytics;
using FirstLight.Game.Utils;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Core;

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
			_ = StartTask();
		}

		private async Task StartTask()
		{
			var asyncOperation = SceneManager.LoadSceneAsync(_mainSceneName, LoadSceneMode.Additive);

			asyncOperation.allowSceneActivation = false;

			InitializePlugins();

			Shader.SetGlobalVector(Shader.PropertyToID("_PhysicalScreenSize"), new Vector4(Screen.width / Screen.dpi, Screen.height / Screen.dpi, Screen.dpi, 69));

			_permissions.RequestPermissions();
			await _permissions.PermissionResponseAwaitTask();

			Debug.Log("initializing with analytics enabled = " + _permissions.IsTrackingAccepted());
			
			await UnityServices.InitializeAsync();
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

		private void OnApplicationFocus(bool hasFocus)
		{
			if(hasFocus) _permissions.RequestPermissions();
		}

		private async Task WaitForInstaller()
		{
			while (!MainInstaller.TryResolve<IGameServices>(out var _))
			{
				await Task.Yield();
			}
		}

		private async Task StartAnalytics()
		{
			var dependencyStatus = FirebaseApp.CheckAndFixDependenciesAsync();

			await dependencyStatus;

			if (dependencyStatus.Result != DependencyStatus.Available)
			{
				throw new InitializationException(InitResult.FailedMissingDependency,
					$"Firebase could not be initialized properly. Status: {dependencyStatus}");
			}

			FirebaseApp.Create();
			FirebaseAnalytics.SetAnalyticsCollectionEnabled(_permissions.IsTrackingAccepted());
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

		private async Task MergeScenes(AsyncOperation asyncOperation)
		{
			while (!SplashScreen.isFinished || asyncOperation.progress < 0.9f || _audioSource.isPlaying)
			{
				await Task.Yield();
			}

			asyncOperation.allowSceneActivation = true;

			while (!asyncOperation.isDone)
			{
				await Task.Yield();
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