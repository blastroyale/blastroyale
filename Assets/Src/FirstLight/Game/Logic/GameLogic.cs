using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;

namespace FirstLight.Game.Logic
{
	/// <summary>
	/// This interface marks the Game Logic as one that needs to initialize it's internal state
	/// </summary>
	public interface IGameLogicInitializer
	{
		/// <summary>
		/// Initializes the Game Logic state to it's default initial values
		/// </summary>
		void Init();
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
		/// <inheritdoc cref="IRewardDataProvider"/>
		IRewardLogic RewardLogic { get; }
	}

	/// <inheritdoc cref="IGameLogic"/>
	public class GameLogic : IGameLogic, IGameLogicInitializer
	{
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
		}

		/// <inheritdoc />
		public void Init()
		{
			// ReSharper disable PossibleNullReferenceException
			
			(AppLogic as IGameLogicInitializer).Init();
			(UniqueIdLogic as IGameLogicInitializer).Init();
			(CurrencyLogic as IGameLogicInitializer).Init();
			(ResourceLogic as IGameLogicInitializer).Init();
			(PlayerLogic as IGameLogicInitializer).Init();
			(EquipmentLogic as IGameLogicInitializer).Init();
			(RewardLogic as IGameLogicInitializer).Init();
		}
	}
}