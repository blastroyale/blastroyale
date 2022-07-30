using System;
using System.Collections.Generic;
using Firebase.Analytics;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.AnalyticsHelpers;
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
		public static readonly string MatchLootboxOpenAction = "match_lootbox_open_action";
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
		                        IGameDataProvider gameDataProvider)
		{
			SessionCalls = new AnalyticsCallsSession(this, services, gameDataProvider);
			MatchCalls = new AnalyticsCallsMatch(this, services, gameDataProvider);
			ErrorsCalls = new AnalyticsCallsErrors(this);
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
			
			Debug.Log("Analytics event "+eventName+": "+JsonConvert.SerializeObject(parameters));
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