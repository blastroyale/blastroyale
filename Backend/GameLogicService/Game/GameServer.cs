using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Logging;
using PlayFab;
using FirstLight.Server.SDK;
using FirstLight.Server.SDK.Events;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Server.SDK.Services;
using IGameCommand = FirstLight.Game.Commands.IGameCommand;
using FirstLight.Game.Commands;

namespace Backend.Game
{
	/// <summary>
/// Class represents game-server instance. Holds logic methods to run gamelogic.
/// This should be networking agnostic.
/// </summary>
public class GameServer
{
	private IServerCommahdHandler _cmdHandler;
	private ILogger _log;
	private IServerStateService _state;
	private IServerMutex _mutex;
	private IEventManager _eventManager;
	private IMetricsService _metrics;
	private IServerConfiguration _serverConfig;
	private IConfigsProvider _gameConfigs;

	public GameServer(IConfigsProvider gameConfigs, IServerConfiguration serverConfig, IServerCommahdHandler cmdHandler, ILogger log, IServerStateService state, IServerMutex mutex, IEventManager eventManager, IMetricsService metrics)
	{
		_cmdHandler = cmdHandler;
		_log = log;
		_state = state;
		_mutex = mutex;
		_eventManager = eventManager;
		_metrics = metrics;
		_serverConfig = serverConfig;
		_gameConfigs = gameConfigs;
	}
	
	/// <summary>
	/// Runs a logic request in server state for the given player. This will persist the updated state at the end.
	/// </summary>
	public async Task<BackendLogicResult> RunLogic(string playerId, LogicRequest logicRequest)
	{
		var cmdType = logicRequest.Command;
		var requestData = logicRequest.Data;
		try
		{
			if (!requestData.TryGetValue(CommandFields.Command, out var commandData))
			{
				throw new LogicException($"Input dict requires field key for cmd: {CommandFields.Command}");
			}
			_log.LogDebug($"{playerId} running {cmdType}");
			await _mutex.Lock(playerId);
			var commandInstance = _cmdHandler.BuildCommandInstance(commandData, cmdType);
			var currentPlayerState = await _state.GetPlayerState(playerId);
			ValidateCommand(currentPlayerState, commandInstance, requestData);
			
			var newState = await _cmdHandler.ExecuteCommand(commandInstance, currentPlayerState);
			_eventManager.CallEvent(new CommandFinishedEvent(playerId, commandInstance, newState, currentPlayerState, commandData));
			await _state.UpdatePlayerState(playerId, newState);
			
			if(requestData.TryGetValue(CommandFields.ConfigurationVersion, out var clientConfigVersion))
			{
				var clientConfigVersionNumber = ulong.Parse(clientConfigVersion);
				if (_gameConfigs.Version > clientConfigVersionNumber)
				{
					newState[CommandFields.ConfigurationVersion] = _gameConfigs.Version.ToString();
				}
			}
			
			return new BackendLogicResult()
			{
				Command = cmdType,
				Data = newState,
				PlayFabId = playerId
			};
		}
		catch (LogicException e)
		{
			_log.LogError(e, $"Exception running command {logicRequest.Command}");
			return GetErrorResult(logicRequest, e);
		}
		finally
		{
			_mutex.Unlock(playerId);
			_metrics.EmitEvent($"Command {logicRequest.Command}");
		}
	}

	/// <summary>
	/// Validates if a given command with a given input can be ran on a given player state.
	/// Will raise exceptions in case its not feasible to run the command.
	/// </summary>
	public bool ValidateCommand(ServerState state, IGameCommand cmd, Dictionary<string,string> cmdData)
	{
		if (!HasAccess(state, cmd, cmdData))
		{
			throw new LogicException("Insuficient permissions to run command");
		}
		if(cmd.ExecutionMode() == CommandExecutionMode.Quantum)
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

		var minVersion = _serverConfig.MinClientVersion;
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
	private BackendLogicResult GetErrorResult(LogicRequest request, LogicException exp)
	{
		return new BackendLogicResult()
		{
			Command = request.Command,
			Data = new Dictionary<string, string>()
			{
				{ "LogicException", exp.Message }
			}
		};
	}

	/// <summary>
	/// Checks if a given player has enough permissions to run a given command.
	/// </summary>s
	private bool HasAccess(ServerState playerState, IGameCommand cmd, Dictionary<string,string> cmdData)
	{
		if (_serverConfig.DevelopmentMode)
		{
			return true;
		}
		// TODO: Validate player access level in player state for admin commands (GMs)
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

