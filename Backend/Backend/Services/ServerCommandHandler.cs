using System;
using System.Collections.Generic;
using System.Reflection;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Microsoft.Extensions.Logging;
using ServerSDK.Models;


namespace Backend.Game.Services;

/// <summary>
/// Server command handler, responsible for receiving and executing commands.
/// </summary>
public interface IServerCommahdHandler
{
	/// <summary>
	/// Will execute the given command on the given state. Returns an update state after the command execution.
	/// The returned state has the same reference as the input state.
	/// </summary>
	ServerState ExecuteCommand(IGameCommand cmd, ServerState currentState);

	/// <summary>
	/// By a given input call, will try to read the command data from input parameters
	/// and deserialize the data as a IGameCommand instance to be executed.
	/// </summary>
	public IGameCommand BuildCommandInstance(string commandData, string commandTypeName);
}

/// <inheritdoc/>
public class ServerCommandHandler : IServerCommahdHandler
{
	private readonly IConfigsProvider _cfg;
	private readonly ILogger _log;
	
	public ServerCommandHandler(IConfigsProvider cfg, ILogger log)
	{
		_cfg = cfg;
		_log = log;
	}

	/// <inheritdoc/>
	public ServerState ExecuteCommand(IGameCommand cmd, ServerState currentState)
	{
		var dataProvider = new ServerPlayerDataProvider(currentState);
		var logic = new GameServerLogic(_cfg, dataProvider);
		logic.Init();
		cmd.Execute(logic, dataProvider);
		var newState = dataProvider.GetUpdatedState();
		return newState;
	}

	/// <inheritdoc/>
	public IGameCommand BuildCommandInstance(string commandData, string commandTypeName)
	{
		var commandType = GetCommandType(commandTypeName);
		if (commandType == null)
		{
			throw new LogicException($"Invalid command type: {commandTypeName}");
		}
		return ModelSerializer.Deserialize<IGameCommand>(commandData, commandType);
	}
	
	/// <summary>
	/// Searches the game assembly for a type matching the given command type name.
	/// </summary>
	public Type GetCommandType(string typeName)
	{
		var gameAssembly = Assembly.GetAssembly(typeof(IGameCommand));
		var types = gameAssembly?.GetType(typeName);
		return types;
	}
}