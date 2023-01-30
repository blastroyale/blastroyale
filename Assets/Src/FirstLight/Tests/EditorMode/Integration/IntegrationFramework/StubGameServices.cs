using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.NotificationService;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UiService;
using NSubstitute;

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
		public virtual IGameBackendService GameBackendService { get; }
		public virtual IAuthenticationService AuthenticationService { get; set; }
		public virtual ITutorialService TutorialService { get; }
		public virtual ILiveopsService LiveopsService { get; set; }
		public virtual IRemoteTextureService RemoteTextureService { get; }
		public virtual IThreadService ThreadService { get; }
		public virtual IHelpdeskService HelpdeskService { get; }
		public virtual IGameModeService GameModeService { get; }
		public virtual IMatchmakingService MatchmakingService { get; }
		public virtual IIAPService IAPService { get; }
		public virtual IPartyService PartyService { get; }
		public virtual IPlayfabPubSubService PlayfabPubSubService { get; }
		public virtual IGameLogic GameLogic { get; }
		public string QuitReason { get; set; }

		public void QuitGame(string reason)
		{
		}

		public StubGameServices(IInternalGameNetworkService networkService, IMessageBrokerService messageBrokerService,
		                        ITimeService timeService, IDataService dataService, IConfigsProvider configsProvider,
		                        IGameLogic gameLogic, IDataProvider dataProvider,
		                        IGenericDialogService genericDialogService,
		                        IAssetResolverService assetResolverService, IInternalTutorialService tutorialService,
		                        IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService,
		                        IPlayerInputService playerInputService, IGameUiService uiService)
		{
			NetworkService = networkService;
			AnalyticsService = new AnalyticsService(this, gameLogic, uiService);
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataService;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = genericDialogService;
			TutorialService = tutorialService;
			AudioFxService = audioFxService;
			PlayerInputService = playerInputService;
			VfxService = vfxService;
			GameLogic = gameLogic;
			
			ThreadService = new ThreadService();
			HelpdeskService = new HelpdeskService();
			GameModeService = new GameModeService(ConfigsProvider, ThreadService);
			IAPService = null;
			GuidService = new GuidService();
			GameBackendService = new StubGameBackendService();
			AuthenticationService = new PlayfabAuthenticationService(this, dataService, networkService, gameLogic, (IConfigsAdder)configsProvider);
			MatchmakingService = new PlayfabMatchmakingService(GameBackendService, CoroutineService);
			CommandService = new StubCommandService(gameLogic, dataProvider, this);
			PoolService = new PoolService();
			TickService = new StubTickService();
			CoroutineService = new StubCoroutineService();
			MatchmakingService = new PlayfabMatchmakingService(GameBackendService, CoroutineService);
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			NotificationService = Substitute.For<INotificationService>();
			PlayfabPubSubService = Substitute.For<PlayfabPubSubService>();
			PartyService = Substitute.For<PartyService>();
		}
	}
}