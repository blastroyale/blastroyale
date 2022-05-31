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
	public interface IMainMenuServices : IDisposable
	{
		/// <inheritdoc cref="IUiVfxService"/>
		IUiVfxService UiVfxService { get; }

		IRemoteTextureService RemoteTextureService { get; }
	}

	/// <inheritdoc />
	public class MainMenuServices : IMainMenuServices
	{
		public IUiVfxService UiVfxService { get; }
		public IRemoteTextureService RemoteTextureService { get; }

		public MainMenuServices(IUiVfxInternalService uiVfxService, IRemoteTextureService remoteTextureService)
		{
			UiVfxService = uiVfxService;
			RemoteTextureService = remoteTextureService;
		}

		/// <inheritdoc />
		public void Dispose()
		{
			// Do Nothing
		}
	}
}