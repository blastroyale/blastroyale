using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NotificationService;
using FirstLight.Services;

namespace FirstLight.Tests.EditorMode
{
	public class StubGameServices : IGameServices
	{
		public virtual IDataSaver DataSaver { get; }
		public virtual IConfigsProvider ConfigsProvider { get; }
		public virtual IGuidService GuidService { get; }
		public virtual IGameNetworkService NetworkService { get; }
		public virtual IPlayerInputService PlayerInputService { get; }
		public virtual IMessageBrokerService MessageBrokerService { get; }
		public virtual IGameCommandService CommandService { get; }
		public virtual IPoolService PoolService { get; }
		public virtual ITickService TickService { get; }
		public virtual ITimeService TimeService { get; }
		public virtual ICoroutineService CoroutineService { get; }
		public virtual IAssetResolverService AssetResolverService { get; }
		public virtual IAnalyticsService AnalyticsService { get; }
		public virtual IGenericDialogService GenericDialogService { get; }
		public virtual IVfxService<VfxId> VfxService { get; }
		public virtual IAudioFxService<AudioId> AudioFxService { get; }
		public virtual INotificationService NotificationService { get; }
		public virtual IPlayfabService PlayfabService { get; }
		public virtual IRemoteTextureService RemoteTextureService { get; }
		public virtual IThreadService ThreadService { get; }
		public virtual IHelpdeskService HelpdeskService { get; }
		public string QuitReason { get; set; }

		public void QuitGame(string reason)
		{
		}

		public StubGameServices(IGameNetworkService networkService, IMessageBrokerService messageBrokerService,
		                        ITimeService timeService, IDataSaver dataSaver, IConfigsProvider configsProvider,
		                        IGameLogic gameLogic, IDataProvider dataProvider,
		                        IGenericDialogService genericDialogService,
		                        IAssetResolverService assetResolverService,
		                        IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService,
		                        IPlayerInputService playerInputService)
		{
			NetworkService = networkService;
			AnalyticsService = new AnalyticsService(this, gameLogic, dataProvider);
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataSaver;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = genericDialogService;
			AudioFxService = audioFxService;
			PlayerInputService = playerInputService;
			VfxService = vfxService;

			ThreadService = new ThreadService();
			HelpdeskService = new HelpdeskService();
			GuidService = new GuidService();
			PlayfabService = new StubPlayfabService();
			CommandService = new StubCommandService(gameLogic, dataProvider);
			PoolService = new PoolService();
			TickService = new StubTickService();
			CoroutineService = new StubCoroutineService();
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			NotificationService = new StubNotificationService();
		}
	}
}