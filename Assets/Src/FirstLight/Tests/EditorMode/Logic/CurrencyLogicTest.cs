using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class CurrencyLogicTest : BaseTestFixture<PlayerData>
	{
		private ResourcePoolConfig _poolConfig;
		private CurrencyLogic _currencyLogic;

		[SetUp]
		public void Init()
		{
			_currencyLogic = new CurrencyLogic(GameLogic, DataService);
			_poolConfig = new ResourcePoolConfig
			{
				Id = GameId.CS,
				PoolCapacity = 1000,
				RestockIntervalMinutes = 100,
				TotalRestockIntervalMinutes = 1000,
				BaseMaxTake = 0,
				ScaleMultiplier = default,
				ShapeModifier = default,
				MaxPoolCapacityDecreaseModifier = default,
				PoolCapacityDecreaseExponent = default,
				MaxTakeDecreaseModifier = default,
				TakeDecreaseExponent = default,
				PoolCapacityTrophiesModifier = default
			};
			
			_currencyLogic.Init();
		}

		[Test]
		public void AddCurrencyCheck()
		{
			const int amount = 100;
			
			SetCurrencyData(new Pair<GameId, uint>(GameId.CS, 0));
			
			_currencyLogic.AddCurrency(GameId.CS, amount);
			
			Assert.AreEqual(amount, _currencyLogic.GetCurrencyAmount(GameId.CS));
		}

		[Test]
		public void DeductCurrencyCheck()
		{
			const int amount = 100;
			
			SetCurrencyData(new Pair<GameId, uint>(GameId.CS, amount));
			
			_currencyLogic.DeductCurrency(GameId.CS, amount);
			
			Assert.AreEqual(0, _currencyLogic.GetCurrencyAmount(GameId.CS));
		}

		[Test]
		public void DeductCurrency_InvalidAmount_ThrowsException()
		{
			const int amount = 100;
			
			SetCurrencyData(new Pair<GameId, uint>(GameId.CS, amount));
			
			Assert.Throws<LogicException>(() => _currencyLogic.DeductCurrency(GameId.CS, amount * 3));
		}

		[Test]
		public void InvalidCurrencyType_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _currencyLogic.AddCurrency(GameId.Random, 0));
			Assert.Throws<LogicException>(() => _currencyLogic.DeductCurrency(GameId.Random, 0));
			Assert.Throws<LogicException>(() => _currencyLogic.GetCurrencyAmount(GameId.Random));
		}

		[Test]
		public void WithdrawFromResourcePoolCheck()
		{
			var extraTime = 5;
			var poolData = new ResourcePoolData(_poolConfig.Id, 0,
			                                    DateTime.UtcNow.AddMinutes(-_poolConfig.RestockIntervalMinutes - extraTime));
			
			SetPoolData(poolData);
			
			var withdraw = _currencyLogic.WithdrawFromResourcePool(poolData.Id, 100);
			
			Assert.AreEqual(poolData.CurrentResourceAmountInPool, withdraw);
			Assert.AreEqual(0, _currencyLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.AreEqual(DateTime.UtcNow.AddMinutes(-extraTime), 
			                _currencyLogic.ResourcePools[poolData.Id].LastPoolRestockTime);
		}

		[Test]
		public void WithdrawFromResourcePool_EmptyPool_NothingHappens()
		{
			var poolData = new ResourcePoolData(_poolConfig.Id, 0, DateTime.UtcNow);
			
			SetPoolData(poolData);
			
			var withdraw = _currencyLogic.WithdrawFromResourcePool(poolData.Id, 100);
			
			Assert.AreEqual(0, withdraw);
			Assert.AreEqual(0, _currencyLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.AreEqual(DateTime.UtcNow, _currencyLogic.ResourcePools[poolData.Id].LastPoolRestockTime);
		}

		[Test]
		public void WithdrawFromResourcePool_OverflowWithdraw_WithdrawLeft()
		{
			const int widrawAmount = 200;
			
			var poolData = new ResourcePoolData(_poolConfig.Id, 100, DateTime.UtcNow);
			
			SetPoolData(poolData);
			
			var withdraw = _currencyLogic.WithdrawFromResourcePool(poolData.Id, widrawAmount);
			
			Assert.AreEqual(100, withdraw);
			Assert.AreEqual(0, _currencyLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.AreEqual(DateTime.UtcNow, _currencyLogic.ResourcePools[poolData.Id].LastPoolRestockTime);
		}

		[Test]
		public void WithdrawFromResourcePool_RestockOverflow_WithdrawFromFull()
		{
			const int widrawAmount = 100;
			
			var poolData = new ResourcePoolData(_poolConfig.Id, 0, DateTime.UtcNow.AddDays(-1));
			
			SetPoolData(poolData);
			
			var withdraw = _currencyLogic.WithdrawFromResourcePool(poolData.Id, widrawAmount);
			
			Assert.AreEqual(widrawAmount, withdraw);
			Assert.AreEqual(_poolConfig.PoolCapacity - widrawAmount, 
			                _currencyLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.AreEqual(DateTime.UtcNow, _currencyLogic.ResourcePools[poolData.Id].LastPoolRestockTime);
		}

		private void SetCurrencyData(params Pair<GameId, uint>[] currencies)
		{
			foreach (var pair in currencies)
			{
				TestData.Currencies.Add(pair.Key, pair.Value);
			}
		}

		private void SetPoolData(ResourcePoolData data)
		{
			TestData.ResourcePools.Add(data.Id, data);
			
			InitConfigData(_poolConfig);
		}
	}
}