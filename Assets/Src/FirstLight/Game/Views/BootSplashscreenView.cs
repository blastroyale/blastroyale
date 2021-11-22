using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Firebase;
using Firebase.Analytics;
using FirstLight.Game.Utils;
using AppsFlyerSDK;
using Facebook.Unity;
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
		
		[SerializeField] private AudioSource _audioSource;

		private void Awake()
		{
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
			
			//TODO# @mr change to read google-services.json file 
#if UNITY_ANDROID
			if (!Debug.isDebugBuild)
			{
				var appOptions = AppOptions.LoadFromJsonConfig("{  \"project_info\": {     \"project_number\": \"705357047706\",     \"firebase_url\": \"https://phoenix-515d2-default-rtdb.europe-west1.firebasedatabase.app\",     \"project_id\": \"phoenix-515d2\",     \"storage_bucket\": \"phoenix-515d2.appspot.com\"   },   \"client\": [     {       \"client_info\": {         \"mobilesdk_app_id\": \"1:705357047706:android:e492ea23f28056e315bc23\",         \"android_client_info\": {           \"package_name\": \"com.firstlightgames.phoenix\"         }       },       \"oauth_client\": [         {           \"client_id\": \"705357047706-hmm2dotje81gklfjc0d182p307fs52g4.apps.googleusercontent.com\",           \"client_type\": 3         }       ],       \"api_key\": [         {           \"current_key\": \"***REMOVED***\"         }       ],       \"services\": {         \"appinvite_service\": {           \"other_platform_oauth_client\": [             {               \"client_id\": \"705357047706-hmm2dotje81gklfjc0d182p307fs52g4.apps.googleusercontent.com\",               \"client_type\": 3             },             {               \"client_id\": \"705357047706-45mojo26ucv4cnkvabpmcedmltn7cgkc.apps.googleusercontent.com\",               \"client_type\": 2,               \"ios_info\": {                 \"bundle_id\": \"com.firstlightgames.phoenix\"               }             }           ]         }       }     }   ],   \"configuration_version\": \"1\" }");
				FirebaseApp.Create(appOptions);
			}
			else		
#endif
			{
				FirebaseApp.Create();
			}
			
			FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
		}

		private void StartSplashScreen()
		{
			SplashScreen.Begin();
			SplashScreen.Draw();
			_audioSource.Play();
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