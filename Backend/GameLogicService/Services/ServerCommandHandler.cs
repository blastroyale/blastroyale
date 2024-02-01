using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Backend.Plugins;
using FirstLight;
using FirstLight.Game.Commands;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK;
using Microsoft.Extensions.Logging;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLightServerSDK.Services;
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

		/// <summary>
		/// Build all initialization commands
		/// </summary>
		public IGameCommand[] GetInitializationCommands();
	}

	/// <inheritdoc/>
	public class ServerCommandHandler : IServerCommahdHandler
	{
		private readonly IGameConfigurationService _cfg;
		private readonly ILogger _log;
		private readonly IEventManager _eventManager;
		private readonly IMetricsService _metrics;
		private readonly IPluginManager _plugins;
		private readonly IGameLogicContextService _logicContext;
		private readonly IStoreService _store;
		private readonly IItemCatalog<ItemData> _catalog;

		public ServerCommandHandler(IGameLogicContextService logicContext, IStoreService store, IItemCatalog<ItemData> catalog, IPluginManager plugins, IGameConfigurationService cfg, ILogger log, IEventManager eventManager,
									IMetricsService metrics)
		{
			_logicContext = logicContext;
			_cfg = cfg;
			_log = log;
			_eventManager = eventManager;
			_metrics = metrics;
			_plugins = plugins;
			_catalog = catalog;
			_store = store;
		}

		/// <inheritdoc/>
		public async Task<ServerState> ExecuteCommand(string userId, IGameCommand cmd, ServerState currentState)
		{
			var logicContext = await _logicContext.GetLogicContext(userId, currentState);
			var serviceContainer = new ServiceContainer();
			serviceContainer.Add(logicContext.GameLogic.MessageBrokerService);
			serviceContainer.Add(_catalog);
			serviceContainer.Add(_store);
			var logicContainer = new LogicContainer().Build(logicContext.GameLogic);
			var commandContext = new CommandExecutionContext(logicContainer, serviceContainer, logicContext.PlayerData);
			await cmd.Execute(commandContext);
			var newState = logicContext.PlayerData.GetUpdatedState();
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
			return _plugins.GetRegisteredCommand(typeName);
		}

		public IGameCommand[] GetInitializationCommands()
		{
			var types = _plugins.GetInitializationCommands();
			var commands = new IGameCommand[types.Length];
			for (var i = 0; i < types.Length; i++)
			{
				var type = types[i];
				commands[i] = Activator.CreateInstance(type) as IGameCommand;
			}

			return commands;
		}
	}
}