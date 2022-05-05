using System;
using System.Collections.Generic;
using Backend.Game.Services;
using Backend.Models;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using Microsoft.Extensions.Logging;

namespace Backend.Game;

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

	/// <summary>
	/// Returns if the server is setup to run dev-mode. In dev-mode all players are admin and cheating will be enabled.
	/// </summary>
	public bool DevMode => Environment.GetEnvironmentVariable("DEV_MODE", EnvironmentVariableTarget.Process) == "true";
	
	public GameServer(IServerCommahdHandler cmdHandler, ILogger log, IServerStateService state, IServerMutex mutex)
	{
		_cmdHandler = cmdHandler;
		_log = log;
		_state = state;
		_mutex = mutex;
	}
	
	/// <summary>
	/// Runs a logic request in server state for the given player. This will persist the updated state at the end.
	/// </summary>
	public BackendLogicResult RunLogic(string playerId, LogicRequest logicRequest)
	{
		var cmdType = logicRequest.Command;
		var cmdData = logicRequest.Data;
		try
		{
			_mutex.Lock(playerId);
			_log.Log(LogLevel.Information, $"Player {playerId} running server command {cmdType}");
			var commandInstance = _cmdHandler.BuildCommandInstance(cmdData, cmdType);
			var currentPlayerState = _state.GetPlayerState(playerId);
			ValidateCommand(currentPlayerState, commandInstance, cmdData);
			var newState = _cmdHandler.ExecuteCommand(commandInstance, currentPlayerState);
			_state.UpdatePlayerState(playerId, newState);
			return new BackendLogicResult()
			{
				Command = cmdType,
				Data = newState,
				PlayFabId = playerId
			};
		}
		finally
		{
			_mutex.Unlock(playerId);
		}
	}

	/// <summary>
	/// Validates if a given command with a given input can be ran on a given player state.
	/// Will raise exceptions in case its not feasible to run the command.
	/// </summary>
	public bool ValidateCommand(ServerState state, IGameCommand cmd, Dictionary<string,string> cmdData)
	{
		if (!HasAccess(state, cmd))
		{
			throw new LogicException("Insuficient permissions to run command");
		}

		if (!cmdData.TryGetValue(CommandFields.Timestamp, out var currentCommandTimeString))
		{
			throw new LogicException($"Command data requires a timestamp to be ran: Key {CommandFields.Timestamp}");
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
	/// Checks if a given player has enough permissions to run a given command.
	/// </summary>
	private bool HasAccess(ServerState playerState, IGameCommand cmd)
	{
		// TODO: Validate player access level in player state
		return DevMode || cmd.AccessLevel == CommandAccessLevel.Player;
	}
}