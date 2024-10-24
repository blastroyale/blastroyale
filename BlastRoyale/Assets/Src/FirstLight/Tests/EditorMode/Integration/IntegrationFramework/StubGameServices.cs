using FirstLight.Game.Domains.VFX;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Analytics;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Services.Social;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UIService;
using NSubstitute;

namespace FirstLight.Tests.EditorMode
{
	public class StubGameServices : IGameServices
	{
		public virtual IDataSaver DataSaver { get; }
		public IDataService DataService { get; }
		public virtual IConfigsProvider ConfigsProvider { get; }
		public virtual IGuidService GuidService { get; }
		public virtual IGameNetworkService NetworkService { get; }
		public virtual IMessageBrokerService MessageBrokerService { get; }
		public virtual IGameCommandService CommandService { get; }
		public virtual IPoolService PoolService { get; }
		public virtual ITickService TickService { get; }
		public virtual ITimeService TimeService { get; }
		public virtual ICoroutineService CoroutineService { get; }
		public virtual IAssetResolverService AssetResolverService { get; }
		public virtual IAnalyticsService AnalyticsService { get; }
		public virtual IGenericDialogService GenericDialogService { get; }
		public virtual IAudioFxService<AudioId> AudioFxService { get; }
		public virtual IGameBackendService GameBackendService { get; }
		public virtual IPlayerProfileService ProfileService { get; }
		public virtual IAuthenticationService AuthenticationService { get; set; }
		public virtual ITutorialService TutorialService { get; }
		public virtual IRemoteTextureService RemoteTextureService { get; }
		public virtual IThreadService ThreadService { get; }
		public virtual ICustomerSupportService CustomerSupportService { get; }
		public virtual IGameModeService GameModeService { get; }
		public virtual IMatchmakingService MatchmakingService { get; }
		public virtual IIAPService IAPService { get; }
		public RateAndReviewService RateAndReviewService { get; }
		public virtual IPlayfabPubSubService PlayfabPubSubService { get; }
		public UIService.UIService UIService { get; }
		public UIVFXService UIVFXService { get; }
		public ICollectionService CollectionService { get; }
		public IControlSetupService ControlsSetup { get; set; }
		public IRoomService RoomService { get; }
		public IGameAppService GameAppService { get; set; }
		public IBattlePassService BattlePassService { get; }
		public ITeamService TeamService { get; }
		public IServerListService ServerListService { get; }
		public INewsService NewsService { get; set; }
		public LocalPrefsService LocalPrefsService { get; }
		public IFLLobbyService FLLobbyService { get; }
		public InGameNotificationService InGameNotificationService { get; }
		public DeepLinkService DeepLinkService { get; }
		public ILeaderboardService LeaderboardService { get; set; }
		public IRewardService RewardService { get; set; }
		public IGameSocialService GameSocialService { get; set; }
		public IPlayfabUnityBridgeService PlayfabUnityBridgeService { get; }

		public INotificationService NotificationService { get; }
		public IBuffService BuffService { get; set; }
		public virtual IGameLogic GameLogic { get; }
		public string QuitReason { get; set; }

		public void QuitGame(string reason)
		{
		}

		public StubGameServices(IInternalGameNetworkService networkService, IMessageBrokerService messageBrokerService,
								ITimeService timeService, IDataService dataService, IConfigsProvider configsProvider,
								IGameLogic gameLogic, IDataProvider dataProvider,
								IAssetResolverService assetResolverService, IInternalTutorialService tutorialService)
		{
			NetworkService = networkService;
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataService;
			DataService = dataService;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = new GenericDialogService(UIService, gameLogic.CurrencyDataProvider);
			TutorialService = tutorialService;
			GameLogic = gameLogic;
			LocalPrefsService = new LocalPrefsService();
			AudioFxService = new GameAudioFxService(assetResolverService, LocalPrefsService);
			ThreadService = new ThreadService();

			InGameNotificationService = new InGameNotificationService(UIService);

			DeepLinkService = new DeepLinkService(MessageBrokerService, UIService, gameLogic.RemoteConfigProvider);

			GuidService = new GuidService();
			GameBackendService = new StubGameBackendService();
			ProfileService = new PlayerProfileService(GameBackendService);
			AuthenticationService = new PlayfabAuthenticationService((IGameLogicInitializer) gameLogic, this,
				dataService, networkService, gameLogic, (IConfigsAdder) configsProvider);
			CommandService = new StubCommandService(gameLogic, dataProvider, this);
			PoolService = new PoolService();
			TickService = new StubTickService();
			FLLobbyService = new StubFLLobbyService();
			CoroutineService = new StubCoroutineService();
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			RateAndReviewService = new RateAndReviewService(MessageBrokerService, LocalPrefsService, gameLogic.RemoteConfigProvider);
			GameModeService = new GameModeService(ConfigsProvider, FLLobbyService, gameLogic.AppDataProvider, LocalPrefsService, RemoteTextureService,
				MessageBrokerService);
			MatchmakingService = new PlayfabMatchmakingService(gameLogic, CoroutineService, FLLobbyService, MessageBrokerService, NetworkService,
				GameBackendService, ConfigsProvider, LocalPrefsService, GameModeService);
			PlayfabPubSubService = Substitute.For<IPlayfabPubSubService>();
			RoomService = Substitute.For<IRoomService>();
			CollectionService = Substitute.For<ICollectionService>();
			NewsService = Substitute.For<INewsService>();
			BattlePassService = Substitute.For<IBattlePassService>();
			GameAppService = Substitute.For<IGameAppService>();
			TeamService = Substitute.For<ITeamService>();
			ServerListService = Substitute.For<IServerListService>();
			IAPService = Substitute.For<IIAPService>();

			GameSocialService = Substitute.For<IGameSocialService>();
		}
	}
}