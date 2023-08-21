using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Services;
using Quantum;
using Object = UnityEngine.Object;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Services that have the lifecycle of a single match, and can only be accessed during one.
	/// </summary>
	public interface  IMatchServices : IDisposable
	{
		public ISpectateService SpectateService { get; }
		public IEntityViewUpdaterService EntityViewUpdaterService { get; }
		public IFrameSnapshotService FrameSnapshotService { get; }
		public IMatchEndDataService MatchEndDataService { get; }
		public IMatchCameraService MatchCameraService { get; }
		public IEntityVisibilityService EntityVisibilityService { get; }
		public IPlayerInputService PlayerInputService { get; }
		public IPlayerIndicatorService PlayerIndicatorService { get; }
		public IBulletService BulletService { get; }
		public IMatchAssetsService MatchAssetService { get; }
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
			void OnMatchEnded(QuantumGame game, bool isDisconnected);
		}

		private MatchEndDataService _matchEndDataService;
		private readonly IMessageBrokerService _messageBrokerService;
		private readonly List<IMatchService> _services = new();
		private IGameServices _gameServices;
		private IGameDataProvider _dataProvider;
		
		public ISpectateService SpectateService { get; }
		public IEntityViewUpdaterService EntityViewUpdaterService { get; }
		public IFrameSnapshotService FrameSnapshotService { get; }
		public IMatchEndDataService MatchEndDataService { get; }
		public IMatchCameraService MatchCameraService { get; }
		public IPlayerInputService PlayerInputService { get; }
		public IPlayerIndicatorService PlayerIndicatorService { get; }
		public IEntityVisibilityService EntityVisibilityService { get; }
		public IBulletService BulletService { get; }
		
		public IMatchAssetsService MatchAssetService { get; }

		public MatchServices(IEntityViewUpdaterService entityViewUpdaterService, 
							 IGameServices services, 
							 IGameDataProvider dataProvider, 
							 IDataService dataService)
		{
			_messageBrokerService = services.MessageBrokerService;
			_gameServices = services;
			_dataProvider = dataProvider;

			EntityViewUpdaterService = entityViewUpdaterService;
			SpectateService = Configure(new SpectateService(services, this));
			FrameSnapshotService = Configure(new FrameSnapshotService(dataService));
			MatchEndDataService = Configure(new MatchEndDataService(_gameServices, _dataProvider));
			MatchCameraService = Configure(new MatchCameraService(dataProvider, this));
			PlayerInputService = Configure(new PlayerInputService(_gameServices, this, _dataProvider));
			PlayerIndicatorService = Configure(new PlayerIndicatorsService(this, _gameServices));
			EntityVisibilityService = Configure(new EntityVisibilityService(this, _gameServices));
			BulletService = Configure(new BulletService(_gameServices, this));
			MatchAssetService = Configure(new MatchAssetsService());
			_messageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStart);
			_messageBrokerService.Subscribe<MatchEndedMessage>(OnMatchEnd);
			FLog.Verbose("Registered Match Services");
		}

		public void Dispose()
		{
			Object.Destroy(((EntityViewUpdaterService) EntityViewUpdaterService)?.gameObject);
			_messageBrokerService?.UnsubscribeAll(this);
			
			foreach (var service in _services)
			{
				service.Dispose();
			}
			FLog.Verbose("Removed Match Services");
		}

		private bool CanTriggerMessage()
		{
			return _gameServices.NetworkService.QuantumClient.IsConnectedAndReady && QuantumRunner.Default.IsDefinedAndRunning();
		}

		private void OnMatchStart(MatchStartedMessage message)
		{
			foreach (var service in _services)
			{
				if (!CanTriggerMessage()) return;
				service.OnMatchStarted(message.Game, message.IsResync);
			}
		}

		private void OnMatchEnd(MatchEndedMessage message)
		{
			foreach (var service in _services)
			{
				if (!CanTriggerMessage()) return;
				service.OnMatchEnded(message.Game, message.IsDisconnected);
			}
		}

		private T Configure<T>(T service) where T : IMatchService
		{
			_services.Add(service);
			return service;
		}
	}
}
