using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using NSubstitute;
using NUnit.Framework;
using Photon.Deterministic;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class ResourceLogicTest : BaseTestFixture<PlayerData>
	{
		private ResourcePoolConfig _poolConfig;
		private ResourceLogic _resourceLogic;

		[SetUp]
		public void Init()
		{
			_resourceLogic = new ResourceLogic(GameLogic, DataService);
			
			SetupPoolConfigs();
			_resourceLogic.Init();
		}
		
		[Test]
		public void GetResourcePoolInfoCheck()
		{
			var info = _resourceLogic.GetResourcePoolInfo(_poolConfig.Id);
			
			Assert.AreEqual(_poolConfig.Id, info.Id);
			Assert.That(20, Is.EqualTo(info.WinnerRewardAmount).Within(1));
			Assert.That(446, Is.EqualTo(info.CurrentAmount).Within(1)); // 668 - 223
			Assert.That(446, Is.EqualTo(info.PoolCapacity).Within(1)); // 668 - 223
			Assert.That(DateTime.UtcNow.AddMinutes(_poolConfig.RestockIntervalMinutes), 
			            Is.EqualTo(info.NextRestockTime).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePoolCheck()
		{
			var extraTime = 5;
			var poolData = new ResourcePoolData(_poolConfig.Id, 0,
			                                    DateTime.UtcNow.AddMinutes(-_poolConfig.RestockIntervalMinutes - extraTime));
			
			TestData.ResourcePools.Add(poolData.Id, poolData);
			
			var withdraw = _resourceLogic.WithdrawFromResourcePool(poolData.Id, 100);
			
			Assert.AreEqual(44, withdraw);
			Assert.AreEqual(0, _resourceLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.That(DateTime.UtcNow.AddMinutes(-extraTime), 
			            Is.EqualTo(_resourceLogic.ResourcePools[poolData.Id].LastPoolRestockTime).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePool_EmptyPool_NothingHappens()
		{
			var poolData = new ResourcePoolData(_poolConfig.Id, 0, DateTime.UtcNow);
			
			TestData.ResourcePools.Add(poolData.Id, poolData);
			
			var withdraw = _resourceLogic.WithdrawFromResourcePool(_poolConfig.Id, 100);
			
			Assert.AreEqual(0, withdraw);
			Assert.AreEqual(0, _resourceLogic.ResourcePools[_poolConfig.Id].CurrentResourceAmountInPool);
			Assert.That(DateTime.UtcNow, Is.EqualTo(_resourceLogic.ResourcePools[_poolConfig.Id].LastPoolRestockTime).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePool_OverflowWithdraw_WithdrawLeft()
		{
			const int widrawAmount = 200;
			
			var poolData = new ResourcePoolData(_poolConfig.Id, 100, DateTime.UtcNow);
			
			TestData.ResourcePools.Add(poolData.Id, poolData);
			
			var withdraw = _resourceLogic.WithdrawFromResourcePool(poolData.Id, widrawAmount);
			
			Assert.AreEqual(100, withdraw);
			Assert.AreEqual(0, _resourceLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.That(DateTime.UtcNow, Is.EqualTo(_resourceLogic.ResourcePools[poolData.Id].LastPoolRestockTime).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePool_RestockOverflow_WithdrawFromFull()
		{
			const int widrawAmount = 100;
			
			var poolData = new ResourcePoolData(_poolConfig.Id, 0, DateTime.UtcNow.AddDays(-1));
			
			TestData.ResourcePools.Add(poolData.Id, poolData);
			
			var withdraw = _resourceLogic.WithdrawFromResourcePool(poolData.Id, widrawAmount);
			
			Assert.AreEqual(widrawAmount, withdraw);
			Assert.That(346, Is.EqualTo(_resourceLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool).Within(1));
			Assert.That(DateTime.UtcNow, Is.EqualTo(_resourceLogic.ResourcePools[poolData.Id].LastPoolRestockTime).Within(10).Seconds);
		}

		private void SetupPoolConfigs()
		{
			var list = new List<EquipmentInfo>
			{
				new() { Equipment = new Equipment(GameId.Hammer, rarity: EquipmentRarity.RarePlus, grade: EquipmentGrade.GradeV, adjective: EquipmentAdjective.Regular, durability: 50, maxDurability: 100 )},
				new() { Equipment = new Equipment(GameId.Hammer, rarity: EquipmentRarity.Rare, grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Exquisite, durability: 70, maxDurability: 100 )},
				new() { Equipment = new Equipment(GameId.Hammer, rarity: EquipmentRarity.Uncommon, grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Cool, durability: 65, maxDurability: 100 )},
				new() { Equipment = new Equipment(GameId.Hammer, rarity: EquipmentRarity.Legendary, grade: EquipmentGrade.GradeI, adjective: EquipmentAdjective.Royal, durability: 34, maxDurability: 100 )},
				new() { Equipment = new Equipment(GameId.Hammer, rarity: EquipmentRarity.LegendaryPlus, grade: EquipmentGrade.GradeIV, adjective: EquipmentAdjective.Divine, durability: 97, maxDurability: 100 )},
			};
			
			_poolConfig = new ResourcePoolConfig
			{
				Id = GameId.CS,
				PoolCapacity = 1000,
				RestockIntervalMinutes = 100,
				TotalRestockIntervalMinutes = 1000,
				BaseMaxTake = 16,
				ScaleMultiplier = 15,
				ShapeModifier = FP._1_50,
				MaxPoolCapacityDecreaseModifier = FP.FromString("0.9"),
				PoolCapacityDecreaseExponent = FP.FromString("0.3"),
				MaxTakeDecreaseModifier = FP.FromString("0.11"),
				TakeDecreaseExponent = FP.FromString("0.18"),
				PoolCapacityTrophiesModifier = 10000
			};

			GameLogic.PlayerLogic.Trophies.Returns(new ObservableField<uint>(1000));
			GameLogic.EquipmentLogic.Loadout.Count.Returns(list.Count);
			EquipmentLogic.GetInventoryEquipmentInfo().Returns(list);
			EquipmentLogic.GetLoadoutEquipmentInfo().Returns(list);
			InitConfigData(_poolConfig);
			InitConfigData(new QuantumGameConfig { NftAssumedOwned = 40, MinNftForEarnings = 3 });
			InitConfigData(config => (int) config.Grade, new GradeDataConfig { Grade = EquipmentGrade.GradeV, PoolIncreaseModifier = FP._0});
			InitConfigData(config => (int) config.Grade, new GradeDataConfig { Grade = EquipmentGrade.GradeV, PoolIncreaseModifier = FP._0_05});
			InitConfigData(config => (int) config.Grade, new GradeDataConfig { Grade = EquipmentGrade.GradeV, PoolIncreaseModifier = FP._0_05});
			InitConfigData(config => (int) config.Grade, new GradeDataConfig { Grade = EquipmentGrade.GradeV, PoolIncreaseModifier = FP.FromString("0.135")});
			InitConfigData(config => (int) config.Grade, new GradeDataConfig { Grade = EquipmentGrade.GradeV, PoolIncreaseModifier = FP.FromString("0.025")});
			InitConfigData(config => (int) config.Adjective, new AdjectiveDataConfig { Adjective = EquipmentAdjective.Regular, PoolCapacityModifier = FP._0});
			InitConfigData(config => (int) config.Adjective, new AdjectiveDataConfig { Adjective = EquipmentAdjective.Exquisite, PoolCapacityModifier = FP.FromString("0.00125")});
			InitConfigData(config => (int) config.Adjective, new AdjectiveDataConfig { Adjective = EquipmentAdjective.Cool, PoolCapacityModifier = FP._0});
			InitConfigData(config => (int) config.Adjective, new AdjectiveDataConfig { Adjective = EquipmentAdjective.Royal, PoolCapacityModifier = FP._0_01});
			InitConfigData(config => (int) config.Adjective, new AdjectiveDataConfig { Adjective = EquipmentAdjective.Divine, PoolCapacityModifier = FP._0_01});
			InitConfigData(config => (int) config.Rarity, new RarityDataConfig { Rarity = EquipmentRarity.RarePlus, PoolCapacityModifier = FP.FromString("0.0025")});
			InitConfigData(config => (int) config.Rarity, new RarityDataConfig { Rarity = EquipmentRarity.Rare, PoolCapacityModifier = FP.FromString("0.0025")});
			InitConfigData(config => (int) config.Rarity, new RarityDataConfig { Rarity = EquipmentRarity.Uncommon, PoolCapacityModifier = FP.FromString("0.00125")});
			InitConfigData(config => (int) config.Rarity, new RarityDataConfig { Rarity = EquipmentRarity.Legendary, PoolCapacityModifier = FP._0_01});
			InitConfigData(config => (int) config.Rarity, new RarityDataConfig { Rarity = EquipmentRarity.LegendaryPlus, PoolCapacityModifier = FP._0_01});
		}
	}
}