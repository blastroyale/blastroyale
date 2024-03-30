using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.UIService;
using FirstLightServerSDK.Modules.RemoteCollection;

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

		IDataService DataService { get; }

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

		/// <inheritdoc cref="IGameBackendService"/>
		IGameBackendService GameBackendService { get; }

		/// <inheritdoc cref="IPlayerProfileService"/>
		IPlayerProfileService ProfileService { get; }

		/// <inheritdoc cref="IAuthenticationService"/>
		IAuthenticationService AuthenticationService { get; }

		/// <inheritdoc cref="ITutorialService"/>
		ITutorialService TutorialService { get; }

		/// <inheritdoc cref="IPlayfabService"/>
		ILiveopsService LiveopsService { get; }

		/// <inheritdoc cref="IRemoteTextureService"/>
		public IRemoteTextureService RemoteTextureService { get; }

		/// <inheritdoc cref="IThreadService"/>
		public IThreadService ThreadService { get; }

		/// <inheritdoc cref="ICustomerSupportService"/>
		public ICustomerSupportService CustomerSupportService { get; }

		/// <inheritdoc cref="IGameModeService"/>
		public IGameModeService GameModeService { get; }

		/// <inheritdoc cref="IMatchmakingService"/>
		public IMatchmakingService MatchmakingService { get; }

		/// <inheritdoc cref="IIAPService"/>
		public IIAPService IAPService { get; }

		/// <inheritdoc cref="IPartyService"/>
		public IPartyService PartyService { get; }

		/// <inheritdoc cref="IPlayfabPubSubService"/>
		public IPlayfabPubSubService PlayfabPubSubService { get; }
		
		public UIService.UIService UIService { get; }
		public UIVFXService UIVFXService { get; }

		public ICollectionEnrichmentService CollectionEnrichnmentService { get; }
		public ICollectionService CollectionService { get; }

		public IControlSetupService ControlsSetup { get; }
		public ILeaderboardService LeaderboardService { get; }
		public IRewardService RewardService { get; }
		public IRoomService RoomService { get; }
		public IGameAppService GameAppService { get; }
		public IBattlePassService BattlePassService { get; }
		public ITeamService TeamService { get; }
		public IServerListService ServerListService { get; }
		public INewsService NewsService { get; }

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
		public IDataService DataService { get; }
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
		public IGameBackendService GameBackendService { get; }
		public IGameAppService GameAppService { get; }
		public IPlayerProfileService ProfileService { get; }
		public IAuthenticationService AuthenticationService { get; }
		public ITutorialService TutorialService { get; }
		public ILiveopsService LiveopsService { get; }
		public IRemoteTextureService RemoteTextureService { get; }
		public IThreadService ThreadService { get; }
		public ICustomerSupportService CustomerSupportService { get; }
		public IGameModeService GameModeService { get; }
		public IMatchmakingService MatchmakingService { get; }
		public IIAPService IAPService { get; }
		public IPartyService PartyService { get; }
		public IPlayfabPubSubService PlayfabPubSubService { get; }
		public UIService.UIService UIService { get; }
		public UIVFXService UIVFXService { get; }
		public UIVFXService UiVfxService { get; }
		public ICollectionEnrichmentService CollectionEnrichnmentService { get; }
		public ICollectionService CollectionService { get; }
		public IControlSetupService ControlsSetup { get; }
		public IRoomService RoomService { get; }
		public IBattlePassService BattlePassService { get; }

		public ITeamService TeamService { get; }
		public IServerListService ServerListService { get; }
		public INewsService NewsService { get; }
		public ILeaderboardService LeaderboardService { get; }
		public ICheatsService CheatsService { get; }
		public IRewardService RewardService { get; }
		public string QuitReason { get; set; }


		public GameServices(IInternalGameNetworkService networkService, IMessageBrokerService messageBrokerService,
							ITimeService timeService, IDataService dataService, IConfigsAdder configsProvider,
							IGameLogic gameLogic, IGenericDialogService genericDialogService,
							IAssetResolverService assetResolverService, ITutorialService tutorialService,
							IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService, UIService.UIService uiService2)
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
			AudioFxService = audioFxService;
			VfxService = vfxService;
			TutorialService = tutorialService;

			ThreadService = new ThreadService();
			GuidService = new GuidService();
			PlayfabPubSubService = new PlayfabPubSubService(MessageBrokerService);
			GameBackendService =
				new GameBackendService(messageBrokerService, gameLogic, this, dataService, GameConstants.Stats.LEADERBOARD_LADDER_NAME);
			ProfileService = new PlayerProfileService(GameBackendService);
			AuthenticationService = new PlayfabAuthenticationService((IGameLogicInitializer) gameLogic, this, dataService, networkService, gameLogic,
				configsProvider);
			PartyService = new PartyService(PlayfabPubSubService, gameLogic.PlayerLogic, gameLogic.AppDataProvider, GameBackendService,
				GenericDialogService, MessageBrokerService);
			GameModeService = new GameModeService(ConfigsProvider, ThreadService, gameLogic, PartyService, gameLogic.AppDataProvider);
			LiveopsService = new LiveopsService(GameBackendService, ConfigsProvider, this, gameLogic.LiveopsLogic);
			CommandService = new GameCommandService(GameBackendService, gameLogic, dataService, this);
			PoolService = new PoolService();
			RewardService = new RewardService(this, gameLogic);
			TickService = new TickService();
			LeaderboardService = new LeaderboardsService(this);
			CoroutineService = new CoroutineService();
			ControlsSetup = new ControlSetupService();
			CollectionEnrichnmentService = new CollectionEnrichmentService(GameBackendService, gameLogic);
			MatchmakingService = new PlayfabMatchmakingService(gameLogic, CoroutineService, PartyService, MessageBrokerService, NetworkService,
				GameBackendService, ConfigsProvider);
			NewsService = new PlayfabNewsService(MessageBrokerService);
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			IAPService = new IAPService(CommandService, MessageBrokerService, GameBackendService, AnalyticsService, gameLogic);
			UIService = uiService2;
			UiVfxService = new UIVFXService(this, assetResolverService);
			UiVfxService.Init().Forget();

			var environmentService = new EnvironmentService(MessageBrokerService);
			CheatsService = new CheatsService(CommandService, GenericDialogService, environmentService, messageBrokerService, gameLogic,
				tutorialService);
			RoomService = new RoomService.RoomService(NetworkService, GameBackendService, ConfigsProvider, CoroutineService, gameLogic, LeaderboardService);
			CollectionService = new CollectionService(AssetResolverService, ConfigsProvider, MessageBrokerService, gameLogic, CommandService);
			BattlePassService = new BattlePassService(MessageBrokerService, gameLogic, this);
			GameAppService = new GameAppService(this);
			TeamService = new TeamService(RoomService);
			ServerListService = new ServerListService(ThreadService, CoroutineService, GameBackendService, MessageBrokerService);
			CustomerSupportService = new CustomerSupportService(AuthenticationService);
		}



		/// <inheritdoc />
		public void QuitGame(string reason)
		{
			MessageBrokerService.Publish(new ApplicationQuitMessage());
			QuitReason = reason;
#if UNITY_EDITOR
			UnityEditor.EditorApplication.isPlaying = false;
#else
				UnityEngine.Application.Quit(); // Apple does not allow to close the app so might not work on iOS :<
#endif
		}
	}
}