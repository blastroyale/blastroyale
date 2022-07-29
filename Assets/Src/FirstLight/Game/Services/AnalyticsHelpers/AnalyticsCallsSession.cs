using System.Collections.Generic;
using System.Linq;
using AppsFlyerSDK;
using Facebook.Unity;
using Firebase.Analytics;
using FirstLight.Game.Data;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using UnityEngine.Analytics;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public class AnalyticsCallsSession : AnalyticsCalls
	{
		/// <summary>
		/// Requests the information if the current device model playing the game is a tablet or 
		/// </summary>
		public static bool IsTablet
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
		
		/// <summary>
		/// Requests the player login data
		/// </summary>
		public static Dictionary<string, object> LoginData => new Dictionary<string, object>
		{
			{"client_version", VersionUtils.VersionExternal },
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
		
		public AnalyticsCallsSession(IAnalyticsService analyticsService) : base(analyticsService)
		{
		}

		/// <summary>
		/// Sends the mark of ending a game session
		/// </summary>
		public void SessionEnd(string reason)
		{
			var dic = new Dictionary<string, object> {{"reason", reason}};
			_analyticsService.LogEvent(AnalyticsEvents.SessionEnd, dic);
		}

		public void Heartbeat()
		{
			_analyticsService.LogEvent(AnalyticsEvents.SessionHeartbeat);
		}

		public void GameLoadStart()
		{
			var dic = new Dictionary<string, object> {{"client_version", Application.version}};
			_analyticsService.LogEvent(AnalyticsEvents.GameLoadStart, dic);
		}
		
		/// <summary>
		/// Logs the first login Event with the given user <paramref name="id"/>
		/// </summary>
		public void PlayerLogin(string id)
		{
			Analytics.SetUserId(id);
			FirebaseAnalytics.SetUserId(id);
			AppsFlyer.setCustomerUserId(id);
			
			var loginData = LoginData;
			
			var appsFlyerData = loginData.ToDictionary(key => key.Key, value => value.ToString());
			AppsFlyer.sendEvent(AnalyticsEvents.PlayerLogin, appsFlyerData);
			
			FB.LogAppEvent(AnalyticsEvents.PlayerLogin, null, loginData);
			
			_analyticsService.LogEvent(AnalyticsEvents.PlayerLogin, loginData);
		}

		public void GameLoaded()
		{
			var gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			var loadout = gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo();
			var dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			var dataService = MainInstaller.Resolve<IGameServices>().DataProvider;

			var data = new Dictionary<string, object>
			{
				{"nfts_owned", dataService.GetData<NftEquipmentData>().Inventory.Keys.Count},
				{"blst_token_balance", (int) dataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.BLST)},
				{"cs_token_balance", (int) dataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.CS)},
				{"total_power", loadout.GetTotalStat(EquipmentStatType.Damage)}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.GameLoaded, data);
		}
	}
}