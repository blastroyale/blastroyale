using System;
using System.Collections.Generic;
using Firebase.Analytics;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using UnityEngine.Analytics;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	/// <summary>
	/// Analytics helper class regarding session events
	/// </summary>
	public class AnalyticsCallsSession : AnalyticsCalls
	{
		private IDataProvider _dataProvider;
		private IGameServices _services;
		private IGameDataProvider _gameData;
		
		/// <summary>
		/// Requests the information if the current device model playing the game is a tablet or 
		/// </summary>
		private static bool IsTablet
		{
			get
			{
#if UNITY_IOS
				return SystemInfo.deviceModel.Contains("iPad");
#elif UNITY_ANDROID
				var screenWidth = Screen.width;
				var screenHeight = Screen.height;
				var screenWidthDpi = screenWidth / Screen.dpi;
				var screenHeightDpi = screenHeight / Screen.dpi;
				var diagonalInchesSqrt =Mathf.Pow (screenWidthDpi, 2) + Mathf.Pow (screenHeightDpi, 2);
				var aspectRatio = Mathf.Max(screenWidth, screenHeight) / Mathf.Min(screenWidth, screenHeight);

				// This are physical size device checks with aspect ratio double confirmation
				return diagonalInchesSqrt > 42f && aspectRatio < 2f;
#else
				return false;
#endif
			}
		}

		public AnalyticsCallsSession(IAnalyticsService analyticsService, IGameServices services,
		                             IDataProvider dataProvider,
		                             IGameDataProvider gameDataProvider) : base(analyticsService)
		{
			_gameData = gameDataProvider;
			_dataProvider = dataProvider;
			_services = services;
		}

		/// <summary>
		/// Sends the mark of ending a game session
		/// </summary>
		public void SessionEnd(string reason)
		{
			var dic = new Dictionary<string, object> {{"reason", reason}};
			_analyticsService.LogEvent(AnalyticsEvents.SessionEnd, dic);
		}

		/// <summary>
		/// Sends a heartbeat analytics event
		/// </summary>
		public void Heartbeat()
		{
			_analyticsService.LogEvent(AnalyticsEvents.SessionHeartbeat);
		}

		/// <summary>
		/// Logs when we start doing the initial loading of the app
		/// </summary>
		public void GameLoadStart()
		{
			// Async call for the AdvertisingId
			var requestAdvertisingIdSuccess = !Application.RequestAdvertisingIdentifierAsync((id, enabled, msg) =>
			{
				var dic = new Dictionary<string, object>
				{
					{"client_version", VersionUtils.VersionInternal},
					{"advertising_id", id},
					{"advertising_tracking_enabled", enabled},
					{"vendor_id", SystemInfo.deviceUniqueIdentifier},
				};
				_analyticsService.LogEvent(AnalyticsEvents.GameLoadStart, dic);
			});
			
			// If the async call fails we try another way
			if (!requestAdvertisingIdSuccess)
			{
				var dic = new Dictionary<string, object>
				{
					{"client_version", VersionUtils.VersionInternal},
#if UNITY_ANDROID && !UNITY_EDITOR
					{"advertising_id", GetAndroidAdvertiserId()},
#endif
					{"vendor_id", SystemInfo.deviceUniqueIdentifier},
				};
				_analyticsService.LogEvent(AnalyticsEvents.GameLoadStart, dic);
			}
		}
		

		
		/// <summary>
		/// Logs the first login Event with the given user <paramref name="id"/>
		/// </summary>
		public void PlayerLogin(string id)
		{
			Analytics.SetUserId(id);
			FirebaseAnalytics.SetUserId(id);

			var loginData = new Dictionary<string, object> 		{
				{"client_version", VersionUtils.VersionInternal },
				{"platform", Application.platform.ToString()},
				{"device", SystemInfo.deviceModel},
				{"tablet", IsTablet},
#if UNITY_IOS
			{"ios_generation", UnityEngine.iOS.Device.generation.ToString()},
			{"ios_att_enabled", UnityEngine.iOS.Device.advertisingTrackingEnabled},
#else
				{"cpu", SystemInfo.processorType},
				{"gpu_api", SystemInfo.graphicsDeviceType.ToString()},
#endif
				{"language", Application.systemLanguage.ToString()},
				{"os", SystemInfo.operatingSystem},
				{"battery_status", SystemInfo.batteryStatus},
				{"memory_readable", SRFileUtil.GetBytesReadable((long) SystemInfo.systemMemorySize*1024*1024)},
			};
			
			// FB.LogAppEvent(AnalyticsEvents.PlayerLogin, null, loginData);
			
			_analyticsService.LogEvent(AnalyticsEvents.PlayerLogin, loginData);
		}

		/// <summary>
		/// Logs when we end doing the initial loading of the app
		/// </summary>
		public void GameLoaded()
		{
			var loadout = _gameData.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.Both);
			var inventory = _gameData.EquipmentDataProvider.GetInventoryEquipmentInfo(EquipmentFilter.NftOnly);

			var data = new Dictionary<string, object>
			{
				{"nfts_owned", inventory.Count},
				{"blst_token_balance", (int) _gameData.CurrencyDataProvider.GetCurrencyAmount(GameId.BLST)},
				{"cs_token_balance", (int) _gameData.CurrencyDataProvider.GetCurrencyAmount(GameId.CS)},
				{"total_power", loadout.GetTotalMight(_services.ConfigsProvider.GetConfigsDictionary<QuantumStatConfig>())}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.GameLoaded, data);
		}
		
		private static string GetAndroidAdvertiserId()
		{
			string advertisingID = "";
			try
			{
				AndroidJavaClass up = new AndroidJavaClass ("com.unity3d.player.UnityPlayer");
				AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject> ("currentActivity");
				AndroidJavaClass client = new AndroidJavaClass ("com.google.android.gms.ads.identifier.AdvertisingIdClient");
				AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject> ("getAdvertisingIdInfo", currentActivity);
     
				advertisingID = adInfo.Call<string> ("getId").ToString();
			}
			catch (Exception ex)
			{
				Debug.LogError("Error acquiring Android AdvertiserId - "+ex.Message);
			}
			return advertisingID;
		}
	}
}