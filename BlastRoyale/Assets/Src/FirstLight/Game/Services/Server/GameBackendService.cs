using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Services;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.CloudScriptModels;
using PlayFab.Json;
using Unity.Services.Authentication;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// This service handles general interaction with playfab that are not needed by the server
	/// </summary>
	public interface IGameBackendService
	{
		/// <summary>
		/// Sets up the backend with the correct cloud environment, per platform
		/// </summary>
		void SetupBackendEnvironment();

		/// <summary>
		/// Updates the user nickname in playfab.
		/// </summary>
		void UpdateDisplayNamePlayfab(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Execute a function using the generic endcpoint, to see current values look at <see cref="FirstLight.Server.SDK.Modules.Commands"/>
		/// </summary>
		public void CallGenericFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError,
										Dictionary<string, string> data = null);

		/// <summary>
		/// Same as <see cref="CallGenericFunction"/>
		/// </summary>
		public UniTask<ExecuteFunctionResult> CallGenericFunction(string functionName, Dictionary<string, string> data = null);

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
		UniTask<bool> CheckIfRewardsMatch();

		/// <summary>
		/// Obtains all segments the player is in.
		/// Segments are group of players based on metrics which can be used for various things.
		/// </summary>
		void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError);

		/// <summary>
		/// Requests to check if the game is currently in maintenance
		/// </summary>
		bool IsGameInMaintenance(out string message);

		/// <summary>
		/// Requests to check if the current game version is outdated
		/// </summary>
		bool IsGameOutdated(out string message);

		/// <summary>
		/// Requests the title version downloaded at authentication time
		/// </summary>
		string GetTitleVersion();

		/// <summary>
		/// Handles an incoming error. Sends outgoing messages, analytics and calls back
		/// </summary>
		void HandleError(PlayFabError error, Action<PlayFabError> callback);

		/// <summary>
		/// Handle an unrecoverable exception in the game, it will close and send analytics
		/// </summary>
		void HandleUnrecoverableException(Exception ex);

		/// <summary>
		/// Will handle a recoverable exception, making sure it will get to all analytics services
		/// </summary>
		void HandleRecoverableException(Exception ex);

		/// <summary>
		/// Returns if the game is running on dev env. On dev things can be different.
		/// </summary>
		bool IsDev();

		/// <summary>
		/// Returns true for environments that run server-side simulation
		/// </summary>
		bool RunsSimulationOnServer();

		/// <summary>
		/// If the environment was forced by a redirect
		/// </summary>
		public bool ForcedEnvironment { get; }

		/// <summary>
		/// Return true if the game is outdated or in maintanance
		/// </summary>
		public bool IsGameInMaintenanceOrOutdated(bool openPopups);

		/// <see cref="UnityRemoteConfigProvider.UpdateConfigWhenExpired"/>
		public UniTask<bool> UpdateConfigs(params Type[] types);
	}

	/// <inheritdoc cref="IGameBackendService" />
	public interface IInternalGameBackendService : IGameBackendService
	{
	}

	/// <inheritdoc cref="IGameBackendService" />
	public class GameBackendService : IInternalGameBackendService
	{
		private readonly IMessageBrokerService _messageBrokerService;
		private readonly IGameDataProvider _dataProvider;
		private readonly IDataService _dataService;
		private readonly IGameServices _services;

		public bool ForcedEnvironment { get; private set; }

		private readonly string _leaderboardLadderName;

		public GameBackendService(IMessageBrokerService msgBroker, IGameDataProvider dataProvider, IGameServices services, IDataService dataService,
								  string leaderboardLadderName)
		{
			_messageBrokerService = msgBroker;
			_dataProvider = dataProvider;
			_dataService = dataService;
			_services = services;
			_leaderboardLadderName = leaderboardLadderName;
		}

		public void GetPlayerSegments(Action<List<GetSegmentResult>> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetPlayerSegments(new GetPlayerSegmentsRequest(), r => { onSuccess(r.Segments); },
				e => { HandleError(e, onError); });
		}

		public void SetupBackendEnvironment()
		{
			var quantumSettings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings;
			var appData = _dataService.GetData<AppData>();

			FLog.Info($"Using environment: {FLEnvironment.Current.UCSEnvironmentName}");

			PlayFabSettings.TitleId = FLEnvironment.Current.PlayFabTitleID;
			quantumSettings.AppSettings.AppIdRealtime = FLEnvironment.Current.PhotonAppIDRealtime;

			if (FLEnvironment.Current.Name != appData.LastEnvironmentName)
			{
				FLog.Warn("Erasing APP data due to environment switch");

				// We only unlink if there was a previous environment set (check is here for migration purposes)
				if (!string.IsNullOrEmpty(appData.LastEnvironmentName))
				{
					_services.AuthenticationService.SetLinkedDevice(false);
				}

				AuthenticationService.Instance.SignOut(true);
				appData.LastEnvironmentName = FLEnvironment.Current.Name;

				_dataService.AddData(appData, true);
				_dataService.SaveData<AppData>();
			}
		}

		/// <inheritdoc />
		public void UpdateDisplayNamePlayfab(string newNickname, Action<UpdateUserTitleDisplayNameResult> onSuccess, Action<PlayFabError> onError)
		{
			var request = new UpdateUserTitleDisplayNameRequest {DisplayName = newNickname};

			void OnSuccessWrapper(UpdateUserTitleDisplayNameResult result)
			{
				onSuccess?.Invoke(result);
			}

			PlayFabClientAPI.UpdateUserTitleDisplayName(request, OnSuccessWrapper, onError);
		}

		public async UniTask<bool> CheckIfRewardsMatch()
		{
			var req = new GetUserDataRequest() {Keys = new List<string>() {typeof(PlayerData).FullName}};
			var result = await AsyncPlayfabAPI.ClientAPI.GetUserReadOnlyData(req);

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
			return inSync;
		}
		
		public UniTask<ExecuteFunctionResult> CallGenericFunction(string functionName, Dictionary<string, string> data = null)
		{
			return CallFunctionAsync("Generic", new LogicRequest()
			{
				Command = functionName,
				Platform = Application.platform.ToString(),
				Data = data
			});
		}

		public void CallGenericFunction(string functionName, Action<ExecuteFunctionResult> onSuccess, Action<PlayFabError> onError,
										Dictionary<string, string> data = null)
		{
			CallFunction("Generic", onSuccess, onError, new LogicRequest()
			{
				Command = functionName,
				Platform = Application.platform.ToString(),
				Data = data
			});
		}

		private async UniTask<ExecuteFunctionResult> CallFunctionAsync(string function, object param = null)
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = function, GeneratePlayStreamEvent = true, FunctionParameter = param,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};
			return await AsyncPlayfabAPI.ExecuteFunction(request);
		}

		/// <inheritdoc />
		/// <inheritdoc />
		private void CallFunction(string functionName, Action<ExecuteFunctionResult> onSuccess,
								  Action<PlayFabError> onError, object parameter = null)
		{
			var request = new ExecuteFunctionRequest
			{
				FunctionName = functionName, GeneratePlayStreamEvent = true, FunctionParameter = parameter,
				AuthenticationContext = PlayFabSettings.staticPlayer
			};

			PlayFabCloudScriptAPI.ExecuteFunction(request, res =>
			{
				var exception = ExtractException(res);
				if (exception != null)
				{
					throw exception;
				}

				onSuccess(res);
			}, e => { HandleError(e, onError); });
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

		public void HandleError(PlayFabError error, Action<PlayFabError> callback)
		{
			var descriptiveError = error.GenerateErrorReport();
			FLog.Error(descriptiveError);

			_services.MessageBrokerService?.Publish(new ServerHttpErrorMessage()
				{ErrorCode = (HttpStatusCode) error.HttpCode, Message = descriptiveError});

			callback?.Invoke(error);
		}

		public void HandleRecoverableException(Exception ex)
		{
			// Unfortunately we have to log as an Error to send to crash analytics, and it is impossible to send exceptions manually :( 
			FLog.Error("recoverable exception", ex);
		}

		/// <inheritdoc/>
		public void HandleUnrecoverableException(Exception ex)
		{
			FLog.Error("unrecoverable exception", ex);
			var descriptiveError = $" {ex.Message}";
#if UNITY_EDITOR
			FLog.Error(descriptiveError);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = "OK",
				ButtonOnClick = () =>
				{
					_services.QuitGame(descriptiveError);
				}
			};
			_services.GenericDialogService.OpenButtonDialog("Server Error", descriptiveError, false, confirmButton);
#else
		FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, "Error", descriptiveError, new FirstLight.NativeUi.AlertButton
			{
				Callback = () =>
				{
					_services.QuitGame(descriptiveError);
				},
					Style = FirstLight.NativeUi.AlertButtonStyle.Negative,
					Text = I2.Loc.ScriptLocalization.MainMenu.QuitGameButton
			});
#endif
		}

		public bool IsDev()
		{
			return FLEnvironment.Current.Name == FLEnvironment.DEVELOPMENT.Name;
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

		public void FetchServerState(Action<ServerState> onSuccess, Action<PlayFabError> onError)
		{
			PlayFabClientAPI.GetUserReadOnlyData(new GetUserDataRequest(), result =>
			{
				onSuccess.Invoke(new ServerState(result.Data
					.ToDictionary(entry => entry.Key,
						entry =>
							entry.Value.Value)));
			}, e => { HandleError(e, onError); });
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
			}, e => { HandleError(e, onError); });
		}

		public bool IsGameInMaintenance(out string message)
		{
			var cfg = _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>();

			if (cfg.Maintenance)
			{
				message = cfg.MaintenanceMessage;
				return true;
			}

			message = null;
			return false;
		}

		public bool IsGameOutdated(out string message)
		{
			var cfg = _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>();
			message = cfg.VersionBlockMessage;
			return VersionUtils.IsOutdatedVersion(cfg.AllowedVersion);
		}

		public string GetTitleVersion()
		{
			return _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>().AllowedVersion;
		}

		public UniTask<bool> UpdateConfigs(params Type[] types)
		{
			var serv = ((IGameRemoteConfigProvider) _dataProvider.RemoteConfigProvider);
			var secondsTtl = serv.GetConfig<GeneralConfig>().ConfigCacheInSeconds;
			return serv.UpdateConfigWhenExpired(TimeSpan.FromSeconds(secondsTtl), types);
		}

		public bool IsGameInMaintenanceOrOutdated(bool openPopups)
		{
			string msg;
			if (IsGameInMaintenance(out msg))
			{
				OpenMaintenanceDialog(msg);
				return true;
			}

			if (IsGameOutdated(out msg))
			{
				OpenGameUpdateDialog(msg);
				return true;
			}

			return false;
		}

		private void OpenGameUpdateDialog(string msg)
		{
			var message = string.Format(msg, VersionUtils.VersionExternal,
				_services.GameBackendService.GetTitleVersion());

			NativeUiService.ShowAlertPopUp(ScriptLocalization.General.NewGameUpdate, message, ScriptLocalization.General.Confirm, OpenStore);

			void OpenStore()
			{
#if UNITY_IOS
				Application.OpenURL(GameConstants.Links.APP_STORE_IOS);
#elif UNITY_ANDROID
				Application.OpenURL(GameConstants.Links.APP_STORE_GOOGLE_PLAY);
#endif
				_services.QuitGame("Closing game blocked dialog");
			}
		}

		private void OpenMaintenanceDialog(string msg)
		{
			var cfg = _dataProvider.RemoteConfigProvider.GetConfig<GameMaintenanceConfig>();

			NativeUiService.ShowAlertPopUp(ScriptLocalization.General.Maintenance, msg, cfg.MaintenanceButtonText, () =>
			{
				Application.OpenURL(cfg.MaintenanceButtonLink);
				_services.QuitGame("Closing game blocked dialog");
				OpenMaintenanceDialog(msg);
			});
		}
	}
}