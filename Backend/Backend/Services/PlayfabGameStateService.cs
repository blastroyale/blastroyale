using System.Linq;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.ServerModels;
using ServerSDK.Models;
using ServerSDK.Services;

namespace Backend.Game.Services;

/// <summary>
/// Implements fetching & saving data to Playfab provider.
/// </summary>
public class PlayfabGameStateService : IServerStateService
{
	private readonly ILogger _log;
	private IErrorService<PlayFabError> _errorService;
	private IPlayfabServer _server;
	
	public PlayfabGameStateService(ILogger log, IErrorService<PlayFabError> errorService, IPlayfabServer server)
	{
		_log = log;
		_errorService = errorService;
		_server = server;
	}

	/// <inheritdoc />
	public UpdateUserDataResult UpdatePlayerState(string playerId, ServerState state)
	{
		var request = new UpdateUserDataRequest()
		{
			Data = state,
			PlayFabId = playerId
		};
		var server = _server.CreateServer(playerId);
		var req = server.UpdateUserReadOnlyDataAsync(request);
		req.Wait();
		var result = req.Result;
		if (result.Error != null)
		{
			throw _errorService.HandleError(result.Error);
		}
		return result.Result;
	}
	
	/// <inheritdoc />
	public ServerState GetPlayerState(string playfabId)
	{
		var server = _server.CreateServer(playfabId);
		var task = server.GetUserReadOnlyDataAsync(new GetUserDataRequest()
		{
			PlayFabId = playfabId
		});
		task.Wait();
		var result = task.Result;
		if (result.Error != null)
		{
			throw _errorService.HandleError(result.Error);
		}

		var fabResult = result.Result.Data.ToDictionary(
		                                                entry => entry.Key,
		                                                entry => entry.Value.Value);
		return new ServerState(fabResult);
	}
}