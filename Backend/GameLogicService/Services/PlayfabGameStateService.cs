using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Data;
using Microsoft.Extensions.Logging;
using PlayFab;
using PlayFab.ServerModels;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;
using FirstLightServerSDK.Services;
using GameLogicService.Services;
using ServerCommon;

namespace Backend.Game.Services
{
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
		public async Task UpdatePlayerState(string playerId, ServerState state)
		{
			var request = new UpdateUserDataRequest()
			{
				PlayFabId = playerId
			};

			request.Data = state;
			var server = _server.CreateServer(playerId);
			var result = await server.UpdateUserReadOnlyDataAsync(request);
			_errorService.CheckErrors(result);
		}

		/// <inheritdoc />
		public async Task<ServerState> GetPlayerState(string playfabId)
		{
			var server = _server.CreateServer(playfabId);
			var result = await server.GetUserReadOnlyDataAsync(new GetUserDataRequest()
			{
				PlayFabId = playfabId
			});
			_errorService.CheckErrors(result);
			var fabResult = result.Result.Data.ToDictionary(
				entry => entry.Key,
				entry => entry.Value.Value);
			return new ServerState(fabResult);
		}

		public async Task DeletePlayerState(string playerId)
		{
			var currentState = await GetPlayerState(playerId);
			var playerData = currentState.DeserializeModel<PlayerData>();
			// Set flag in case playfab delays the deletion
			playerData.Flags |= PlayerFlags.Deleted;
			currentState.UpdateModel(playerData);
			await UpdatePlayerState(playerId, currentState);
			var result = await PlayFabAdminAPI.DeleteMasterPlayerAccountAsync(new()
			{
				PlayFabId = playerId
			});
			_errorService.CheckErrors(result);
		}
	}

	public static class ServerStateServiceExtensions
	{
		public static async Task<(ServerState, IRemoteConfigProvider)> FetchStateAndConfigs(this IServerStateService state, IRemoteConfigService remoteConfigService, string playerId, int clientConfigVersion)
		{
			var currentConfigTask = remoteConfigService.FetchConfig(clientConfigVersion);
			var currentPlayerStateTask = state.GetPlayerState(playerId);
			await Task.WhenAll(currentConfigTask, currentPlayerStateTask);
			return (currentPlayerStateTask.Result, currentConfigTask.Result);
		}
	}
}