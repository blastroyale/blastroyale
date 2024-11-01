using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using GameLogicService.Services;

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
		private readonly ILogger _log;
		private readonly IPlayerSetupService _setupService;
		private readonly IServerStateService _stateService;
		private readonly IBaseServiceConfiguration _serviceConfiguration;
		private readonly GameServer _server;
		private readonly IEventManager _eventManager;
		private readonly IStatisticsService _statistics;
		private readonly IUserMutex _userMutex;
		private readonly IMetricsService _metrics;
		private readonly IRemoteConfigService _remoteConfigService;

		public GameLogicWebWebService(IEventManager eventManager, ILogger log,
									  IPlayerSetupService service, IServerStateService stateService, GameServer server,
									  IBaseServiceConfiguration serviceConfiguration,
									  IUserMutex userMutex, IMetricsService metricsService,
									  IRemoteConfigService remoteConfigService)
		{
			_setupService = service;
			_stateService = stateService;
			_server = server;
			_serviceConfiguration = serviceConfiguration;
			_userMutex = userMutex;
			_eventManager = eventManager;
			_log = log;
			_metrics = metricsService;
			_remoteConfigService = remoteConfigService;
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
			await using (await _userMutex.LockUser(playerId))
			{
				try
				{
					var (state, serverConfig) =
						await _stateService.FetchStateAndConfigs(_remoteConfigService, playerId, 0);
					if (!_setupService.IsSetup(state))
					{
						_log.LogInformation($"Setting up player {playerId}");
						await SetupPlayer(playerId);
					}

					state = await _server.RunInitializationCommands(playerId, state, serverConfig);
					await _eventManager.CallEvent(new PlayerDataLoadEvent(playerId, state));
					if (state.HasDelta())
					{
						await _stateService.UpdatePlayerState(playerId, state.GetOnlyUpdatedState());
					}

					_metrics.EmitMetric("RouteGetPlayerData", 1);
					return Playfab.Result(playerId, new Dictionary<string, string>
					{
						{ "BuildNumber", _serviceConfiguration.BuildNumber },
						{ "BuildCommit", _serviceConfiguration.BuildCommit },
					});
				}
				catch (Exception e)
				{
					var errorResult = _server.GetErrorResult(null, e);
					_metrics.EmitException(e, "GetPlayerData");
					return GetPlayfabError(errorResult);
				}
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
								? new[] { errorResult.Error.StackTrace }
								: errorResult?.Data?.Values.ToArray()
						}
					}
				},
				Result = errorResult
			};
		}
	}
}