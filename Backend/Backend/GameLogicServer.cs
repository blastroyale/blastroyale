using System.Threading.Tasks;
using Backend.Game;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using PlayFab;
using ServerSDK;
using ServerSDK.Events;
using ServerSDK.Services;

namespace Backend;

/// <summary>
/// Represents the functionality of the game logic service. (AKA Game Backend).
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
}

public class GameLogicWebWebService : ILogicWebService
{
	private readonly IPlayerSetupService _setupService;
	private readonly IServerStateService _stateService;
	private readonly GameServer _server;
	private readonly IEventManager _eventManager;
	
	public GameLogicWebWebService(IEventManager eventManager, IPlayerSetupService service, IServerStateService stateService, GameServer server)
	{
		_setupService = service;
		_stateService = stateService;
		_server = server;
		_eventManager = eventManager;
	}

	public async Task<PlayFabResult<BackendLogicResult>> RunLogic(string playerId, LogicRequest request)
	{
		return new PlayFabResult<BackendLogicResult>
		{
			
			Result = _server.RunLogic(playerId, request)
		};
	}

	public async Task<PlayFabResult<BackendLogicResult>> GetPlayerData(string playerId)
	{
		if (!_setupService.IsSetup(_stateService.GetPlayerState(playerId)))
		{
			await SetupPlayer(playerId);
		}
		_eventManager.CallEvent(new PlayerDataLoadEvent(playerId));
		return new PlayFabResult<BackendLogicResult>
		{
			Result = new BackendLogicResult()
			{
				PlayFabId = playerId,
				Data = _stateService.GetPlayerState(playerId)
			}
		};
	}
	
	public async Task<PlayFabResult<BackendLogicResult>> SetupPlayer(string playerId)
	{
		var serverData = _setupService.GetInitialState(playerId);
		_stateService.UpdatePlayerState(playerId, serverData);
		return new PlayFabResult<BackendLogicResult>
		{
			Result = new BackendLogicResult
			{
				PlayFabId = playerId,
				Data = serverData
			}
		};
	}
}