using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK;
using Microsoft.Extensions.Logging;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using GameLogicService.Game;


namespace Backend.Game.Services
{
	/// <summary>
	/// Server command handler, responsible for receiving and executing commands.
	/// </summary>
	public interface IServerCommahdHandler
	{
		/// <summary>
		/// Will execute the given command on the given state. Returns an update state after the command execution.
		/// The returned state has the same reference as the input state.
		/// </summary>
		Task<ServerState> ExecuteCommand(string userId, IGameCommand cmd, ServerState currentState);

		/// <summary>
		/// By a given input call, will try to read the command data from input parameters
		/// and deserialize the data as a IGameCommand instance to be executed.
		/// </summary>
		public IGameCommand BuildCommandInstance(string commandData, string commandTypeName);
	}

	/// <inheritdoc/>
	public class ServerCommandHandler : IServerCommahdHandler
	{
		private readonly IGameConfigurationService _cfg;
		private readonly ILogger _log;
		private readonly IEventManager _eventManager;
		private readonly IMetricsService _metrics;

		public ServerCommandHandler(IGameConfigurationService cfg, ILogger log, IEventManager eventManager,
									IMetricsService metrics)
		{
			_cfg = cfg;
			_log = log;
			_eventManager = eventManager;
			_metrics = metrics;
		}

		/// <inheritdoc/>
		public async Task<ServerState> ExecuteCommand(string userId, IGameCommand cmd, ServerState currentState)
		{
			var dataProvider = new ServerPlayerDataProvider(currentState);
			var msgBroker = new GameServerLogicMessageBroker(userId, _eventManager, _log);
			var logic = new GameServerLogic(await _cfg.GetGameConfigs(), dataProvider, msgBroker);
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
}