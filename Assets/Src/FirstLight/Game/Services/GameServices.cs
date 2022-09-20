using FirstLight.Game.Logic;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.NotificationService;
using FirstLight.Server.SDK.Modules.GameConfiguration;
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

		/// <inheritdoc cref="IPlayfabService"/>
		IPlayfabService PlayfabService { get; }

		/// <inheritdoc cref="IRemoteTextureService"/>
		public IRemoteTextureService RemoteTextureService { get; }

		/// <inheritdoc cref="IThreadService"/>
		public IThreadService ThreadService { get; }
		
		/// <inheritdoc cref="IHelpdeskService"/>
		public IHelpdeskService HelpdeskService { get; }
		
		/// <inheritdoc cref="IGameModeService"/>
		public IGameModeService GameModeService { get; }
		
		/// <summary>
		/// Reason why the player quit the app
		/// </summary>
		public string QuitReason { get; }
		
		/// <summary>
		/// Method used when we want to leave the app, so we can record the reason
		/// </summary>
		/// <param name="reason">Reason why we quit the app</param>
		public void QuitGame(string reason);
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
		public IPlayfabService PlayfabService { get; }
		public IRemoteTextureService RemoteTextureService { get; }
		public IThreadService ThreadService { get; }
		public IHelpdeskService HelpdeskService { get; }
		public IGameModeService GameModeService { get; }
		public string QuitReason { get; set; }

		public GameServices(IGameNetworkService networkService, IMessageBrokerService messageBrokerService,
		                    ITimeService timeService, IDataService dataService, IConfigsProvider configsProvider,
		                    IGameLogic gameLogic,
		                    IGenericDialogService genericDialogService,
		                    IAssetResolverService assetResolverService,
		                    IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService)
		{
			NetworkService = networkService;
			AnalyticsService = new AnalyticsService(this, gameLogic, dataService);
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataService;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = genericDialogService;
			AudioFxService = audioFxService;
			VfxService = vfxService;

			ThreadService = new ThreadService();
			HelpdeskService = new HelpdeskService();
			GameModeService = new GameModeService(ConfigsProvider, ThreadService);
			GuidService = new GuidService();
			PlayfabService = new PlayfabService(gameLogic.AppLogic, messageBrokerService, GameConstants.Network.LEADERBOARD_LADDER_NAME);
			CommandService = new GameCommandService(PlayfabService, gameLogic, dataService, this, networkService);
			PoolService = new PoolService();
			TickService = new TickService();
			CoroutineService = new CoroutineService();
			PlayerInputService = new PlayerInputService();
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
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
			QuitReason = reason;
			Application.Quit();
		}
	}
}