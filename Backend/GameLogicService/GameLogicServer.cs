using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Backend.Game;
using Backend.Game.Services;
using Backend.Models;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using Newtonsoft.Json;

namespace Backend
{
	/// <summary>
	/// Represents the functionality of the game logic service. 
	/// Responsible for abstracting any networking layer needed to communicate with the server functionality.
	/// </summary>
	public interface ILogicWebService
	{
		/// <summary>
		/// Responsible for creating initial data models for the given player.
		/// </summary>
		public Task<PlayFabResult<BackendLogicResult>> SetupPlayer(string playerId);

		/// <summary>
		/// Runs server logic
		/// </summary>
		public Task<PlayFabResult<BackendLogicResult>> RunLogic(string player, LogicRequest logic);

		/// <summary>
		/// Obtains the current player state.
		/// </summary>
		public Task<PlayFabResult<BackendLogicResult>> GetPlayerData(string playerId);

		/// <summary>
		/// Removes the player from playfab.
		/// </summary>
		public Task<PlayFabResult<BackendLogicResult>> RemovePlayerData(string playerId);
	}

	public class GameLogicWebWebService : ILogicWebService
	{
		private const string PROJECT_ID = "***REMOVED***";
		private const string ENV_PROD = "***REMOVED***";
		private const string ENV_DEV = "***REMOVED***";
		private const string ENV_STAGING = "***REMOVED***";

		private readonly ILogger _log;
		private readonly IPlayerSetupService _setupService;
		private readonly IServerStateService _stateService;
		private readonly IBaseServiceConfiguration _serviceConfiguration;
		private readonly GameServer _server;
		private readonly IEventManager _eventManager;
		private readonly IStatisticsService _statistics;
		private readonly IServerMutex _mutex;
		internal HttpClient _client;

		private string _unityAccessToken = null;
		private DateTime _unityAccessTokenExpiration;

		public GameLogicWebWebService(IEventManager eventManager, ILogger log,
									  IPlayerSetupService service, IServerStateService stateService, GameServer server,
									  IBaseServiceConfiguration serviceConfiguration, IServerMutex mutex)
		{
			_setupService = service;
			_stateService = stateService;
			_server = server;
			_serviceConfiguration = serviceConfiguration;
			_mutex = mutex;
			_eventManager = eventManager;
			_log = log;

			_client = new HttpClient();
			_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
				"***REMOVED***");
			_client.DefaultRequestHeaders.Add("UnityEnvironment", "development");
		}

		public async Task<PlayFabResult<BackendLogicResult>> RunLogic(string playerId, LogicRequest request)
		{
			try
			{
				return
					Playfab.Result(playerId, await _server.RunLogic(playerId, request));
			}
			catch (Exception e)
			{
				return GetPlayfabError(_server.GetErrorResult(request, e));
			}
		}

		public async Task<PlayFabResult<BackendLogicResult>> GetPlayerData(string playerId)
		{
			try
			{
				await _mutex.Lock(playerId);

				if (string.IsNullOrEmpty(_unityAccessToken) || _unityAccessTokenExpiration < DateTime.Now)
				{
					var tokenExchangeResponse = await _client.PostAsync($"https://services.api.unity.com/auth/v1/token-exchange?projectId={PROJECT_ID}&environmentId={ENV_DEV}", null);
					if (tokenExchangeResponse.StatusCode != HttpStatusCode.Created)
					{
						throw new Exception("Unity Token Exchange API call failed.");
					}

					var tokenExchangeStr = await tokenExchangeResponse.Content.ReadAsStringAsync();

					_unityAccessToken = JsonConvert.DeserializeObject<TokenExchangeResponse>(tokenExchangeStr)!.accessToken;
					_unityAccessTokenExpiration = DateTime.Now.AddMinutes(45);
					_client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _unityAccessToken);
				}

				var customIdTask = _client.PostAsync($"https://player-auth.services.api.unity.com/v1/projects/{PROJECT_ID}/authentication/server/custom-id", JsonContent.Create(new {externalId = playerId, signInOnly = false}));

				var state = await _stateService.GetPlayerState(playerId);
				if (!_setupService.IsSetup(state))
				{
					_log.LogInformation($"Setting up player {playerId}");
					await SetupPlayer(playerId);
				}

				state = await _server.RunInitializationCommands(playerId, state);
				await _eventManager.CallEvent(new PlayerDataLoadEvent(playerId, state));
				if (state.HasDelta())
				{
					await _stateService.UpdatePlayerState(playerId, state.GetOnlyUpdatedState());
				}

				var customIdResponse = await customIdTask;
				if (customIdResponse.StatusCode != HttpStatusCode.OK)
				{
					throw new Exception("Unity Custom ID API call failed.");
				}

				var customID =
					JsonConvert.DeserializeObject<CustomIDResponse>(await customIdResponse.Content
						.ReadAsStringAsync())!;

				return Playfab.Result(playerId, new Dictionary<string, string>
				{
					{"BuildNumber", _serviceConfiguration.BuildNumber},
					{"BuildCommit", _serviceConfiguration.BuildCommit},
					{"idToken", customID.idToken},
					{"sessionToken", customID.sessionToken},
				});
			}
			catch (Exception e)
			{
				var errorResult = _server.GetErrorResult(null, e);
				return GetPlayfabError(errorResult);
			}
			finally
			{
				_mutex.Unlock(playerId);
			}
		}

		public async Task<PlayFabResult<BackendLogicResult>> RemovePlayerData(string playerId)
		{
			try
			{
				await _stateService.DeletePlayerState(playerId);
				return Playfab.Result(playerId);
			}
			catch (Exception e)
			{
				var errorResult = _server.GetErrorResult(null, e);
				return GetPlayfabError(errorResult);
			}
		}

		public async Task<PlayFabResult<BackendLogicResult>> SetupPlayer(string playerId)
		{
			var serverData = _setupService.GetInitialState(playerId);
			await _stateService.UpdatePlayerState(playerId, serverData);
			await _eventManager.CallEvent(new PlayerDataSetupEvent(playerId));
			return Playfab.Result(playerId, serverData);
		}

		/// <summary>
		/// Responsible for formatting logic errors to specific playfab errors.
		/// </summary>
		private PlayFabResult<BackendLogicResult> GetPlayfabError(BackendErrorResult errorResult)
		{
			return new PlayFabResult<BackendLogicResult>
			{
				Error = new PlayFabError()
				{
					HttpCode = 500,
					Error = PlayFabErrorCode.Unknown,
					ErrorMessage =
						errorResult.Error != null
							? errorResult.Error.Message
							: errorResult?.Data?.Values.First(),
					ErrorDetails = new Dictionary<string, string[]>()
					{
						{
							"Exception",
							errorResult.Error != null
								? new[] {errorResult.Error.StackTrace}
								: errorResult?.Data?.Values.ToArray()
						}
					}
				},
				Result = errorResult
			};
		}

		private class TokenExchangeResponse
		{
			public string accessToken { get; set; }
		}

		private class CustomIDResponse
		{
			public string idToken { get; set; }
			public string sessionToken { get; set; }
		}
	}
}