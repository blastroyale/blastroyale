using PlayFab.ServerModels;
using ServerSDK.Models;

namespace ServerSDK.Services;

/// <summary>
/// Service responsible for reading and saving player data.
/// </summary>
public interface IServerStateService
{
	/// <summary>
	/// Saves the current ServerData referencing the specified PlayerId.
	/// </summary>
	public UpdateUserDataResult UpdatePlayerState(string playerId, ServerState state);
	
	/// <summary>
	/// Reads the player data and returns it as a ServerData type.
	/// </summary>
	public ServerState GetPlayerState(string playerId);
}