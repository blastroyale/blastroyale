using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Services;


namespace Backend.Game.Services
{
	/// <inheritdoc />
	public class DefaultPlayerSetupService : IPlayerSetupService
	{
		/// <inheritdoc />
		public ServerState GetInitialState(string playFabId)
		{
			return new ServerState();
		}

		/// <inheritdoc />
		public bool IsSetup(ServerState state)
		{
			return state != null;
		}
	}
}


