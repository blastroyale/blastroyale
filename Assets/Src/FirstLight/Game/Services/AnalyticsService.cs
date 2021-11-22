using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Firebase.Analytics;
using UnityEngine.Analytics;
using AppsFlyerSDK;
using Facebook.Unity;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// The analytics service is an endpoint in the game to log custom events to Game's analytics console
	/// </summary>
	public interface IAnalyticsService
	{
		/// <summary>
		/// Logs the first login Event with the given user <paramref name="id"/>
		/// </summary>
		void LoginEvent(string id);

		/// <summary>
		/// Logs an analytics event with the given <paramref name="eventName"/>
		/// </summary>
		void LogEvent(string eventName, Dictionary<string, object> parameters);
		
		/// <summary>
		/// Logs a crash with the given <paramref name="message"/>
		/// </summary>
		void CrashLog(string message);
		
		/// <summary>
		/// Logs a crash with the given <paramref name="exception"/>
		/// </summary>
		void CrashLog(Exception exception);

		/// <summary>
		/// Sends the mark of ending a game session
		/// </summary>
		void SessionEnd();
	}
	
	/// <inheritdoc />
	public class AnalyticsService : IAnalyticsService
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
			{"version", Application.version},
			{"platform", Application.platform.ToString()},
			{"device", SystemInfo.deviceModel},
			{"tablet", IsTablet},
#if UNITY_IOS
			{"ios_generation", UnityEngine.iOS.Device.generation.ToString()},
			{"ios_att_enabled", UnityEngine.iOS.Device.advertisingTrackingEnabled},
			//{"ios_low_power_enabled", UnityEngine.iOS.Device.lowPowerModeEnabled},
#else
			{"cpu", SystemInfo.processorType},
			//{"gpu", SystemInfo.graphicsDeviceName},
			//{"gpu_vendor", SystemInfo.graphicsDeviceVendor},
			{"gpu_api", SystemInfo.graphicsDeviceType.ToString()},
#endif
			{"language", Application.systemLanguage.ToString()},
			{"os", SystemInfo.operatingSystem},
			{"battery_status", SystemInfo.batteryStatus},
			//{"cpu_count", SystemInfo.processorCount},
			//{"memory", SystemInfo.systemMemorySize},
			{"memory_readable", SRFileUtil.GetBytesReadable((long) SystemInfo.systemMemorySize*1024*1024)},
			//{"max_textures_size", SystemInfo.maxTextureSize.ToString()},
		};

		/// <inheritdoc />
		public void LoginEvent(string id)
		{
			var loginData = LoginData;
			var loginEventName = "player_login";
			var appsFlyerData = loginData.ToDictionary(key => key.Key, value => value.ToString());
			
			Analytics.SetUserId(id);
			FirebaseAnalytics.SetUserId(id);
			AppsFlyer.setCustomerUserId(id);
			
			FB.LogAppEvent(loginEventName, null, loginData);
			AppsFlyer.sendEvent(loginEventName, appsFlyerData);
			LogEvent(loginEventName, loginData);
		}

		/// <inheritdoc />
		public void LogEvent(string eventName, Dictionary<string, object> parameters)
		{
			// max of 10 parameters
			Analytics.CustomEvent(eventName, parameters);

			if (PlayFabSettings.staticPlayer.IsClientLoggedIn())
			{
				var request = new WriteClientPlayerEventRequest { EventName = eventName, Body = parameters };
				PlayFabClientAPI.WritePlayerEvent(request, null, null);
			}

			if (parameters == null)
			{
				FirebaseAnalytics.LogEvent(eventName);
				return;
			}

			var firebaseParams = new List<Parameter>(parameters.Count);
			foreach (var parameter in parameters)
			{
				if (parameter.Value is long)
				{
					firebaseParams.Add(new Parameter(parameter.Key, (long) parameter.Value));
				}
				else if (parameter.Value is string)
				{
					firebaseParams.Add(new Parameter(parameter.Key, (string) parameter.Value));
				}
				else if (parameter.Value is double)
				{
					firebaseParams.Add(new Parameter(parameter.Key, (string) parameter.Value));
				}
			}
			
			FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
		}

		/// <inheritdoc />
		public void SessionEnd()
		{
			var dic = new Dictionary<string, object> {{"time", Time.realtimeSinceStartup}};
			
			if (PlayerPrefs.GetInt("first_session", 0) == 0)
			{
				LogEvent("first_session_end", dic);
				PlayerPrefs.SetInt("first_session", 1);
			}
			
			LogEvent("session_end", dic);
		}

		/// <inheritdoc />
		public void CrashLog(string message)
		{
			CrashLog(new UnityException(message));
		}

		/// <inheritdoc />
		public void CrashLog(Exception exception)
		{
			Debug.LogException(exception);
		}
	}
}