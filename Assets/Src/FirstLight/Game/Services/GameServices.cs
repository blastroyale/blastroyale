using System.Collections.Generic;
using FirstLight.Game.Logic;
using FirstLight.GoogleSheetImporter;
using FirstLight.Services;
using FirstLight;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.NotificationService;
using PlayFab;
using UnityEngine.InputSystem.UI;

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
		/// <inheritdoc cref="IEntityViewUpdaterService"/>
		IEntityViewUpdaterService EntityViewUpdaterService { get; }
		/// <inheritdoc cref="IGuidService"/>
		IGuidService GuidService { get; }

		/// <inheritdoc cref="IStoreService"/>
		IStoreService StoreService { get; }
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
	}

	/// <inheritdoc />
	public class GameServices : IGameServices
	{
		/// <inheritdoc />
		public IDataSaver DataSaver { get; }
		/// <inheritdoc />
		public IConfigsProvider ConfigsProvider { get; }
		/// <inheritdoc />
		public IEntityViewUpdaterService EntityViewUpdaterService { get; }
		/// <inheritdoc />
		public IGuidService GuidService { get; }

		/// <inheritdoc />
		public IStoreService StoreService { get; }
		/// <inheritdoc />
		public IGameNetworkService NetworkService { get; }
		/// <inheritdoc />
		public IMessageBrokerService MessageBrokerService { get; }
		/// <inheritdoc />
		public IGameCommandService CommandService { get; }
		/// <inheritdoc />
		public IPoolService PoolService { get; }
		/// <inheritdoc />
		public ITickService TickService { get; }
		/// <inheritdoc />
		public ITimeService TimeService { get; }
		/// <inheritdoc />
		public ICoroutineService CoroutineService { get; }
		/// <inheritdoc />
		public IAssetResolverService AssetResolverService { get; }
		/// <inheritdoc />
		public IAnalyticsService AnalyticsService { get; }
		/// <inheritdoc />
		public IGenericDialogService GenericDialogService { get; }
		/// <inheritdoc />
		public IVfxService<VfxId> VfxService { get; }
		/// <inheritdoc />
		public IAudioFxService<AudioId> AudioFxService { get; }
		/// <inheritdoc />
		public INotificationService NotificationService { get; }
		
		public GameServices(IGameNetworkService networkService, IMessageBrokerService messageBrokerService, 
		                    ITimeService timeService, IDataSaver dataSaver, IConfigsProvider configsProvider,
		                    IGameLogic gameLogic, IDataProvider dataProvider, IGenericDialogService genericDialogService, 
		                    IEntityViewUpdaterService entityViewUpdaterService, IAssetResolverService assetResolverService, 
		                    IAnalyticsService analyticsService, IVfxService<VfxId> vfxService, IAudioFxService<AudioId> audioFxService)
		{
			EntityViewUpdaterService = entityViewUpdaterService;
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

			GuidService = new GuidService();
			CommandService = new GameCommandService(gameLogic, dataProvider);
			PoolService = new PoolService();
			TickService =  new TickService();
			CoroutineService = new CoroutineService();
			StoreService = new StoreService(CommandService);
			NotificationService = new MobileNotificationService(
				new GameNotificationChannel(GameConstants.NotificationBoxesChannel, GameConstants.NotificationBoxesChannel,GameConstants.NotificationBoxesChannel),
				new GameNotificationChannel(GameConstants.NotificationIdleBoxesChannel, GameConstants.NotificationIdleBoxesChannel,GameConstants.NotificationIdleBoxesChannel));
		}
	}
}