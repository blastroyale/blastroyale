using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using FirstLight.SDK.Services;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Photon.Deterministic.Protocol;

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
		public IBattlePassDataProvider BattlePassDataProvider => BattlePassLogic;

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
		}
		
		/// <summary>
		/// Initializes the local-only Game Logic state to it's default values
		/// </summary>
		public void InitLocal()
		{
			// ReSharper disable PossibleNullReferenceException
			
			// AppLogic is initialized separately, earlier than rest of logic which requires data after auth
			(AppLogic as IGameLogicInitializer).Init();
		}

		/// <inheritdoc />
		public void Init()
		{
			// ReSharper disable PossibleNullReferenceException
			AppLogic.Init();
			(UniqueIdLogic as IGameLogicInitializer).Init();
			(CurrencyLogic as IGameLogicInitializer).Init();
			(ResourceLogic as IGameLogicInitializer).Init();
			(PlayerLogic as IGameLogicInitializer).Init();
			(EquipmentLogic as IGameLogicInitializer).Init();
			(RewardLogic as IGameLogicInitializer).Init();
			(BattlePassLogic as IGameLogicInitializer).Init();
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
			return container;
		}
		
		public static IMessageBrokerService MessageBrokerService(this ServiceContainer c)
		{
			return c.Get<IMessageBrokerService>();
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
			return container;
		}

		public static IRewardLogic RewardLogic(this LogicContainer c) => c.Get<IRewardLogic>();
		public static ICurrencyLogic CurrencyLogic(this LogicContainer c) => c.Get<ICurrencyLogic>();
		public static IResourceLogic ResourceLogic(this LogicContainer c) => c.Get<IResourceLogic>();
		public static IPlayerLogic PlayerLogic(this LogicContainer c) => c.Get<IPlayerLogic>();
		public static IBattlePassLogic BattlePassLogic(this LogicContainer c) => c.Get<IBattlePassLogic>();
		public static IEquipmentLogic EquipmentLogic(this LogicContainer c) => c.Get<IEquipmentLogic>();
		public static IUniqueIdLogic UniqueIdLogic(this LogicContainer c) => c.Get<IUniqueIdLogic>();
	}

}