using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Services;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Services that have the lifecycle of a single match, and can only be accessed during one.
	/// </summary>
	public interface IMatchServices
	{
		public ISpectateService SpectateService { get; }
	}

	/// <summary>
	/// A service that receives <see cref="OnMatchStarted"/> and <see cref="OnMatchEnded"/> signals.
	/// </summary>
	public interface IMatchService
	{
		/// <summary>
		/// Triggered when <see cref="MatchStartedMessage"/> has been published.
		/// </summary>
		void OnMatchStarted();

		/// <summary>
		/// Triggered when <see cref="MatchEndedMessage"/> has been published.
		/// </summary>
		void OnMatchEnded();
	}

	internal class MatchServices : IMatchServices
	{
		public ISpectateService SpectateService { get; }

		private readonly List<IMatchService> _services = new();

		public MatchServices(IEntityViewUpdaterService entityViewUpdaterService,
		                     IMessageBrokerService messageBrokerService, IGameNetworkService networkService,
		                     IConfigsProvider configsProvider)
		{
			SpectateService = Configure(new SpectateService(entityViewUpdaterService, networkService, configsProvider));

			messageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStart);
			messageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnd);
		}

		private void OnMatchStart(MatchStartedMessage message)
		{
			foreach (var service in _services)
			{
				service.OnMatchStarted();
			}
		}

		private void OnMatchEnd(MatchEndedMessage message)
		{
			foreach (var service in _services)
			{
				service.OnMatchEnded();
			}
		}

		private T Configure<T>(T service) where T : IMatchService
		{
			_services.Add(service);
			return service;
		}
	}
}