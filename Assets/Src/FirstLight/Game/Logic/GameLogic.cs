using System.Collections.Generic;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLightServerSDK.Services;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This interface marks the Game Logic as one that needs to initialize it's internal state
	/// </summary>
	public interface IGameLogicInitializer
	{
		/// <summary>
		/// Initializes the Game Logic states to its default values
		/// </summary>
		void Init();

		/// <summary>
		/// Reinitializes the Game Logic states to its default values, and copies over any relevant values that would be
		/// otherwise lost by doing a simple init. E.g. copying over observable listeners from already initialized
		/// observable fields
		/// </summary>
		void ReInit();
	}

	/// <summary>
	/// Provides access to all game's data.
	/// This interface provides the data with view only permissions
	/// </summary>
	public interface IGameDataProvider
	{
		/// <inheritdoc cref="IAppDataProvider"/>
		IAppDataProvider AppDataProvider { get; }

		/// <inheritdoc cref="IUniqueIdDataProvider"/>
		IUniqueIdDataProvider UniqueIdDataProvider { get; }

		/// <inheritdoc cref="IRngDataProvider"/>
		IRngDataProvider RngDataProvider { get; }

		/// <inheritdoc cref="ICurrencyDataProvider"/>
		ICurrencyDataProvider CurrencyDataProvider { get; }

		/// <inheritdoc cref="IResourceDataProvider"/>
		IResourceDataProvider ResourceDataProvider { get; }

		/// <inheritdoc cref="IPlayerDataProvider"/>
		IPlayerDataProvider PlayerDataProvider { get; }

		/// <inheritdoc cref="IEquipmentDataProvider"/>
		IEquipmentDataProvider EquipmentDataProvider { get; }

		/// <inheritdoc cref="IRewardDataProvider"/>
		IRewardDataProvider RewardDataProvider { get; }

		/// <inheritdoc cref="IBattlePassDataProvider"/>
		IBattlePassDataProvider BattlePassDataProvider { get; }

		ICollectionDataProvider CollectionDataProvider { get; }
	}

	/// <summary>
	/// Provides access to all game's logic
	/// This interface shouldn't be exposed to the views or controllers
	/// To interact with the logic, execute a <see cref="IGameCommand"/> via the <see cref="ICommandService{TGameLogic}"/>
	/// </summary>
	public interface IGameLogic : IGameDataProvider
	{
		/// <inheritdoc cref="IMessageBrokerService"/>
		IMessageBrokerService MessageBrokerService { get; }

		/// <inheritdoc cref="ITimeService"/>
		ITimeService TimeService { get; }

		/// <inheritdoc cref="IConfigsProvider"/>
		IConfigsProvider ConfigsProvider { get; }

		/// <inheritdoc cref="IAppLogic"/>
		IAppLogic AppLogic { get; }

		/// <inheritdoc cref="IUniqueIdLogic"/>
		IUniqueIdLogic UniqueIdLogic { get; }

		/// <inheritdoc cref="IRngLogic"/>
		IRngLogic RngLogic { get; }

		/// <inheritdoc cref="ICurrencyLogic"/>
		ICurrencyLogic CurrencyLogic { get; }

		/// <inheritdoc cref="IResourceLogic"/>
		IResourceLogic ResourceLogic { get; }

		/// <inheritdoc cref="IPlayerLogic"/>
		IPlayerLogic PlayerLogic { get; }

		/// <inheritdoc cref="IEquipmentLogic"/>
		IEquipmentLogic EquipmentLogic { get; }

		/// <inheritdoc cref="IRewardLogic"/>
		IRewardLogic RewardLogic { get; }

		/// <inheritdoc cref="IBattlePassLogic"/>
		IBattlePassLogic BattlePassLogic { get; }

		/// <inheritdoc cref="ILiveopsLogic"/>
		ILiveopsLogic LiveopsLogic { get; }

		ICollectionLogic CollectionLogic { get; }
	}

	/// <inheritdoc cref="IGameLogic"/>
	public class GameLogic : IGameLogic, IGameLogicInitializer
	{
		private List<IGameLogicInitializer> _logicInitializers;

		/// <inheritdoc />
		public IMessageBrokerService MessageBrokerService { get; }

		/// <inheritdoc />
		public ITimeService TimeService { get; }

		/// <inheritdoc />
		public IAnalyticsService AnalyticsService { get; }

		/// <inheritdoc />
		public IConfigsProvider ConfigsProvider { get; }

		/// <inheritdoc />
		public IAppDataProvider AppDataProvider => AppLogic;

		/// <inheritdoc />
		public IUniqueIdDataProvider UniqueIdDataProvider => UniqueIdLogic;

		/// <inheritdoc />
		public IRngDataProvider RngDataProvider => RngLogic;

		/// <inheritdoc />
		public ICurrencyDataProvider CurrencyDataProvider => CurrencyLogic;

		/// <inheritdoc />
		public IResourceDataProvider ResourceDataProvider => ResourceLogic;

		/// <inheritdoc />
		public IPlayerDataProvider PlayerDataProvider => PlayerLogic;

		/// <inheritdoc />
		public IEquipmentDataProvider EquipmentDataProvider => EquipmentLogic;

		/// <inheritdoc />
		public IRewardDataProvider RewardDataProvider => RewardLogic;

		/// <inheritdoc />
		public IBattlePassDataProvider BattlePassDataProvider => BattlePassLogic;

		/// <inheritdoc />
		public ILiveopsDataProvider LiveopsDataProvider => LiveopsLogic;

		public ICollectionDataProvider CollectionDataProvider => CollectionLogic;

		/// <inheritdoc />
		public IAppLogic AppLogic { get; }

		/// <inheritdoc />
		public IUniqueIdLogic UniqueIdLogic { get; }

		/// <inheritdoc />
		public IRngLogic RngLogic { get; }

		/// <inheritdoc />
		public ICurrencyLogic CurrencyLogic { get; }

		/// <inheritdoc />
		public IResourceLogic ResourceLogic { get; }

		/// <inheritdoc />
		public IPlayerLogic PlayerLogic { get; }

		/// <inheritdoc />
		public IEquipmentLogic EquipmentLogic { get; }

		/// <inheritdoc />
		public IRewardLogic RewardLogic { get; }

		/// <inheritdoc />
		public IBattlePassLogic BattlePassLogic { get; }

		/// <inheritdoc />
		public ILiveopsLogic LiveopsLogic { get; }

		public ICollectionLogic CollectionLogic { get; }

		public GameLogic(IMessageBrokerService messageBroker, ITimeService timeService, IDataProvider dataProvider,
						 IConfigsProvider configsProvider, IAudioFxService<AudioId> audioFxService)
		{
			MessageBrokerService = messageBroker;
			TimeService = timeService;
			ConfigsProvider = configsProvider;

			AppLogic = new AppLogic(this, dataProvider, audioFxService);
			UniqueIdLogic = new UniqueIdLogic(this, dataProvider);
			RngLogic = new RngLogic(this, dataProvider);
			CurrencyLogic = new CurrencyLogic(this, dataProvider);
			ResourceLogic = new ResourceLogic(this, dataProvider);
			PlayerLogic = new PlayerLogic(this, dataProvider);
			EquipmentLogic = new EquipmentLogic(this, dataProvider);
			RewardLogic = new RewardLogic(this, dataProvider);
			BattlePassLogic = new BattlePassLogic(this, dataProvider);
			LiveopsLogic = new LiveopsLogic(this, dataProvider);
			CollectionLogic = new CollectionLogic(this, dataProvider);
			_logicInitializers = new List<IGameLogicInitializer>();

			_logicInitializers.Add(UniqueIdLogic as IGameLogicInitializer);
			_logicInitializers.Add(CurrencyLogic as IGameLogicInitializer);
			_logicInitializers.Add(ResourceLogic as IGameLogicInitializer);
			_logicInitializers.Add(EquipmentLogic as IGameLogicInitializer);
			_logicInitializers.Add(PlayerLogic as IGameLogicInitializer);
			_logicInitializers.Add(RewardLogic as IGameLogicInitializer);
			_logicInitializers.Add(BattlePassLogic as IGameLogicInitializer);
			_logicInitializers.Add(LiveopsLogic as IGameLogicInitializer);
			_logicInitializers.Add(CollectionLogic as IGameLogicInitializer);
		}

		/// <inheritdoc />
		public void Init()
		{
			foreach (var logicInitializer in _logicInitializers)
			{
				logicInitializer.Init();
			}
		}

		public void ReInit()
		{
			foreach (var logicInitializer in _logicInitializers)
			{
				logicInitializer.ReInit();
			}

			MessageBrokerService.Publish(new DataReinitializedMessage());
		}
	}


	/// <summary>
	/// Exposes blast royale typed services into generic services container
	/// </summary>
	public static class BlastRoyaleServicesContainer
	{
		public static ServiceContainer Build(this ServiceContainer container, IGameServices services)
		{
			container.Add(services.MessageBrokerService);

			var store = services.IAPService;
			container.Add(store.RemoteCatalogStore as IItemCatalog<ItemData>);
			container.Add(store.RemoteCatalogStore);
			return container;
		}

		public static IMessageBrokerService MessageBrokerService(this ServiceContainer c)
		{
			return c.Get<IMessageBrokerService>();
		}
		
		public static IItemCatalog<ItemData> CatalogService(this ServiceContainer c)
		{
			return c.Get<IItemCatalog<ItemData>>();
		}
		
		public static IStoreService StoreService(this ServiceContainer c)
		{
			return c.Get<IStoreService>();
		}
	}

	/// <summary>
	/// Exposes blast royale typed logic objects from generic container
	/// </summary>
	public static class BlastRoyaleLogicContainer
	{
		public static LogicContainer Build(this LogicContainer container, IGameLogic logic)
		{
			container.Add(logic.RewardLogic);
			container.Add(logic.CurrencyLogic);
			container.Add(logic.ResourceLogic);
			container.Add(logic.PlayerLogic);
			container.Add(logic.BattlePassLogic);
			container.Add(logic.EquipmentLogic);
			container.Add(logic.UniqueIdLogic);
			container.Add(logic.LiveopsLogic);
			container.Add(logic.CollectionLogic);
			container.Add(logic.RngLogic);
			return container;
		}

		public static IRewardLogic RewardLogic(this LogicContainer c) => c.Get<IRewardLogic>();
		public static ICurrencyLogic CurrencyLogic(this LogicContainer c) => c.Get<ICurrencyLogic>();
		public static IResourceLogic ResourceLogic(this LogicContainer c) => c.Get<IResourceLogic>();
		public static IPlayerLogic PlayerLogic(this LogicContainer c) => c.Get<IPlayerLogic>();
		public static IBattlePassLogic BattlePassLogic(this LogicContainer c) => c.Get<IBattlePassLogic>();
		public static IEquipmentLogic EquipmentLogic(this LogicContainer c) => c.Get<IEquipmentLogic>();
		public static IUniqueIdLogic UniqueIdLogic(this LogicContainer c) => c.Get<IUniqueIdLogic>();
		public static ILiveopsLogic LiveopsLogic(this LogicContainer c) => c.Get<ILiveopsLogic>();
		public static ICollectionLogic CollectionLogic(this LogicContainer c) => c.Get<ICollectionLogic>();
		public static IRngLogic RngLogic(this LogicContainer c) => c.Get<IRngLogic>();
	}
}