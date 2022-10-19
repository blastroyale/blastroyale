using System;
using System.Collections.Generic;
using System.Linq;
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
using FirstLight.Server.SDK.Services;

namespace Backend
{
	/// <summary>
	/// Represents the functionality of the game logic service. (AKA Game FunctionApp).
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
		private readonly GameServer _server;
		private readonly IEventManager _eventManager;
		private readonly IStateMigrator<ServerState> _migrator;

		public GameLogicWebWebService(
				IEventManager eventManager,
				ILogger log,
				IStateMigrator<ServerState> migrator,
				IPlayerSetupService service,
				IServerStateService stateService,
				GameServer server
				)
		{
			_setupService = service;
			_stateService = stateService;
			_server = server;
			_eventManager = eventManager;
			_migrator = migrator;
			_log = log;
		}

		public async Task<PlayFabResult<BackendLogicResult>> RunLogic(string playerId, LogicRequest request)
		{
			try
			{
				return new PlayFabResult<BackendLogicResult>
				{
					Result = await _server.RunLogic(playerId, request)
				};
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
				var state = await _stateService.GetPlayerState(playerId);
				if (!_setupService.IsSetup(state))
				{
					_log.LogInformation($"Setting up player {playerId}");
					await SetupPlayer(playerId);
				}
				else
				{
					var versionUpdates = _migrator.RunMigrations(state);
					if (versionUpdates > 0)
					{
						await _stateService.UpdatePlayerState(playerId, state);
						_log.LogDebug($"Bumped state for {playerId} by {versionUpdates} versions, ending in version {state.GetVersion()}");
					}
				}

				_eventManager.CallEvent(new PlayerDataLoadEvent(playerId));
				return new PlayFabResult<BackendLogicResult>
				{
					Result = new BackendLogicResult()
					{
						PlayFabId = playerId,
						Data = await _stateService.GetPlayerState(playerId)
					}
				};
			}
			catch (Exception e)
			{
				var errorResult = _server.GetErrorResult(null, e);
				return GetPlayfabError(errorResult);
			}
		}

		public async Task<PlayFabResult<BackendLogicResult>> RemovePlayerData(string playerId)
		{
			try
			{
				await _stateService.DeleteState(playerId);
				return new PlayFabResult<BackendLogicResult>
				{
					Result = new BackendLogicResult
					{
						PlayFabId = playerId
					}
				};
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
			return new PlayFabResult<BackendLogicResult>
			{
				Result = new BackendLogicResult
				{
					PlayFabId = playerId,
					Data = serverData
				}
			};
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
					ErrorMessage = errorResult.Error != null ? 
						               errorResult.Error.Message : 
						               errorResult?.Data?.Values.First(),
					ErrorDetails = new Dictionary<string, string[]>()
					{
						{ "Exception", errorResult.Error != null ? 
							               new[] {errorResult.Error.StackTrace} : 
							               errorResult?.Data?.Values.ToArray() 
						}
					}
				},
				Result = errorResult
			};
		}
	}
}

