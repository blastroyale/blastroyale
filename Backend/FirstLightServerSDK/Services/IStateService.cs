using System.Threading.Tasks;
using FirstLight.Server.SDK.Models;

namespace FirstLight.Server.SDK.Services
{
	/// <summary>
	/// Service responsible for reading and saving player data.
	/// </summary>
	public interface IServerStateService
	{
		/// <summary>
		/// Saves the current ServerData referencing the specified PlayerId.
		/// </summary>
		public Task UpdatePlayerState(string playerId, ServerState state);
	
		/// <summary>
		/// Reads the player data and returns it as a ServerData type.
		/// </summary>
		public Task<ServerState> GetPlayerState(string playerId);

		/// <summary>
		/// Removes a given player state from server.
		/// </summary>
		public Task DeleteState(string playerId);
	}
}

