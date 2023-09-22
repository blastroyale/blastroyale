using System;
using System.Collections.Generic;
using Firebase.Analytics;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Services;
using FirstLight.UiService;
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
		public static readonly string ScreenView = "screen_view";
		public static readonly string ButtonAction = "button_action";
		public static readonly string MatchInitiate = "match_initiate";
		public static readonly string MatchStart = "match_start";
		public static readonly string MatchEnd = "match_end";
		public static readonly string MatchEndBattleRoyalePlayerDead = "match_end_br_player_dead";
		public static readonly string MatchKillAction = "match_kill";
		public static readonly string MatchDeadAction = "match_death";
		public static readonly string MatchPickupAction = "match_pickup_action";
		public static readonly string MatchChestOpenAction = "match_chest_open_action";
		public static readonly string MatchChestItemDrop = "match_chest_item_drop";
		public static readonly string Error = "error_log";
		public static readonly string Purchase = "purchase";
		public static readonly string ItemEquipAction = "item_equip_action";
		public static readonly string InitialLoadingComplete = "initial_loading_complete";
		public static readonly string LoadCoreAssetsComplete = "load_core_assets_complete";
		public static readonly string LoadMatchAssetsComplete = "load_match_assets_complete";
		public static readonly string TutorialStepCompleted = "tutorial_step_completed";
	}
	
	/// <summary>
	/// The analytics service is an endpoint in the game to log custom events to Game's analytics console
	/// </summary>
	public interface IAnalyticsService
	{
		/// <inheritdoc cref="AnalyticsCallsSession"/>
		public AnalyticsCallsSession SessionCalls { get; }
		
		/// <inheritdoc cref="AnalyticsCallsMatch"/>
		public AnalyticsCallsMatch MatchCalls { get; }
		
		/// <inheritdoc cref="AnalyticsCallsEconomy"/>
		public AnalyticsCallsEconomy EconomyCalls { get; }
		
		/// <inheritdoc cref="AnalyticsCallsErrors"/>
		public AnalyticsCallsErrors ErrorsCalls { get; }
		
		/// <inheritdoc cref="AnalyticsCallsUi"/>
		public AnalyticsCallsUi UiCalls { get; }
		
		/// <inheritdoc cref="AnalyticsCallsEquipment"/>
		public AnalyticsCallsEquipment EquipmentCalls { get; }
		
		/// <inheritdoc cref="AnalyticsCallsTutorial"/>
		public AnalyticsCallsTutorial TutorialCalls { get; }

		/// <summary>
		/// Logs an analytics event with the given <paramref name="eventName"/>.
		/// <paramref name="isCriticalEvent"/> represents data that are critical to send to any data end point no matter what
		/// </summary>
		void LogEvent(string eventName, Dictionary<string, object> parameters = null, bool isCriticalEvent = true);
		
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
		public AnalyticsCallsSession SessionCalls { get; }
		public AnalyticsCallsMatch MatchCalls { get; }
		public AnalyticsCallsEconomy EconomyCalls { get; }
		public AnalyticsCallsTutorial TutorialCalls { get; }
		public AnalyticsCallsErrors ErrorsCalls { get; }
		public AnalyticsCallsUi UiCalls { get; }
		public AnalyticsCallsEquipment EquipmentCalls { get; }

		public AnalyticsService(IGameServices services,
		                        IGameDataProvider gameDataProvider,
								IUiService uiService)
		{
			SessionCalls = new AnalyticsCallsSession(this, services, gameDataProvider);
			MatchCalls = new AnalyticsCallsMatch(this, services, gameDataProvider);
			EconomyCalls = new AnalyticsCallsEconomy(this);
			EquipmentCalls = new AnalyticsCallsEquipment(this, services);
			TutorialCalls = new AnalyticsCallsTutorial(this);
			ErrorsCalls = new AnalyticsCallsErrors(this);
			UiCalls = new AnalyticsCallsUi(this, uiService);
			EquipmentCalls = new AnalyticsCallsEquipment(this, services);
		}


		/// <inheritdoc />
		public void LogEvent(string eventName, Dictionary<string, object> parameters = null, bool isCriticalEvent = true)
		{
			if (!AppPermissions.Get().IsTrackingAccepted()) return;
			
			try
			{
				//PlayFab Analytics
				if (PlayFabSettings.staticPlayer.IsClientLoggedIn())
				{
					var request = new WriteClientPlayerEventRequest {EventName = eventName, Body = parameters};
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

				if (isCriticalEvent)
				{
					SingularSDK.Event(parameters, eventName);
				}

				// Prepare parameters for Firebase
				var firebaseParams = new List<Parameter>(parameters.Count);
				foreach (var parameter in parameters)
				{
					if (parameter.Value == null)
					{
						Debug.LogWarning(
							"Analytics null parameter '" + parameter.Key + "' in event '" + eventName + "'");
						continue;
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

				// Firebase
				FirebaseAnalytics.LogEvent(eventName, firebaseParams.ToArray());
			}
			catch (Exception e)
			{
				FLog.Error("Error while sending analytics: " + e.Message);
				Debug.LogException(e);
			}
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
