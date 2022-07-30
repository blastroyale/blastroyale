using System;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Services;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Services that have the lifecycle of a single match, and can only be accessed during one.
	/// </summary>
	public interface IMatchServices : IDisposable
	{
		/// <inheritdoc cref="ISpectateService"/>
		public ISpectateService SpectateService { get; }
	}

	internal class MatchServices : IMatchServices
	{
		/// <summary>
		/// A service that receives <see cref="OnMatchStarted"/> and <see cref="OnMatchEnded"/> signals and only lives
		/// during the cycle of a match in the game simulation.
		/// </summary>
		/// <remarks>
		/// As <see cref="IMatchService"/> only exists inside the scope of the <see cref="MatchServices"/>, it requires
		/// the direct invocation to avoid confusion with similar named interface <see cref="IMatchServices"/>
		/// </remarks>
		public interface IMatchService
		{
			/// <summary>
			/// Triggered when <see cref="MatchStartedMessage"/> has been published.
			/// </summary>
			void OnMatchStarted(bool isReconnect);

			/// <summary>
			/// Triggered when <see cref="MatchEndedMessage"/> has been published.
			/// </summary>
			void OnMatchEnded();
		}
		
		public ISpectateService SpectateService { get; }

		private readonly IMessageBrokerService _messageBrokerService;

		private readonly List<IMatchService> _services = new();

		public MatchServices(IEntityViewUpdaterService entityViewUpdaterService, IGameServices services)
		{
			_messageBrokerService = services.MessageBrokerService;

			SpectateService = Configure(new SpectateService(entityViewUpdaterService, services.NetworkService, 
			                                                services.ConfigsProvider));

			_messageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStart);
			_messageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnd);
		}

		public void Dispose()
		{
			_messageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchStart(MatchStartedMessage message)
		{
			foreach (var service in _services)
			{
				service.OnMatchStarted(message.IsResync);
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