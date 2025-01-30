using System.Threading.Tasks;
using FirstLight.Server.SDK;
using Microsoft.Extensions.Logging;
using FirstLight.Server.SDK.Models;
using FirstLightServerSDK.Services;
using GameLogicService.Game;
using GameLogicService.Models;


namespace Backend.Game.Services
{
	/// <summary>
	/// Service to create game logic execution context
	/// </summary>
	public interface IGameLogicContextService
	{
		/// <summary>
		/// Creates a game logic execution context to run for a given player.
		/// Current state can be passed as a optional argument. When not provided will be fetched.
		/// </summary>
		public Task<GameLogicExecutionContext> GetLogicContext(string userId, IRemoteConfigProvider remoteConfigProvider, ServerState currentState);
	}

	public class GameLogicContextService : IGameLogicContextService
	{
		private readonly IGameConfigurationService _cfg;
		private readonly ILogger _log;
		private readonly IEventManager _eventManager;

		public GameLogicContextService(IGameConfigurationService cfg, ILogger log, IEventManager eventManager)
		{
			_cfg = cfg;
			_log = log;
			_eventManager = eventManager;
		}

		public async Task<GameLogicExecutionContext> GetLogicContext(string userId, IRemoteConfigProvider remoteConfigProvider, ServerState currentState)
		{
			var dataProvider = new ServerPlayerDataProvider(currentState);
			dataProvider.ClearDeltas(); // initializing logic triggers deltas
			var msgBroker = new GameServerLogicMessageBroker(userId, _eventManager, _log);
			var logic = new GameServerLogic(await _cfg.GetGameConfigs(), remoteConfigProvider, dataProvider, msgBroker);
			logic.Init();
			return new GameLogicExecutionContext()
			{
				GameLogic = logic,
				PlayerData = dataProvider
			};
		}
	}
}