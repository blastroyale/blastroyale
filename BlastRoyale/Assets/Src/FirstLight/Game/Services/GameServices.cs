using Cysharp.Threading.Tasks;
using FirstLight.Game.Logic;
using FirstLight.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services.Analytics;
using FirstLight.Game.Services.Party;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Services.Collection;
using FirstLight.Game.Services.Social;
using FirstLight.Game.Utils;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using Unity.Services.RemoteConfig;

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

		public RateAndReviewService RateAndReviewService { get; }

		/// <inheritdoc cref="IPlayfabPubSubService"/>
		public IPlayfabPubSubService PlayfabPubSubService { get; }

		public UIService.UIService UIService { get; }
		public UIVFXService UIVFXService { get; }

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
		public LocalPrefsService LocalPrefsService { get; }
		public IFLLobbyService FLLobbyService { get; }
		public InGameNotificationService InGameNotificationService { get; }
		public DeepLinkService DeepLinkService { get; }
		public IGameSocialService GameSocialService { get; }
		public IPlayfabUnityBridgeService PlayfabUnityBridgeService { get; }
		public INotificationService NotificationService { get; }
		public IBuffService BuffService { get; }

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
		public IRemoteTextureService RemoteTextureService { get; }
		public IThreadService ThreadService { get; }
		public ICustomerSupportService CustomerSupportService { get; }
		public IGameModeService GameModeService { get; }
		public IMatchmakingService MatchmakingService { get; }
		public IIAPService IAPService { get; }
		public RateAndReviewService RateAndReviewService { get; }
		public IPlayfabPubSubService PlayfabPubSubService { get; }
		public UIService.UIService UIService { get; }
		public UIVFXService UIVFXService { get; }
		public ICollectionService CollectionService { get; }
		public IControlSetupService ControlsSetup { get; }
		public IRoomService RoomService { get; }
		public IBattlePassService BattlePassService { get; }
		public ITeamService TeamService { get; }
		public IServerListService ServerListService { get; }
		public INewsService NewsService { get; }
		public ILeaderboardService LeaderboardService { get; }
		public IRewardService RewardService { get; }
		public LocalPrefsService LocalPrefsService { get; }
		public IFLLobbyService FLLobbyService { get; }
		public InGameNotificationService InGameNotificationService { get; }
		public DeepLinkService DeepLinkService { get; }
		public IGameSocialService GameSocialService { get; }

		public IPlayfabUnityBridgeService PlayfabUnityBridgeService { get; }
		public INotificationService NotificationService { get; }
		public IBuffService BuffService { get; }

		public string QuitReason { get; set; }

		public GameServices(IInternalGameNetworkService networkService, IMessageBrokerService messageBrokerService,
							ITimeService timeService, IDataService dataService, IConfigsAdder configsProvider,
							IGameLogic gameLogic, IAssetResolverService assetResolverService)
		{
			NetworkService = networkService;
			MessageBrokerService = messageBrokerService;
			TimeService = timeService;
			DataSaver = dataService;
			DataService = dataService;
			ConfigsProvider = configsProvider;
			AssetResolverService = assetResolverService;
			LocalPrefsService = new LocalPrefsService();
			AudioFxService = new GameAudioFxService(assetResolverService, LocalPrefsService);
			VfxService = new VfxService<VfxId>();

			UIService = new UIService.UIService();
			GenericDialogService = new GenericDialogService(UIService, gameLogic.CurrencyDataProvider);
			UIVFXService = new UIVFXService(this, assetResolverService);
			UIVFXService.Init().Forget();

			DeepLinkService = new DeepLinkService(MessageBrokerService, UIService);

			InGameNotificationService = new InGameNotificationService(UIService);

			AnalyticsService = new AnalyticsService(this, gameLogic, UIService);

			ThreadService = new ThreadService();
			CoroutineService = new CoroutineService();
			GuidService = new GuidService();
			PlayfabPubSubService = new PlayfabPubSubService(MessageBrokerService);
			GameBackendService =
				new GameBackendService(messageBrokerService, gameLogic, this, dataService, GameConstants.Stats.RANKED_LEADERBOARD_LADDER_NAME);
			ProfileService = new PlayerProfileService(GameBackendService);
			AuthenticationService = new PlayfabAuthenticationService((IGameLogicInitializer) gameLogic, this, dataService, networkService, gameLogic,
				configsProvider);
			FLLobbyService = new FLLobbyService(MessageBrokerService, gameLogic, InGameNotificationService, LocalPrefsService);
			RemoteTextureService = new RemoteTextureService(CoroutineService, ThreadService);
			RateAndReviewService = new RateAndReviewService(MessageBrokerService, LocalPrefsService);
			GameModeService = new GameModeService(ConfigsProvider, FLLobbyService, gameLogic.AppDataProvider, LocalPrefsService, RemoteTextureService, MessageBrokerService);
			CommandService = new GameCommandService(GameBackendService, gameLogic, dataService, this);
			PoolService = new PoolService();
			BuffService = new BuffService(this, gameLogic);
			RewardService = new RewardService(this, gameLogic);
			TickService = new TickService();
			LeaderboardService = new LeaderboardsService(this);
			ControlsSetup = new ControlSetupService();
			MatchmakingService = new PlayfabMatchmakingService(gameLogic, CoroutineService, FLLobbyService, MessageBrokerService, NetworkService,
				GameBackendService, ConfigsProvider, LocalPrefsService, GameModeService);
			NewsService = new PlayfabNewsService(MessageBrokerService);
			IAPService = new IAPService(CommandService, MessageBrokerService, GameBackendService, AnalyticsService, gameLogic);

			RoomService = new RoomService.RoomService(NetworkService, GameBackendService, ConfigsProvider, CoroutineService, gameLogic,
				LeaderboardService);
			TutorialService = new TutorialService(RoomService, CommandService, ConfigsProvider, gameLogic);
			CollectionService = new CollectionService(AssetResolverService, ConfigsProvider, MessageBrokerService, gameLogic, CommandService);
			BattlePassService = new BattlePassService(MessageBrokerService, gameLogic, this);
			GameAppService = new GameAppService(this);
			TeamService = new TeamService(RoomService);
			ServerListService = new ServerListService(ThreadService, CoroutineService, GameBackendService, MessageBrokerService);
			CustomerSupportService = new CustomerSupportService(AuthenticationService);
			GameSocialService = new GameSocialService(this, gameLogic);
			PlayfabUnityBridgeService = new PlayfabUnityBridgeService(ProfileService, MessageBrokerService);
			NotificationService = new NotificationService(gameLogic.RemoteConfigProvider, MessageBrokerService);
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