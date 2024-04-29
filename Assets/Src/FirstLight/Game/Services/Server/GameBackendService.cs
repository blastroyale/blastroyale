using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public enum Environment
	{
		DEV,
		STAGING,
		COMMUNITY,
		PROD
	}

	public class BackendEnvironmentData
	{
		public Environment EnvironmentID;

		/// <summary>
		/// Identifies the playfab title for this environment
		/// </summary>
		public string TitleID;

		/// <summary>
		/// Identifies the web3 id that will be used to identify it on the web3 layer
		/// </summary>
		public string Web3Id;

		/// <summary>
		/// Identify the photon application id that will be used by quantum
		/// </summary>
		public string AppIDRealtime;

		/// <summary>
		/// Playfab template ID that contains the password recovery html template.
		/// </summary>
		public string RecoveryEmailTemplateID;
	}

	/// <summary>
	/// This service handles general interaction with playfab that are not needed by the server
	/// </summary>
	public interface IGameBackendService
	{
		/// <summary>
		/// Sets up the backend with the correct cloud environment, per platform
		/// </summary>
		void SetupBackendEnvironment(Environment? forceEnvironment = null);

		/// <summary>
		/// Updates the user nickname in playfab.
		/// </summary>
		void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Calls the given cloudscript function with the given arguments.
		/// </summary>
		void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
						  Action<PlayFabError> onError, object parameter = null);

		/// <summary>
		/// Reads the specific title data by the given key.
		/// Throws an error if the key was not present.
		/// </summary>
		void GetTitleData(string key, Action<string> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Obtains the server state of the logged in player
		/// </summary>
		void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Compare server with client rewards to check if they match.
		/// Introduced as a workaround due to requiring two synchronous commands
		/// from two different services (Logic Service & Quantum Server)
		/// </summary>
		void CheckIfRewardsMatch(Action<bool> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Obtains all segments the player is in.
		/// Segments are group of players based on metrics which can be used for various things.
		/// </summary>
		void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Requests a data object with compiled info about the current backend environment
		/// </summary>
		BackendEnvironmentData CurrentEnvironmentData { get; }

		/// <summary>
		/// Requests to check if the game is currently in maintenance
		/// </summary>
		bool IsGameInMaintenance();

		/// <summary>
		/// Requests to check if the current game version is outdated
		/// </summary>
		bool IsGameOutdated();

		/// <summary>
		/// Requests the title version downloaded at authentication time
		/// </summary>
		string GetTitleVersion();

		/// <summary>
		/// Handles an incoming error. Sends outgoing messages, analytics and calls back
		/// </summary>
		void HandleError(PlayFabError error, Action<PlayFabError> callback, AnalyticsCallsErrors.ErrorType errorType);

		/// <summary>
		/// Handle an unrecoverable exception in the game, it will close and send analytics
		/// </summary>
		void HandleUnrecoverableException(Exception ex, AnalyticsCallsErrors.ErrorType errorType);

		/// <summary>
		/// Will handle a recoverable exception, making sure it will get to all analytics services
		/// </summary>
		void HandleRecoverableException(Exception ex, AnalyticsCallsErrors.ErrorType errorType = AnalyticsCallsErrors.ErrorType.Recoverable);

		/// <summary>
		/// Returns if the game is running on dev env. On dev things can be different.
		/// </summary>
		bool IsDev();

		/// <summary>
		/// Returns true for environments that run server-side simulation
		bool RunsSimulationOnServer();
	}

	/// <inheritdoc cref="IGameBackendService" />
	public interface IInternalGameBackendService : IGameBackendService
	{
		new BackendEnvironmentData CurrentEnvironmentData { get; set; }
	}

	/// <inheritdoc cref="IGameBackendService" />
	public class GameBackendService : IInternalGameBackendService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private readonly IGameDataProvider _dataProvider;
		private readonly IDataService _dataService;
		private readonly IGameServices _services;

		private readonly string _leaderboardLadderName;

		public BackendEnvironmentData CurrentEnvironmentData { get; set; }

		public GameBackendService(IMessageBrokerService msgBroker, IGameDataProvider dataProvider, IGameServices services, IDataService dataService, string leaderboardLadderName)
		{
			_messageBrokerService = msgBroker;
			_dataProvider = dataProvider;
			_dataService = dataService;
			_services = services;
			_leaderboardLadderName = leaderboardLadderName;
		}

		public void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetPlayerSegments(new GetPlayerSegmentsRequest(), r => { onSuccess(r.Segments); }, e => { HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session); });
		}

		private void SetupLive(BackendEnvironmentData envData)
		{
			envData.EnvironmentID = Environment.PROD;
			envData.TitleID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
		}

		private void SetupCommunity(BackendEnvironmentData envData)
		{
			envData.EnvironmentID = Environment.COMMUNITY;
			envData.TitleID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
		}

		private void SetupStaging(BackendEnvironmentData envData)
		{
			envData.EnvironmentID = Environment.STAGING;
			envData.TitleID = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
		}

		private void SetupDev(BackendEnvironmentData envData)
		{
			envData.EnvironmentID = Environment.DEV;
			envData.TitleID = "***REMOVED***";
			envData.RecoveryEmailTemplateID = "***REMOVED***";
			envData.AppIDRealtime = "***REMOVED***";
			envData.Web3Id = "***REMOVED***";
		}

		private void SetupEnvironmentFromLocalConfig(Environment env, BackendEnvironmentData envData)
		{
			switch (env)
			{
				case Environment.PROD:
					SetupLive(envData);
					break;
				case Environment.STAGING:
					SetupStaging(envData);
					break;
				case Environment.COMMUNITY:
					SetupCommunity(envData);
					break;
				default:
					SetupDev(envData);
					break;
			}
		}

		private void SetupEnvironmentFromCompilerFlags(BackendEnvironmentData envData)
		{
#if PROD_SERVER
			SetupLive(envData);
#elif COMMUNITY_SERVER
			SetupCommunity(envData);
#elif STAGE_SERVER
			SetupStaging(envData);
#else
			SetupEnvironmentFromLocalConfig(FeatureFlags.GetLocalConfiguration().EnvironmentOverride, envData);
#endif
		}

		public void SetupBackendEnvironment(Environment? forceEnvironment)
		{
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
			var appData = _dataService.GetData<AppData>();
			var envData = new BackendEnvironmentData();

			if (forceEnvironment.HasValue)
			{
				FLog.Info("Forcing Environment");
				SetupEnvironmentFromLocalConfig(forceEnvironment.Value, envData);
			}
			else
			{
				SetupEnvironmentFromCompilerFlags(envData);
			}

			FLog.Info($"Using environment: {envData.EnvironmentID.ToString()}");

			_messageBrokerService.Publish(new EnvironmentChanged() {NewEnvironment = envData.EnvironmentID});
			CurrentEnvironmentData = envData;
			PlayFabSettings.TitleId = CurrentEnvironmentData.TitleID;
			quantumSettings.AppSettings.AppIdRealtime = CurrentEnvironmentData.AppIDRealtime;

			if (CurrentEnvironmentData.EnvironmentID != appData.LastEnvironment)
			{
				FLog.Warn("Erasing APP data due to environment switch");

				_services.AuthenticationService.SetLinkedDevice(false);

				appData.LastEnvironment = CurrentEnvironmentData.EnvironmentID;

				_dataService.AddData(appData, true);
				_dataService.SaveData<AppData>();
			}
		}

		/// <inheritdoc />
		public void UpdateDisplayName(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess, Action<PlayFabError> onError)
		{
			var request = new UpdateUserTitleDisplayNameRequest {DisplayName = newNickname};

			void OnSuccessWrapper(UpdateUserTitleDisplayNameResult result)
			{
				_dataProvider.AppDataProvider.DisplayName.Value = result.DisplayName;
				onSuccess?.Invoke(result);
			}

			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnSuccessWrapper, onError);
		}

		public void CheckIfRewardsMatch(Action<bool> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest() {Keys = new List<string>() {typeof(PlayerData).FullName}}, result =>
			{
				var modelJson = result.Data[typeof(PlayerData).FullName].Value;
				var model = ModelSerializer.Deserialize<PlayerData>(modelJson);
				var serverState = model.UncollectedRewards;
				var clientState = _dataProvider.RewardDataProvider.UnclaimedRewards;
				var inSync = serverState.SequenceEqual(clientState);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
				if (!inSync)
				{
					FLog.Error("Client Rewards: " + ModelSerializer.Serialize(clientState));
					FLog.Error("Server Rewards: " + ModelSerializer.Serialize(serverState));
				}
#endif
				onSuccess(inSync);
			}, e => { HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session); });
		}

		/// <inheritdoc />
		/// <inheritdoc />
		public void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
								 Action<PlayFabError> onError, object parameter = null)
		{
			var request = new ExecuteFunctionRequest {FunctionName = functionName, GeneratePlayStreamEvent = true, FunctionParameter = parameter, AuthenticationContext = PlayFabSettings.staticPlayer};

			PlayFabCloudScriptAPI.ExecuteFunction(request, res =>
			{
				var exception = ExtractException(res);
				if (exception != null)
				{
					throw exception;
				}

				onSuccess(res);
			}, e => { HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session); });
		}

		/// <summary>
		/// Playfab cannot return error codes by default. So we require to wrap errors inside ok results.
		/// This function unpacks an exception packed with OK result so its visible on client
		/// </summary>
		private Exception ExtractException(ExecuteFunctionResult req)
		{
			var result = req.FunctionResult as JsonObject;
			if (result != null && result.TryGetValue("error", out var error) && error != null)
			{
				return new Exception(error.ToString());
			}

			return null;
		}

		public void HandleError(PlayFabError error, Action<PlayFabError> callback, AnalyticsCallsErrors.ErrorType errorType)
		{
			var descriptiveError = error.GenerateErrorReport();
			FLog.Error(descriptiveError);

			_services.AnalyticsService.ErrorsCalls.ReportError(errorType, error.ErrorMessage);

			_services.MessageBrokerService?.Publish(new ServerHttpErrorMessage() {ErrorCode = (HttpStatusCode) error.HttpCode, Message = descriptiveError});

			callback?.Invoke(error);
		}

		public void HandleRecoverableException(Exception ex, AnalyticsCallsErrors.ErrorType errorType = AnalyticsCallsErrors.ErrorType.Recoverable)
		{
			// Unfortunately we have to log as an Error to send to crash analytics, and it is impossible to send exceptions manually :( 
			FLog.Error("recoverable exception", ex);
			_services.AnalyticsService.ErrorsCalls.ReportError(errorType, ex.Message);
		}

		/// <inheritdoc/>
		public void HandleUnrecoverableException(Exception ex, AnalyticsCallsErrors.ErrorType errorType)
		{
			FLog.Error("unrecoverable exception", ex);
			var descriptiveError = $" {ex.Message}";
			_services.AnalyticsService.ErrorsCalls.ReportError(errorType, ex.Message);
#if UNITY_EDITOR
			FLog.Error(descriptiveError);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = "OK",
				ButtonOnClick = () =>
				{
					_services.AnalyticsService.CrashLog(descriptiveError);
					_services.QuitGame(descriptiveError);
				}
			};
			_services.GenericDialogService.OpenButtonDialog("Server Error", descriptiveError, false, confirmButton);
#else
		FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, "Error", descriptiveError, new FirstLight.NativeUi.AlertButton
			{
				Callback = () =>
				{
					_services.AnalyticsService.CrashLog(descriptiveError);
					_services.QuitGame(descriptiveError);
				},
					Style = FirstLight.NativeUi.AlertButtonStyle.Negative,
					Text = I2.Loc.ScriptLocalization.MainMenu.QuitGameButton
			});
#endif
		}

		public bool IsDev()
		{
			return CurrentEnvironmentData.EnvironmentID == Environment.DEV;
		}

		public bool RunsSimulationOnServer()
		{
#if UNITY_EDITOR
			var qtnConfig = MainInstaller.ResolveServices().ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			if (!qtnConfig.PhotonServerSettings.AppSettings.UseNameServer)
			{
				return true;
			}
#endif
			return FeatureFlags.QUANTUM_CUSTOM_SERVER;
		}

		public Environment? EnvironmentRedirect { get; set; } = null;

		public void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest(), result =>
			{
				onSuccess.Invoke(new ServerState(result.Data
					.ToDictionary(entry => entry.Key,
						entry =>
							entry.Value.Value)));
			}, e => { HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session); });
		}

		/// <summary>
		/// Gets an specific internal title key data
		/// </summary>
		public void GetTitleData(string key, Action<string> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetTitleData(new GetTitleDataRequest() {Keys = new List<string>() {key}}, res =>
			{
				if (!res.Data.TryGetValue(key, out var data))
				{
					data = null;
				}

				onSuccess.Invoke(data);
			}, e => { HandleError(e, onError, AnalyticsCallsErrors.ErrorType.Session); });
		}

		public bool IsGameInMaintenance()
		{
			var titleData = _dataService.GetData<AppData>().TitleData;

			return titleData.TryGetValue(GameConstants.PlayFab.MAINTENANCE_KEY, out var version) &&
				VersionUtils.IsOutdatedVersion(version);
		}

		public bool IsGameOutdated()
		{
			var titleVersion = GetTitleVersion();

			return VersionUtils.IsOutdatedVersion(titleVersion);
		}

		public string GetTitleVersion()
		{
			var titleData = _dataService.GetData<AppData>().TitleData;

			if (!titleData.TryGetValue(GameConstants.PlayFab.VERSION_KEY, out var titleVersion))
			{
				throw new Exception($"{GameConstants.PlayFab.VERSION_KEY} not set in title data");
			}

			return titleVersion;
		}

		private String UnityDeviceID()
		{
			var id = SystemInfo.deviceUniqueIdentifier;
#if UNITY_EDITOR
			if (ParrelSync.ClonesManager.IsClone())
			{
				id += "_clone_" + ParrelSync.ClonesManager.GetArgument();
			}
#endif
			return id;
		}
	}
}