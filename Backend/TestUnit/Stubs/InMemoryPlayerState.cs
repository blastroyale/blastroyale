using System.Collections.Generic;
using System.Threading.Tasks;
using Backend.Game.Services;
using FirstLight.Game.Logic;
using PlayFab.ServerModels;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;


public class InMemoryPlayerState : IServerStateService
{
	private Dictionary<string, ServerState> _states = new Dictionary<string, ServerState>();

	public async Task UpdatePlayerState(string playerId, ServerState state)
	{
		_states[playerId] = state;
	}

	public async Task<ServerState> GetPlayerState(string playerId)
	{
		ServerState? state = null;
		if (!_states.TryGetValue(playerId, out state))
			state = new ServerState();
		return state;
	}
}