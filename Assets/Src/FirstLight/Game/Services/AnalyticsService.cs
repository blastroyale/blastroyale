using System;
using System.Collections.Generic;
using Firebase.Analytics;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.Analytics;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Static class that defines all the event types names
	/// </summary>
	public static class AnalyticsEvents
	{
		public static readonly string SessionStart = "session_start";
		public static readonly string SessionEnd = "session_end";
		public static readonly string SessionHeartbeat = "session_heartbeat";
		public static readonly string GameLoadStart = "game_load_start";
		public static readonly string PlayerRegister = "player_register";
		public static readonly string PlayerLogin = "player_login";
		public static readonly string GameLoaded = "game_loaded";
		public static readonly string MatchInitiate = "match_initiate";
		public static readonly string MatchStart = "match_start";
		public static readonly string MatchEnd = "match_end";
		public static readonly string MatchKillAction = "match_kill_action";
		public static readonly string MatchPickupAction = "match_pickup_action";
		public static readonly string MatchChestOpenAction = "match_chest_open_action";
		public static readonly string MatchChestItemDrop = "match_chest_item_drop";
		public static readonly string Error = "error";
	}
	
	/// <summary>
	/// The analytics service is an endpoint in the game to log custom events to Game's analytics console
	/// </summary>
	public interface IAnalyticsService
	{
		public AnalyticsCallsSession SessionCalls { get; }
		public AnalyticsCallsMatch MatchCalls { get; }
		public AnalyticsCallsErrors ErrorsCalls { get; }

		/// <summary>
		/// Logs an analytics event with the given <paramref name="eventName"/>
		/// </summary>
		void LogEvent(string eventName, Dictionary<string, object> parameters = null);
		
		/// <summary>
		/// Logs a crash with the given <paramref name="message"/>
		/// </summary>
		void CrashLog(string message);
		
		/// <summary>
		/// Logs a crash with the given <paramref name="exception"/>
		/// </summary>
		void CrashLog(Exception exception);
	}
	
	/// <inheritdoc />
	public class AnalyticsService : IAnalyticsService
	{
		public AnalyticsCallsSession SessionCalls { get; private set; }
		public AnalyticsCallsMatch MatchCalls { get; private set; }
		public AnalyticsCallsErrors ErrorsCalls { get; private set; }

		public AnalyticsService(IGameServices services,
		                        IGameDataProvider gameDataProvider,
		                        IDataProvider dataProvider)
		{
			SessionCalls = new AnalyticsCallsSession(this, services, dataProvider, gameDataProvider);
			MatchCalls = new AnalyticsCallsMatch(this, services, gameDataProvider);
			ErrorsCalls = new AnalyticsCallsErrors(this);
		}

		/// <inheritdoc />
		public void LogEvent(string eventName, Dictionary<string, object> parameters)
		{
			//Debug.Log("Analytics event "+eventName+": "+JsonConvert.SerializeObject(parameters));
   
			//PlayFab Analytics
			if (PlayFabSettings.staticPlayer.IsClientLoggedIn())
			{
				var request = new WriteClientPlayerEventRequest { EventName = eventName, Body = parameters };
				PlayFabClientAPI.WritePlayerEvent(request, null, null);
			}
   
			if (parameters == null)
			{
				// Firebase
				FirebaseAnalytics.LogEvent(eventName);
				// Unity
				Analytics.CustomEvent(eventName);
				return;
			}
   
			// Prepare parameters for Unity and Firebase
			var unityParams = new Dictionary<string, object>();
			var firebaseParams = new List<Parameter>(parameters.Count);
			int count = 0;
			foreach(var parameter in parameters)
			{
				if (parameter.Value == null)
				{
					Debug.LogWarning("Analytics null parameter '"+parameter.Key+"' in event '"+eventName+"'");
					continue;
				}
				
				// Unity (max 10 params)
				if (count++ < 10)
				{
					unityParams[parameter.Key] = parameter.Value;
				}

				switch (parameter.Value)
				{
					// Firebase
					case long or uint or int or byte:
						firebaseParams.Add(new Parameter(parameter.Key, Convert.ToInt64(parameter.Value)));
						break;
					case double or float:
						firebaseParams.Add(new Parameter(parameter.Key, Convert.ToDouble(parameter.Value)));
						break;
					default:
						firebaseParams.Add(new Parameter(parameter.Key, parameter.Value.ToString()));
						break;
				}
			}
			// Unity
			Analytics.CustomEvent(eventName, unityParams);
			// Firebase
			FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
		}

		/// <inheritdoc />
		public void CrashLog(string message)
		{
			CrashLog(new UnityException(message));
		}

		/// <inheritdoc />
		public void CrashLog(Exception exception)
		{
			ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Session, "CrashLog:"+exception.Message);
			Debug.LogException(exception);
		}
	}
}