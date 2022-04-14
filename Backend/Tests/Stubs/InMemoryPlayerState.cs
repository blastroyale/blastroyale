using System.Collections.Generic;
using Backend.Game.Services;
using Backend.Models;
using FirstLight.Game.Logic;
using PlayFab.ServerModels;

namespace Tests.Stubs;

public class InMemoryPlayerState : IServerStateService
{
	private Dictionary<string, ServerState> _states = new Dictionary<string, ServerState>();

	public UpdateUserDataResult UpdatePlayerState(string playerId, ServerState state)
	{
		_states[playerId] = state;
		return new UpdateUserDataResult();
	}

	public ServerState GetPlayerState(string playerId)
	{
		ServerState? state = null;
		if (!_states.TryGetValue(playerId, out state))
			state = new ServerState();
		return state;
	}
}