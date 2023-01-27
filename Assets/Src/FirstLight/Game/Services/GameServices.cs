using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using FirstLight.NotificationService;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.UiService;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Provides access to all game's common helper services
	/// This services are stateless interfaces that establishes a set of available operations with deterministic response
	/// without manipulating any game’s data
	/// </summary>
	/// <remarks>
	/// Follows the "Service Locator Pattern" <see cref="https://www.geeksforgeeks.org/service-locator-pattern/"/>
	/// </remarks>
	public interface IGameServices
	{
		/// <inheritdoc cref="IDataSaver"/>
		IDataSaver DataSaver { get; }

		/// <inheritdoc cref="IConfigsProvider"/>
		IConfigsProvider ConfigsProvider { get; }

		/// <inheritdoc cref="IGuidService"/>
		IGuidService GuidService { get; }

		/// <inheritdoc cref="IGameNetworkService"/>
		IGameNetworkService NetworkService { get; }
		
		/// <inheritdoc cref="IPlayerInputService"/>
		IPlayerInputService PlayerInputService { get; }

		/// <inheritdoc cref="IMessageBrokerService"/>
		IMessageBrokerService MessageBrokerService { get; }

		/// <inheritdoc cref="ICommandService{T}"/>
		IGameCommandService CommandService { get; }

		/// <inheritdoc cref="IPoolService"/>
		IPoolService PoolService { get; }

		/// <inheritdoc cref="ITickService"/>
		ITickService TickService { get; }

		/// <inheritdoc cref="ITimeService"/>
		ITimeService TimeService { get; }

		/// <inheritdoc cref="ICoroutineService"/>
		ICoroutineService CoroutineService { get; }

		/// <inheritdoc cref="IAssetResolverService"/>
		IAssetResolverService AssetResolverService { get; }

		/// <inheritdoc cref="IAnalyticsService"/>
		IAnalyticsService AnalyticsService { get; }

		/// <inheritdoc cref="IGenericDialogService"/>
		IGenericDialogService GenericDialogService { get; }

		/// <inheritdoc cref="IVfxService{T}"/>
		IVfxService<VfxId> VfxService { get; }

		/// <inheritdoc cref="IAudioFxService{T}"/>
		IAudioFxService<AudioId> AudioFxService { get; }

		/// <inheritdoc cref="INotificationService"/>
		INotificationService NotificationService { get; }

		/// <inheritdoc cref="IGameBackendService"/>
		IGameBackendService GameBackendService { get; }
		
		/// <inheritdoc cref="IAuthenticationService"/>
		IAuthenticationService AuthenticationService { get; }
		
		/// <inheritdoc cref="IGameBackendService"/>
		ILiveopsService LiveopsService { get; }
		
		/// <inheritdoc cref="ITutorialService"/>
		ITutorialService TutorialService { get; }

		/// <inheritdoc cref="IRemoteTextureService"/>
		IRemoteTextureService RemoteTextureService { get; }

		/// <inheritdoc cref="IThreadService"/>
		IThreadService ThreadService { get; }
		
		/// <inheritdoc cref="IHelpdeskService"/>
		IHelpdeskService HelpdeskService { get; }
		
		/// <inheritdoc cref="IGameModeService"/>
		IGameModeService GameModeService { get; }
		
		/// <inheritdoc cref="IMatchmakingService"/>
		IMatchmakingService MatchmakingService { get; }

		/// <inheritdoc cref="IIAPService"/>
		IIAPService IAPService { get; }
		
		/// <summary>
		/// Reason why the player quit the app
		/// </summary>
		string QuitReason { get; }
		
		/// <summary>
		/// Method used when we want to leave the app, so we can record the reason
		/// </summary>
		/// <param name="reason">Reason why we quit the app</param>
		void QuitGame(string reason);
	}

	public class GameServices : IGameServices
	{
		public IDataSaver DataSaver { get; }
		public IConfigsProvider ConfigsProvider { get; }
		public IGuidService GuidService { get; }
		public IGameNetworkService NetworkService { get; }
		public IPlayerInputService PlayerInputService { get; }
		public IMessageBrokerService MessageBrokerService { get; }
		public IGameCommandService CommandService { get; }
		public IPoolService PoolService { get; }
		public ITickService TickService { get; }
		public ITimeService TimeService { get; }
		public ICoroutineService CoroutineService { get; }
		public IAssetResolverService AssetResolverService { get; }
		public IAnalyticsService AnalyticsService { get; }
		public IGenericDialogService GenericDialogService { get; }
		public IVfxService<VfxId> VfxService { get; }
		public IAudioFxService<AudioId> AudioFxService { get; }
		public INotificationService NotificationService { get; }
		public IGameBackendService GameBackendService { get; }
		public IAuthenticationService AuthenticationService { get; }
		public ILiveopsService LiveopsService { get; }
		public ITutorialService TutorialService { get; }
		public IRemoteTextureService RemoteTextureService { get; }
		public IThreadService ThreadService { get; }
		public IHelpdeskService HelpdeskService { get; }
		public IGameModeService GameModeService { get; }
		public IMatchmakingService MatchmakingService { get; }
		public IIAPService IAPService { get; }
		public string QuitReason { get; set; }

		public GameServices(IInternalGameNetworkService networkService, IMessageBrokerService messageBrokerService,
							ITimeService timeService, IDataService dataService, IConfigsAdder configsProvider,
							IGameLogic gameLogic, IGenericDialogService genericDialogService, 
							IAssetResolverService assetResolverService, ITutorialService tutorialService,
							IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService, IUiService uiService)
		{
			NetworkService = networkService;
			AnalyticsService = new AnalyticsService(this, gameLogic, dataService, uiService);
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataService;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = genericDialogService;
			AudioFxService = audioFxService;
			VfxService = vfxService;
			TutorialService = tutorialService;
			
			ThreadService = new ThreadService();
			HelpdeskService = new HelpdeskService();
			GameModeService = new GameModeService(ConfigsProvider, ThreadService);
			GuidService = new GuidService();
			GameBackendService = new GameBackendService(gameLogic, this, dataService, GameConstants.Stats.LEADERBOARD_LADDER_NAME);
			AuthenticationService = new PlayfabAuthenticationService(this, dataService,networkService, gameLogic, configsProvider);
			MatchmakingService = new PlayfabMatchmakingService(GameBackendService);
			LiveopsService = new LiveopsService(GameBackendService, ConfigsProvider, this, gameLogic.LiveopsLogic);
			CommandService = new GameCommandService(GameBackendService, gameLogic, dataService, this);
			PoolService = new PoolService();
			TickService = new TickService();
			CoroutineService = new CoroutineService();
			PlayerInputService = new PlayerInputService();
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			IAPService = new IAPService(CommandService, MessageBrokerService, GameBackendService, AnalyticsService, gameLogic);
			NotificationService = new MobileNotificationService(
			                                                    new
				                                                    GameNotificationChannel(GameConstants.Notifications.NOTIFICATION_BOXES_CHANNEL,
					                                                    GameConstants.Notifications
						                                                    .NOTIFICATION_BOXES_CHANNEL,
					                                                    GameConstants.Notifications
						                                                    .NOTIFICATION_BOXES_CHANNEL),
			                                                    new
				                                                    GameNotificationChannel(GameConstants.Notifications.NOTIFICATION_IDLE_BOXES_CHANNEL,
					                                                    GameConstants.Notifications
						                                                    .NOTIFICATION_IDLE_BOXES_CHANNEL,
					                                                    GameConstants.Notifications
						                                                    .NOTIFICATION_IDLE_BOXES_CHANNEL));
		}
		
		/// <inheritdoc />
		public void QuitGame(string reason) 
		{
			MessageBrokerService.Publish(new ApplicationQuitMessage());
			QuitReason = reason;
			#if UNITY_EDITOR
				UnityEditor.EditorApplication.isPlaying = false;
			#else
				Application.Quit(); // Apple does not allow to close the app so might not work on iOS :<
			#endif
		}
	}
}