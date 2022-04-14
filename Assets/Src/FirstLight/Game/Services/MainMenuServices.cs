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
	}
	
	/// <inheritdoc />
	public class MainMenuServices : IMainMenuServices
	{
		private readonly IUiVfxInternalService _uiVfxService;
		private readonly IAssetResolverService _assetResolverService;
		private readonly IMessageBrokerService _messageBrokerService;

		/// <inheritdoc />
		public IMessageBrokerService MessageBrokerService => _messageBrokerService;

		/// <inheritdoc />
		public IUiVfxService UiVfxService => _uiVfxService;
		
		public MainMenuServices(IAssetResolverService assetResolverService, IUiVfxInternalService uiVfxService, IMessageBrokerService messageBrokerService)
		{
			_assetResolverService = assetResolverService;
			_uiVfxService = uiVfxService;
			_messageBrokerService = messageBrokerService;
		}
		
		/// <inheritdoc />
		public void Dispose()
		{
			// Do Nothing
		}
	}
}