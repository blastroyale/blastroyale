using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Services;
using NSubstitute;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Quantum.Allocator;
using Quantum.Core;

namespace FirstLight.Tests.EditorMode
{
	/// <inheritdoc />
	/// <remarks>
	/// Helper class with a reference <seealso cref="TestData"/> of <typeparamref name="T"/> type to test Game Logic
	/// </remarks>
	public abstract class BaseTestFixture<T> : BaseTestFixture where T : class
	{
		protected T TestData;

		[SetUp]
		public void InitData()
		{
			TestData = Activator.CreateInstance<T>();
			DataService.GetData<T>().Returns(x => TestData);
		}
	}

	/// <summary>
	/// Helper base class for unit tests to mock dependencies.
	/// All tests should inherit from this class to reduce code duplications
	/// </summary>
	public abstract class BaseTestFixture
	{
		// Services
		protected IGameServices GameServices;
		protected IDataService DataService;
		protected IGameBackendNetworkService NetworkService;
		protected IMessageBrokerService MessageBrokerService;
		protected IGameCommandService CommandService;
		protected IPoolService PoolService;
		protected ITickService TickService;
		protected ITimeService TimeService;
		protected ICoroutineService CoroutineService;
		protected IAssetResolverService AssetResolverService;
		protected IAudioFxService<AudioId> AudioFxService;
		protected IConfigsProvider ConfigsProvider;

		// Logic
		protected IGameLogic GameLogic;
		protected IAppLogic AppLogic;
		protected IUniqueIdLogic UniqueIdLogic;
		protected IRngLogic RngLogic;
		protected ICurrencyLogic CurrencyLogic;
		protected IPlayerLogic PlayerLogic;
		protected IEquipmentLogic EquipmentLogic;
		protected IRewardLogic RewardLogic;

		[SetUp]
		public void InitMocks()
		{
			// Services
			GameServices = Substitute.For<IGameServices>();
			DataService = Substitute.For<IDataService>();
			NetworkService = Substitute.For<IGameBackendNetworkService>();
			MessageBrokerService = Substitute.For<IMessageBrokerService>();
			CommandService = Substitute.For<IGameCommandService>();
			PoolService = Substitute.For<IPoolService>();
			TickService = Substitute.For<ITickService>();
			TimeService = Substitute.For<ITimeService>();
			CoroutineService = Substitute.For<ICoroutineService>();
			AssetResolverService = Substitute.For<IAssetResolverService>();
			AudioFxService = Substitute.For<IAudioFxService<AudioId>>();
			ConfigsProvider = Substitute.For<IConfigsProvider>();

			// Services
			GameLogic = Substitute.For<IGameLogic>();
			AppLogic = Substitute.For<IAppLogic>();
			UniqueIdLogic = Substitute.For<IUniqueIdLogic>();
			RngLogic = Substitute.For<IRngLogic>();
			CurrencyLogic = Substitute.For<ICurrencyLogic>();
			PlayerLogic = Substitute.For<IPlayerLogic>();
			EquipmentLogic = Substitute.For<IEquipmentLogic>();
			RewardLogic = Substitute.For<IRewardLogic>();

			// Returns
			GameLogic.AppLogic.Returns(AppLogic);
			GameLogic.UniqueIdLogic.Returns(UniqueIdLogic);
			GameLogic.RngLogic.Returns(RngLogic);
			GameLogic.CurrencyLogic.Returns(CurrencyLogic);
			GameLogic.PlayerLogic.Returns(PlayerLogic);
			GameLogic.EquipmentLogic.Returns(EquipmentLogic);
			GameLogic.RewardLogic.Returns(RewardLogic);

			GameLogic.MessageBrokerService.Returns(MessageBrokerService);
			GameLogic.TimeService.Returns(TimeService);
			GameLogic.ConfigsProvider.Returns(ConfigsProvider);

			GameServices.AssetResolverService.Returns(AssetResolverService);
			GameServices.CommandService.Returns(CommandService);
			GameServices.CoroutineService.Returns(CoroutineService);
			GameServices.DataSaver.Returns(DataService);
			GameServices.MessageBrokerService.Returns(MessageBrokerService);
			GameServices.NetworkService.Returns(NetworkService);
			GameServices.PoolService.Returns(PoolService);
			GameServices.TickService.Returns(TickService);
			GameServices.TimeService.Returns(TimeService);
			GameServices.ConfigsProvider.Returns(ConfigsProvider);

			TimeService.DateTimeUtcNow.Returns(DateTime.UtcNow);
		}

		protected void InitConfigData<T>(Func<T, int> idResolver, params T[] data)
		{
			var list = new List<T>();
			var dictionary = new Dictionary<int, T>();

			foreach (var value in data)
			{
				list.Add(value);
				dictionary.Add(idResolver(value), value);
				ConfigsProvider.GetConfig<T>(idResolver(value)).Returns(value);
			}

			ConfigsProvider.GetConfig<T>().Returns(data[0]);
			ConfigsProvider.GetConfigsList<T>().Returns(list);
			ConfigsProvider.GetConfigsDictionary<T>().Returns(dictionary);
		}

		protected void InitConfigData<T>(T data)
		{
			var list = new List<T> {data};
			var dictionary = new Dictionary<int, T> {{Arg.Any<int>(), data}};

			ConfigsProvider.GetConfig<T>(Arg.Any<int>()).Returns(data);
			ConfigsProvider.GetConfig<T>().Returns(data);
			ConfigsProvider.GetConfigsList<T>().Returns(list);
			ConfigsProvider.GetConfigsDictionary<T>().Returns(dictionary);
		}
	}


	/// <inheritdoc cref="BaseTestFixture"/>
	/// <remarks>
	/// Helper class with a reference <seealso cref="TestData"/> of unsafe <typeparamref name="T"/> type to test Quantum's Systems
	/// </remarks>
	public unsafe abstract class UnsafeBaseTestFixture<T> where T : unmanaged
	{
		protected T TestData;
		protected Frame Frame;
		protected FP DeltaTime;

		[SetUp]
		public void InitData()
		{
			var platformInfo = new DeterministicPlatformInfo
			{
				Allocator = new QuantumUnityNativeAllocator()
			};
			var args = new FrameContext.Args
			{
				AssetDatabase = UnityDB.DefaultResourceManager.CreateAssetDatabase(),
				AssetSerializer = null,
				CommandSerializer = null,
				HeapConfig = Heap.Config.Default(4),
				IsLocalPlayer = null,
				NavigationConfig = null,
				PhysicsConfig = null,
				PlatformInfo = platformInfo,
				UseCullingArea = false,
				UseNavigation = false,
				UsePhysics2D = false,
				UsePhysics3D = false
			};

			DeltaTime = FP._0_10;
			Frame = new Frame(new FrameContext(args), null, null, null, null, null, DeltaTime);
		}
	}
}