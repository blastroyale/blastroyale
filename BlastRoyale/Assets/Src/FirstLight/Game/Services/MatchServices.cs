using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Domains.Flags;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services.Match;
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
	public interface IMatchServices : IDisposable, IMatchServiceAssetLoader
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
		public IWeaponCustomizationService WeaponCustomization { get; }
		public IMatchVfxService VfxService { get; }
		public IMatchFlagService FlagService { get; }

		/// <summary>
		///  Run the actions when the match starts, if the match already started run instantaneously
		///  The bool parameter is if is a reconnection
		/// </summary>
		public void RunOnMatchStart(Action<bool> action);
	}

	internal class MatchServices : IMatchServices
	{
		private MatchEndDataService _matchEndDataService;
		private readonly IMessageBrokerService _messageBrokerService;
		private readonly List<IMatchService> _services = new ();
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
		public IHapticsService HapticsService { get; }
		public IWeaponCustomizationService WeaponCustomization { get; }
		public IMatchAssetsService MatchAssetService { get; }
		public IMatchVfxService VfxService { get; }
		public IMatchFlagService FlagService { get; }
		public ITeamService TeamService { get; }

		private bool _matchStarted = false;
		private bool _isReconnect = false;
		private Action<bool> _runOnMatchStart;

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
			MatchCameraService = Configure(new MatchCameraService(dataProvider, this, services));
			PlayerInputService = Configure(new PlayerInputService(_gameServices, this, _dataProvider));
			PlayerIndicatorService = Configure(new PlayerIndicatorsService(this, _gameServices));
			EntityVisibilityService = Configure(new EntityVisibilityService(this, _gameServices));
			BulletService = Configure(new BulletService(_gameServices, this));
			HapticsService = Configure(new HapticsService(_gameServices.LocalPrefsService));
			MatchAssetService = Configure(new MatchAssetsService());
			VfxService = Configure(new MatchVfxService(_gameServices));
			FlagService = Configure(new MatchFlagService(_gameServices));
			WeaponCustomization = Configure(new WeaponCustomizationService(services, VfxService));
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

		public void RunOnMatchStart(Action<bool> action)
		{
			if (_matchStarted)
			{
				action.Invoke(_isReconnect);
				return;
			}

			_runOnMatchStart += action;
		}

		private void OnMatchStart(MatchStartedMessage message)
		{
			if (!CanTriggerMessage()) return;
			_isReconnect = message.IsResync;
			_matchStarted = true;
			foreach (var service in _services)
			{
				service.OnMatchStarted(message.Game, message.IsResync);
			}

			_runOnMatchStart?.Invoke(_isReconnect);
			_runOnMatchStart = null;
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

		public UniTask LoadMandatoryAssets()
		{
			var tasks = _services.Where(srv => srv is IMatchServiceAssetLoader)
				.Select(srv => ((IMatchServiceAssetLoader) srv).LoadMandatoryAssets());
			return UniTask.WhenAll(tasks);
		}

		public UniTask LoadOptionalAssets()
		{
			var tasks = _services.Where(srv => srv is IMatchServiceAssetLoader)
				.Select(srv => ((IMatchServiceAssetLoader) srv).LoadOptionalAssets());
			return UniTask.WhenAll(tasks);
		}

		public UniTask UnloadAssets()
		{
			var tasks = _services.Where(srv => srv is IMatchServiceAssetLoader)
				.Select(srv => ((IMatchServiceAssetLoader) srv).UnloadAssets());
			return UniTask.WhenAll(tasks);
		}
	}

	public interface IMatchServiceAssetLoader
	{
		public UniTask LoadMandatoryAssets();
		public UniTask LoadOptionalAssets();
		public UniTask UnloadAssets();
	}

	/// <summary>
	/// A service that receives <see cref="OnMatchStarted"/> and <see cref="OnMatchEnded"/> signals and only lives
	/// during the cycle of a match in the game simulation.
	/// </summary>
	/// <remarks>
	/// As <see cref="IMatchService"/> only exists inside the scope of the <see cref="MatchServices"/>, it requires
	/// the direct invocation to avoid confusion with similar named interface <see cref="IMatchServices"/>
	/// </remarks>
	internal interface IMatchService : IDisposable
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
}