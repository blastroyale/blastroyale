using System;
using Backend.Game.Services;
using Backend.Models;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
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

	public bool DevMode => Environment.GetEnvironmentVariable("DEV_MODE", EnvironmentVariableTarget.Process) == "true";
	
	
	public GameServer(IServerCommahdHandler cmdHandler, ILogger log, IServerStateService state)
	{
		_cmdHandler = cmdHandler;
		_log = log;
		_state = state;
	}
	
	/// <summary>
	/// Runs a logic request in server state for the given player. This will persist the updated state at the end.
	/// </summary>
	public BackendLogicResult RunLogic(string playerId, LogicRequest logicRequest)
	{
		var cmdType = logicRequest.Command;
		var cmdData = logicRequest.Data;
		_log.Log(LogLevel.Information, $"Player {playerId} running server command {cmdType}");
		var commandInstance = _cmdHandler.BuildCommandInstance(cmdData, cmdType);
		var currentPlayerState = _state.GetPlayerState(playerId);
		if (!HasAccess(currentPlayerState, commandInstance))
			throw new LogicException("Insuficient permissions to run command");
		
		var newState = _cmdHandler.ExecuteCommand(commandInstance, currentPlayerState);
		_state.UpdatePlayerState(playerId, newState);
		return new BackendLogicResult()
		{
			Command = cmdType,
			Data = newState,
			PlayFabId = playerId
		};
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