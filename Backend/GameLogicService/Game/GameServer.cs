using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Utils;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using FirstLight.Game.Data;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using GameLogicService.Game;

namespace Backend.Game
{
	/// <summary>
	/// Class represents game-server instance. Holds logic methods to run gamelogic.
	/// This should be networking agnostic.
	/// </summary>
	public class GameServer
	{
		private IServerCommahdHandler _cmdHandler;
		private ServerEnvironmentService _environmentService;
		private ILogger _log;
		private IServerStateService _state;
		private IServerMutex _mutex;
		private IEventManager _eventManager;
		private IMetricsService _metrics;
		private IBaseServiceConfiguration _baseServiceConfig;
		private IConfigsProvider _gameConfigs;

		public GameServer(IConfigsProvider gameConfigs, IBaseServiceConfiguration baseServiceConfig, IServerCommahdHandler cmdHandler, ILogger log, IServerStateService state, IServerMutex mutex, IEventManager eventManager, IMetricsService metrics, ServerEnvironmentService environmentService)
		{
			_cmdHandler = cmdHandler;
			_log = log;
			_state = state;
			_mutex = mutex;
			_eventManager = eventManager;
			_metrics = metrics;
			_environmentService = environmentService;
			_baseServiceConfig = baseServiceConfig;
			_gameConfigs = gameConfigs;
		}


		/// <summary>
		/// Runs a logic request in server state for the given player. This will persist the updated state at the end.
		/// </summary>
		public async Task<BackendLogicResult> RunLogic(string playerId, LogicRequest logicRequest)
		{
			var requestData = logicRequest.Data;
			var savedDataAmount = 0;
			try
			{
				if (!requestData.TryGetValue(CommandFields.CommandType, out var cmdType))
				{
					throw new LogicException($"Input dict requires field key for cmd: {CommandFields.CommandData}");
				}

				if (!requestData.TryGetValue(CommandFields.CommandData, out var commandData))
				{
					throw new LogicException($"Input dict requires field key for cmd: {CommandFields.CommandData}");
				}

				_log.LogInformation($"{playerId} running {cmdType}");
				await _mutex.Lock(playerId);
				var commandInstance = _cmdHandler.BuildCommandInstance(commandData, cmdType);
				var currentPlayerState = await _state.GetPlayerState(playerId);
				ValidateCommand(currentPlayerState, commandInstance, requestData);

				var newState = await RunCommands(playerId, new[] { commandInstance }, currentPlayerState, commandData);

				if (newState.HasDelta())
				{
					var onlyUpdatedState = newState.GetOnlyUpdatedState();
					await _state.UpdatePlayerState(playerId, onlyUpdatedState);
					savedDataAmount = onlyUpdatedState.Count;
				}

				var response = new Dictionary<string, string>();
				if (requestData.TryGetValue(CommandFields.ConfigurationVersion, out var clientConfigVersion))
				{
					var clientConfigVersionNumber = ulong.Parse(clientConfigVersion);
					if (_gameConfigs.Version > clientConfigVersionNumber)
					{
						response[CommandFields.ConfigurationVersion] = _gameConfigs.Version.ToString();
					}
				}

				ModelSerializer.SerializeToData(response, newState.GetDeltas());
				return new BackendLogicResult() { Command = cmdType, Data = response, PlayFabId = playerId };
			}
			catch (Exception ex)
			{
				_log.LogError(ex, "Error on run logic");
				throw;
			}
			finally
			{
				_mutex.Unlock(playerId);
				_metrics.EmitEvent("GameCommand", new Dictionary<string, string>()
				{
					{ "command", logicRequest.Command },
					{ "playerId", playerId },
					{ "savedDeltas", savedDataAmount.ToString() },
				});
			}
		}

		/// <summary>
		/// Run all initialization commands and SAVES the player state if it has modifications
		/// </summary>
		public Task<ServerState> RunInitializationCommands(string playerId, ServerState state)
		{
			var cmds = _cmdHandler.GetInitializationCommands();
			_log.LogInformation($"{playerId} running initialization commands: {string.Join(", ", cmds.Select(a => a.GetType().Name))}");
			return RunCommands(playerId, cmds, state, "{}");
		}

		private async Task<ServerState> RunCommands(string playerId, IGameCommand[] commandInstances, ServerState currentPlayerState, string commandData)
		{
			var currentState = currentPlayerState;
			var deltas = new StateDelta();
			currentState.GetDeltas().Merge(deltas);
			foreach (var commandInstance in commandInstances)
			{
				var newState = await _cmdHandler.ExecuteCommand(playerId, commandInstance, currentState);
				await _eventManager.CallCommandEvent(playerId, commandInstance, newState);
				currentState = newState;
				deltas.Merge(currentState.GetDeltas());
			}

			var hasDeltas = deltas.GetModifiedTypes().Any();
			if (!hasDeltas) return currentState;

			currentState.SetDelta(deltas);
			return currentState;
		}

		/// <summary>
		/// Validates if a given command with a given input can be ran on a given player state.
		/// Will raise exceptions in case its not feasible to run the command.
		/// </summary>
		public bool ValidateCommand(ServerState state, IGameCommand cmd, Dictionary<string, string> cmdData)
		{
			if (!HasAccess(state, cmd, cmdData))
			{
				throw new LogicException("Insuficient permissions to run command");
			}

			if (cmd.ExecutionMode() == CommandExecutionMode.Initialization)
			{
				throw new LogicException("Command can only be triggerred from server!");
			}

			if (cmd.ExecutionMode() == CommandExecutionMode.Quantum)
			{
				return true;
			}

			if (!cmdData.TryGetValue(CommandFields.Timestamp, out var currentCommandTimeString))
			{
				throw new LogicException($"Command data requires a timestamp to be ran: Key {CommandFields.Timestamp}");
			}

			if (!cmdData.TryGetValue(CommandFields.ClientVersion, out var clientVersionString))
			{
				throw new LogicException($"Command data requires a version to be ran: Key {CommandFields.ClientVersion}");
			}

			var minVersion = _baseServiceConfig.MinClientVersion;
			var clientVersion = new Version(clientVersionString);
			if (clientVersion < minVersion)
			{
				throw new LogicException($"Outdated client {clientVersion} but expected minimal version {minVersion}");
			}

			state.TryGetValue(CommandFields.Timestamp, out var lastCommandTime);
			Int64.TryParse(lastCommandTime, out var lastCmdTimestamp);
			Int64.TryParse(currentCommandTimeString, out var currentCmdTimestamp);
			if (currentCmdTimestamp <= lastCmdTimestamp)
			{
				throw new LogicException($"Outdated command timestamp for command {cmd.GetType().Name}. Command out of order ?");
			}

			return true;
		}

		/// <summary>
		/// Returns a logic result to contain information about the logic exception that was
		/// thrown to be acknowledge by the game client.
		/// </summary>
		public BackendErrorResult GetErrorResult(LogicRequest request, Exception exp)
		{
			_log.LogError(exp, $"Unhandled Server Error for {request?.Command}");
			_metrics.EmitException(exp, $"{exp.Message} at {exp.StackTrace} on {request?.Command}");
			return new BackendErrorResult() { Error = exp, Command = request?.Command, Data = new Dictionary<string, string>() { { "LogicException", exp.Message } } };
		}

		/// <summary>
		/// Checks if a given player has enough permissions to run a given command.
		/// </summary>s
		private bool HasAccess(ServerState playerState, IGameCommand cmd, Dictionary<string, string> cmdData)
		{
			if (cmd is IEnvironmentLock env)
			{
				if (!env.AllowedEnvironments().Contains(_environmentService.Environment))
				{
					return false;
				}
			}

			if (_baseServiceConfig.DevelopmentMode)
			{
				return true;
			}

			if (cmd.AccessLevel() == CommandAccessLevel.Admin)
			{
				var data = playerState.DeserializeModel<PlayerData>();
				if (data.Flags.HasFlag(PlayerFlags.Admin))
				{
					return true;
				}
			}

			if (cmd.AccessLevel() == CommandAccessLevel.Service)
			{
				if (!FeatureFlags.QUANTUM_CUSTOM_SERVER)
				{
					return true;
				}

				var secretKey = PlayFabSettings.staticSettings.DeveloperSecretKey;
				return cmdData.TryGetValue("SecretKey", out var key) && key == secretKey;
			}

			return cmd.AccessLevel() == CommandAccessLevel.Player;
		}
	}
}