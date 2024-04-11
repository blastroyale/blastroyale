using System;
using System.Collections.Generic;
using Firebase.Analytics;
using FirstLight.FLogger;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.TestCases;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
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
				var diagonalInchesSqrt = Mathf.Pow(screenWidthDpi, 2) + Mathf.Pow(screenHeightDpi, 2);
				var aspectRatio = Mathf.Max(screenWidth, screenHeight) / Mathf.Min(screenWidth, screenHeight);

				// This are physical size device checks with aspect ratio double confirmation
				return diagonalInchesSqrt > 42f && aspectRatio < 2f;
#else
				return false;
#endif
			}
		}

		public AnalyticsCallsSession(IAnalyticsService analyticsService, IGameServices services,
									 IGameDataProvider gameDataProvider) : base(analyticsService)
		{
			_gameData = gameDataProvider;
			_services = services;
		}

		/// <summary>
		/// Sends the mark of ending a game session
		/// </summary>
		public void SessionEnd(string reason)
		{
			var dic = new Dictionary<string, object> { { "reason", reason } };
			_analyticsService.LogEvent(AnalyticsEvents.SessionEnd, dic, ignoreForUnity: true);
		}

		/// <summary>
		/// Sends a heartbeat analytics event
		/// </summary>
		public void Heartbeat()
		{
			Dictionary<string, object> parameters = null;

			_analyticsService.LogEvent(AnalyticsEvents.SessionHeartbeat, parameters, false, true);
		}

		public void Disconnection(bool critical)
		{
			var dic = new Dictionary<string, object>
			{
				{ "client_version", VersionUtils.VersionInternal },
				{ "device_internet", Application.internetReachability.ToString() },
				{ "client_state", _services.NetworkService?.QuantumClient?.State.ToString() },
				{ "critical", critical },
				{ "peer_state", _services.NetworkService?.QuantumClient?.LoadBalancingPeer?.PeerState.ToString() },
				{ "connected_server", _services.NetworkService?.QuantumClient?.Server.ToString() }
			};
			_analyticsService.LogEvent(AnalyticsEvents.PlayerDisconnect, dic, false);
		}


		/// <summary>
		/// Logs when we start doing the initial loading of the app
		/// </summary>
		public void GameLoadStart()
		{
			var testName = FLGTestRunner.Instance.GetRunningTestName();
			FLog.Info("#424242 Game launched with test " + testName);
			// Async call for the AdvertisingId
			var requestAdvertisingIdSuccess = Application.RequestAdvertisingIdentifierAsync((id, enabled, msg) =>
			{
				var dic = new Dictionary<string, object>
				{
					{ "client_version", VersionUtils.VersionInternal },
					{ "advertising_id", id },
					{ "boot_time", Time.realtimeSinceStartup },
					{ "advertising_tracking_enabled", enabled },
					{ "vendor_id", SystemInfo.deviceUniqueIdentifier },
					{ "session_id", AnalyticsSessionInfo.sessionId }
				};

				if (testName != null)
				{
					dic["test_name"] = testName;
				}

				_analyticsService.LogEvent(AnalyticsEvents.GameLoadStart, dic);
			});

			// If the async call fails we try another way
			if (!requestAdvertisingIdSuccess)
			{
				var dic = new Dictionary<string, object>
				{
					{ "client_version", VersionUtils.VersionInternal },
#if UNITY_ANDROID && !UNITY_EDITOR
					{"advertising_id", GetAndroidAdvertiserId()},
#endif
					{ "vendor_id", SystemInfo.deviceUniqueIdentifier },
					{ "session_id", AnalyticsSessionInfo.sessionId },
				};
				if (testName != null)
				{
					dic["test_name"] = testName;
				}

				_analyticsService.LogEvent(AnalyticsEvents.GameLoadStart, dic);
			}
		}


		/// <summary>
		/// Logs the first login Event with the given user <paramref name="id"/>
		/// </summary>
		public void PlayerLogin(string id, bool isGuest)
		{
			FirebaseAnalytics.SetUserId(id);
			SingularSDK.SetCustomUserId(id);
			UnityEngine.CrashReportHandler.CrashReportHandler.SetUserMetadata("playfab_id", id);

			var loginData = new Dictionary<string, object>
			{
				{ "is_guest", isGuest },
				{ "client_version", VersionUtils.VersionInternal },
				{ "platform", Application.platform.ToString() },
				{ "device", SystemInfo.deviceModel },
				{ "tablet", IsTablet },
				{ "session_id", AnalyticsSessionInfo.sessionId },
#if UNITY_IOS
				{"ios_generation", UnityEngine.iOS.Device.generation.ToString()},
				{"ios_att_enabled", UnityEngine.iOS.Device.advertisingTrackingEnabled},
#else
				{ "cpu", SystemInfo.processorType },
				{ "gpu_api", SystemInfo.graphicsDeviceType.ToString() },
#endif
				{ "language", Application.systemLanguage.ToString() },
				{ "os", SystemInfo.operatingSystem },
				{ "battery_status", SystemInfo.batteryStatus.ToString() },
				{ "memory_readable", SRFileUtil.GetBytesReadable((long)SystemInfo.systemMemorySize * 1024 * 1024) },
			};

			_analyticsService.LogEvent(AnalyticsEvents.PlayerLogin, loginData);
		}

		/// <summary>
		/// Logs when we end doing the initial loading of the app
		/// </summary>
		public void GameLoaded()
		{
			var inventoryCount = _gameData.EquipmentDataProvider.GetInventoryEquipmentCount(EquipmentFilter.NftOnly);

			var data = new Dictionary<string, object>
			{
				{ "nfts_owned", inventoryCount },
				{ "blst_token_balance", (int)_gameData.CurrencyDataProvider.GetCurrencyAmount(GameId.BLST) },
			};

			_analyticsService.LogEvent(AnalyticsEvents.GameLoaded, data);
		}

#if UNITY_ANDROID
		private static string GetAndroidAdvertiserId()
		{
			string advertisingID = "";
			try
			{
				AndroidJavaClass up = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject currentActivity = up.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaClass client = new AndroidJavaClass("com.google.android.gms.ads.identifier.AdvertisingIdClient");
				AndroidJavaObject adInfo = client.CallStatic<AndroidJavaObject>("getAdvertisingIdInfo", currentActivity);

				advertisingID = adInfo.Call<string>("getId").ToString();
			}
			catch (Exception ex)
			{
				Debug.LogError("Error acquiring Android AdvertiserId - " + ex.Message);
			}

			return advertisingID;
		}
#endif
	}
}