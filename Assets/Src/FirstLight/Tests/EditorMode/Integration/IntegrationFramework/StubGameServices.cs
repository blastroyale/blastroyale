using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Services.RoomService;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.UIService;
using FirstLightServerSDK.Modules.RemoteCollection;
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
		public virtual IVfxService<VfxId> VfxService { get; }
		public virtual IAudioFxService<AudioId> AudioFxService { get; }
		public virtual IGameBackendService GameBackendService { get; }
		public virtual IPlayerProfileService ProfileService { get; }
		public virtual IAuthenticationService AuthenticationService { get; set; }
		public virtual ITutorialService TutorialService { get; }
		public virtual ILiveopsService LiveopsService { get; set; }
		public virtual IRemoteTextureService RemoteTextureService { get; }
		public virtual IThreadService ThreadService { get; }
		public virtual ICustomerSupportService CustomerSupportService { get; }
		public virtual IGameModeService GameModeService { get; }
		public virtual IMatchmakingService MatchmakingService { get; }
		public virtual IIAPService IAPService { get; }
		public virtual IPartyService PartyService { get; }
		public virtual IPlayfabPubSubService PlayfabPubSubService { get; }
		public UIService2 UIService { get; }
		public ICollectionEnrichmentService CollectionEnrichnmentService { get; }
		public ICollectionService CollectionService { get; }
		public IControlSetupService ControlsSetup { get; set; }
		public IRoomService RoomService { get; }
		public IGameAppService GameAppService { get; set; }

		public IBattlePassService BattlePassService { get; }
		public ITeamService TeamService { get; }
		public IServerListService ServerListService { get; }
		public INewsService NewsService { get; set; }
		public ILeaderboardService LeaderboardService { get; set; }
		public IRewardService RewardService { get; set; }
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
								IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService)
		{
			NetworkService = networkService;
			MessageBrokerService = messageBrokerService;
			AnalyticsService = new AnalyticsService(this, gameLogic);
			TimeService = timeService;
			DataSaver = dataService;
			DataService = dataService;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			GenericDialogService = genericDialogService;
			TutorialService = tutorialService;
			AudioFxService = audioFxService;
			VfxService = vfxService;
			GameLogic = gameLogic;

			ThreadService = new ThreadService();
			
			PartyService = Substitute.For<IPartyService>();
			GameModeService = new GameModeService(ConfigsProvider, ThreadService, gameLogic,
				PartyService, gameLogic.AppDataProvider);
		
			GuidService = new GuidService();
			GameBackendService = new StubGameBackendService();
			ProfileService = new PlayerProfileService(GameBackendService);
			AuthenticationService = new PlayfabAuthenticationService((IGameLogicInitializer) gameLogic, this,
				dataService, networkService, gameLogic, (IConfigsAdder) configsProvider);
			CommandService = new StubCommandService(gameLogic, dataProvider, this);
			PoolService = new PoolService();
			TickService = new StubTickService();
			CoroutineService = new StubCoroutineService();
			MatchmakingService = new PlayfabMatchmakingService(gameLogic, CoroutineService, PartyService,
				MessageBrokerService, NetworkService, GameBackendService,ConfigsProvider);
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			PlayfabPubSubService = Substitute.For<IPlayfabPubSubService>();
			RoomService = Substitute.For<IRoomService>();
			CollectionService = Substitute.For<ICollectionService>();
			NewsService = Substitute.For<INewsService>();
			BattlePassService = Substitute.For<IBattlePassService>();
			GameAppService = Substitute.For<IGameAppService>();
			TeamService = Substitute.For<ITeamService>();
			ServerListService = Substitute.For<IServerListService>();
			IAPService = Substitute.For<IIAPService>();
			CustomerSupportService = new CustomerSupportService(AuthenticationService);
		}
	}
}