using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Services;


public class InMemoryPlayerState : IServerStateService
{
	private Dictionary<string, string> _states = new Dictionary<string, string>();

	public async Task UpdatePlayerState(string playerId, ServerState state)
	{
		var oldState = await GetPlayerState(playerId);
		foreach (var key in state.Keys)
		{
			oldState[key] = state[key];
		}
		_states[playerId] = ModelSerializer.Serialize(oldState).Value;
	}

	public async Task<ServerState> GetPlayerState(string playerId)
	{
		if (!_states.TryGetValue(playerId, out var stateJson))
			return new ServerState();
		return ModelSerializer.Deserialize<ServerState>(stateJson);
	}

	public async Task DeletePlayerState(string playerId)
	{
		_states.Remove(playerId);
	}
}
