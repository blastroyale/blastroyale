using FirstLight.Game.Logic;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.NotificationService;

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
		
		/// <inheritdoc cref="IGameFlowService"/>
		public IGameFlowService GameFlowService { get; }
		public IDataProvider DataProvider { get; }
	}

	public class GameServices : IGameServices
	{
		public IDataSaver DataSaver { get; }
		public IConfigsProvider ConfigsProvider { get; }
		public IGuidService GuidService { get; }
		public IGameNetworkService NetworkService { get; }
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
		public IGameFlowService GameFlowService { get; }
		public IDataProvider DataProvider { get; }

		public GameServices(IGameNetworkService networkService, IMessageBrokerService messageBrokerService,
		                    ITimeService timeService, IDataSaver dataSaver, IConfigsProvider configsProvider,
		                    IGameLogic gameLogic, IDataProvider dataProvider,
		                    IGenericDialogService genericDialogService,
		                    IAssetResolverService assetResolverService, IAnalyticsService analyticsService,
		                    IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService,
		                    IThreadService threadService, IGameFlowService gameFlowService)
		{
			NetworkService = networkService;
			AnalyticsService = analyticsService;
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataSaver;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = genericDialogService;
			AudioFxService = audioFxService;
			VfxService = vfxService;
			ThreadService = threadService;
			GameFlowService = gameFlowService;
			DataProvider = dataProvider;

			GuidService = new GuidService();
			PlayfabService = new PlayfabService(gameLogic.AppLogic, messageBrokerService);
			CommandService = new GameCommandService(PlayfabService, gameLogic, dataProvider);
			PoolService = new PoolService();
			TickService = new TickService();
			CoroutineService = new CoroutineService();
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
			HelpdeskService = new HelpdeskService();
		}
		
	}
}