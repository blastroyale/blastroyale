using FirstLight.Server.SDK.Models;

namespace FirstLight.Server.SDK.Services
{
	/// <summary>
	/// Service responsible for generating initial player state.
	/// </summary>
	public interface IPlayerSetupService
	{
		/// <summary>
		/// Generates initial player state and returns as server data.
		/// </summary>
		public ServerState GetInitialState(string playFabId);

		/// <summary>
		/// Checks if a given state is already setup
		/// </summary>
		public bool IsSetup(ServerState state);
	}
}