using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Analytics;
using FirstLight.Game.Utils;
using AppsFlyerSDK;
using FirstLight.Game.Data;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine.Analytics;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// The first entry object that shows the splash screen and boots the <see cref="Main"/> scene reference
	/// </summary>
	public class BootSplashscreenView : MonoBehaviour
	{
		private const string _bootSceneName = "Boot";
		private const string _mainSceneName = "Main";
		
		[SerializeField, Required] private AudioSource _audioSource;

		private void Awake()
		{
			// Hack to stop errors after build:
			// Unity: NullReferenceException: Object reference not set to an instance of an object.
			// 	at UnityEngine.Rendering.DebugManager.UpdateActions () [0x00000] in <00000000000000000000000000000000>:0
			// at UnityEngine.Rendering.DebugUpdater.Update () [0x00000] in <00000000000000000000000000000000>:0
			//
			// Know more and follow the Unity issue in https://issuetracker.unity3d.com/issues/isdebugbuild-returns-false-in-the-editor-when-its-value-is-checked-after-a-build
			// Remove once Unity solves it and we have a patched version
			DebugManager.instance.enableRuntimeUI = false;
			
			var appsFlyerReceiver = new GameObject(nameof(AppsFlyerReceiver)).AddComponent<AppsFlyerReceiver>();
			
			DontDestroyOnLoad(appsFlyerReceiver.gameObject);
			Analytics.CustomEvent("session_start");
			AppsFlyer.setIsDebug(Debug.isDebugBuild);
#if UNITY_ANDROID && !UNITY_EDITOR
			AppsFlyer.initSDK("GVzBhnWFUC2GjwKivnuH2H", null, appsFlyerReceiver);
#else
			AppsFlyer.initSDK("GVzBhnWFUC2GjwKivnuH2H", "1557220333", appsFlyerReceiver);
#endif
		}

		private async void Start()
		{
			var asyncOperation = SceneManager.LoadSceneAsync(_mainSceneName, LoadSceneMode.Additive);

			asyncOperation.allowSceneActivation = false;
			
			if (Debug.isDebugBuild)
			{
				SRDebug.Init();
			}
			
			await InitAtt();
			await StartAnalytics();
			StartSplashScreen();
			MergeScenes(asyncOperation);
		}
		private async Task InitAtt()
		{
#if UNITY_IOS
			if (Unity.Advertisement.IosSupport.ATTrackingStatusBinding.GetAuthorizationTrackingStatus() == 
			    Unity.Advertisement.IosSupport.ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
			{
				AppsFlyeriOS.waitForATTUserAuthorizationWithTimeoutInterval(60);
				Unity.Advertisement.IosSupport.ATTrackingStatusBinding.RequestAuthorizationTracking();

				await Task.Delay(500);
			}
#endif
		}

		private async Task StartAnalytics()
		{
			var dependencyStatus = FirebaseApp.CheckAndFixDependenciesAsync();
			
			AppsFlyer.startSDK();
			
			await dependencyStatus;
			
			if (dependencyStatus.Result != DependencyStatus.Available)
			{
				throw new InitializationException(InitResult.FailedMissingDependency,
				                                  $"Firebase could not be initialized properly. Status: {dependencyStatus}");
			}
			
			FirebaseApp.Create();
			FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
		}

		private void StartSplashScreen()
		{
			var json = PlayerPrefs.GetString(nameof(AppData), "");
			var isSoundEnabled = string.IsNullOrEmpty(json) || JsonConvert.DeserializeObject<AppData>(json).SfxEnabled;
			
			SplashScreen.Begin();
			SplashScreen.Draw();
			
			if (isSoundEnabled)
			{
				_audioSource.Play();
			}
		}

		private async void MergeScenes(AsyncOperation asyncOperation)
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

			SceneManager.MergeScenes(SceneManager.GetSceneByName(_bootSceneName), 
			                         SceneManager.GetSceneByName(_mainSceneName));
			Destroy(gameObject);
		}
	}
}