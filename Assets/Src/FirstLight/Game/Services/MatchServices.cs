using System;
using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.StateMachines;
using FirstLight.Services;
using Quantum;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Services that have the lifecycle of a single match, and can only be accessed during one.
	/// </summary>
	public interface IMatchServices : IDisposable
	{
		/// <inheritdoc cref="ISpectateService"/>
		public ISpectateService SpectateService { get; }
		
		/// <inheritdoc cref="IEntityViewUpdaterService"/>
		public IEntityViewUpdaterService EntityViewUpdaterService { get; }
		
		/// <inheritdoc cref="ILocalPlayerService"/>
		public ILocalPlayerService LocalPlayerService { get; }
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
		public interface IMatchService : IDisposable
		{
			/// <summary>
			/// Triggered when <see cref="MatchStartedMessage"/> has been published.
			/// </summary>
			void OnMatchStarted(QuantumGame game, bool isReconnect);

			/// <summary>
			/// Triggered when <see cref="MatchEndedMessage"/> has been published.
			/// </summary>
			void OnMatchEnded();
		}

		private readonly IMessageBrokerService _messageBrokerService;
		private readonly List<IMatchService> _services = new();
		
		/// <inheritdoc />
		public ISpectateService SpectateService { get; }
		/// <inheritdoc />
		public IEntityViewUpdaterService EntityViewUpdaterService { get; }
		/// <inheritdoc />
		public ILocalPlayerService LocalPlayerService { get; }

		public MatchServices(IEntityViewUpdaterService entityViewUpdaterService, IGameServices services)
		{
			_messageBrokerService = services.MessageBrokerService;

			EntityViewUpdaterService = entityViewUpdaterService;
			SpectateService = Configure(new SpectateService(services, this));

			if (!services.NetworkService.IsSpectorPlayer)
			{
				LocalPlayerService = Configure(new LocalPlayerService(services, this));
			}

			_messageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStart);
			_messageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnd);
		}

		public void Dispose()
		{
			Object.Destroy(((EntityViewUpdaterService) EntityViewUpdaterService)?.gameObject);
			_messageBrokerService?.UnsubscribeAll(this);
			
			foreach (var service in _services)
			{
				service.Dispose();
			}
		}

		private void OnMatchStart(MatchStartedMessage message)
		{
			foreach (var service in _services)
			{
				service.OnMatchStarted(message.Game, message.IsResync);
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