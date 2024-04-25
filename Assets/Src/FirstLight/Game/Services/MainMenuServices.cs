using System;
using FirstLight.Services;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Provides access to all main menu's common helper services
	/// This services are stateless interfaces that establishes a set of available operations with deterministic response
	/// without manipulating any game’s data
	/// </summary>
	/// <remarks>
	/// Follows the "Service Locator Pattern" <see cref="https://www.geeksforgeeks.org/service-locator-pattern/"/>
	/// </remarks>
	// TODO mihak: Remove all this
	public interface IMainMenuServices : IDisposable
	{

		/// <inheritdoc cref="IRemoteTextureService"/>
		IRemoteTextureService RemoteTextureService { get; }
	}

	/// <inheritdoc />
	public class MainMenuServices : IMainMenuServices
	{
		/// <inheritdoc />
		public IRemoteTextureService RemoteTextureService { get; }

		public MainMenuServices(IRemoteTextureService remoteTextureService)
		{
			RemoteTextureService = remoteTextureService;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			// Do Nothing
		}
	}
}