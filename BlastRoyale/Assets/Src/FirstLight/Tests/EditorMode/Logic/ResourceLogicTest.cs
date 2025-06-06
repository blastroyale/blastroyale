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
using Equipment = Quantum.Equipment;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class ResourceLogicTest : MockedTestFixture<PlayerData>
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

		// Depends on the equipment used and config. We shouldn't test it like this
		// [Test]
		// public void GetResourcePoolInfoCheck()
		// {
		// 	var info = _resourceLogic.GetResourcePoolInfo(_poolConfig.Id);
		// 	
		// 	Assert.AreEqual(_poolConfig.Id, info.Id);
		// 	Assert.That(info.WinnerRewardAmount, Is.EqualTo(20).Within(1));
		// 	Assert.That(info.CurrentAmount, Is.EqualTo(223).Within(1));
		// 	Assert.That(info.PoolCapacity, Is.EqualTo(223).Within(1));
		// 	Assert.That(info.NextRestockTime, 
		// 	            Is.EqualTo(DateTime.UtcNow.AddMinutes(_poolConfig.RestockIntervalMinutes)).Within(10).Seconds);
		// }

		[Test]
		public void WithdrawFromResourcePoolCheck()
		{
			var extraTime = 5;
			var poolData = new ResourcePoolData(_poolConfig.Id, 0,
				DateTime.UtcNow.AddMinutes(-_poolConfig.RestockIntervalMinutes - extraTime));

			TestData.ResourcePools[poolData.Id] = poolData;

			var withdraw = _resourceLogic.WithdrawFromResourcePool(poolData.Id, 100);

			Assert.That(withdraw, Is.EqualTo(50).Within(1));
			Assert.AreEqual(0, _resourceLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.That(_resourceLogic.ResourcePools[poolData.Id].LastPoolRestockTime,
				Is.EqualTo(DateTime.UtcNow.AddMinutes(-extraTime)).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePool_EmptyPool_NothingHappens()
		{
			var poolData = new ResourcePoolData(_poolConfig.Id, 0, DateTime.UtcNow);

			TestData.ResourcePools[poolData.Id] = poolData;

			var withdraw = _resourceLogic.WithdrawFromResourcePool(_poolConfig.Id, 100);

			Assert.AreEqual(0, withdraw);
			Assert.AreEqual(0, _resourceLogic.ResourcePools[_poolConfig.Id].CurrentResourceAmountInPool);
			Assert.That(DateTime.UtcNow,
				Is.EqualTo(_resourceLogic.ResourcePools[_poolConfig.Id].LastPoolRestockTime).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePool_OverflowWithdraw_WithdrawLeft()
		{
			const int widrawAmount = 200;

			var poolData = new ResourcePoolData(_poolConfig.Id, 100, DateTime.UtcNow);

			TestData.ResourcePools[poolData.Id] = poolData;

			var withdraw = _resourceLogic.WithdrawFromResourcePool(poolData.Id, widrawAmount);

			Assert.AreEqual(100, withdraw);
			Assert.AreEqual(0, _resourceLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool);
			Assert.That(_resourceLogic.ResourcePools[poolData.Id].LastPoolRestockTime,
				Is.EqualTo(DateTime.UtcNow).Within(10).Seconds);
		}

		[Test]
		public void WithdrawFromResourcePool_RestockOverflow_WithdrawFromFull()
		{
			const int withdrawAmount = 100;

			var poolData = new ResourcePoolData(_poolConfig.Id, 0, DateTime.UtcNow.AddDays(-2));

			TestData.ResourcePools[poolData.Id] = poolData;

			var withdraw = _resourceLogic.WithdrawFromResourcePool(poolData.Id, withdrawAmount);

			Assert.AreEqual(withdrawAmount, withdraw);
			Assert.That(_resourceLogic.ResourcePools[poolData.Id].CurrentResourceAmountInPool, Is.EqualTo(100));
			Assert.That(_resourceLogic.ResourcePools[poolData.Id].LastPoolRestockTime,
				Is.EqualTo(DateTime.UtcNow).Within(10).Seconds);
		}

		private void SetupPoolConfigs()
		{
			var nftList = new List<EquipmentInfo>
			{
				new()
				{
					Equipment = new Equipment(GameId.BaseballArmor, rarity: EquipmentRarity.RarePlus,
						grade: EquipmentGrade.GradeV, adjective: EquipmentAdjective.Regular, maxDurability: 100),
					CurrentDurability = 50
				},
				new()
				{
					Equipment = new Equipment(GameId.SciCannon, rarity: EquipmentRarity.Rare,
						grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Exquisite, maxDurability: 100),
					CurrentDurability = 70
				},
				new()
				{
					Equipment = new Equipment(GameId.MouseAmulet, rarity: EquipmentRarity.Uncommon,
						grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Cool, maxDurability: 100),
					CurrentDurability = 65
				},
				new()
				{
					Equipment = new Equipment(GameId.RoadShield, rarity: EquipmentRarity.Legendary,
						grade: EquipmentGrade.GradeI, adjective: EquipmentAdjective.Royal, maxDurability: 100),
					CurrentDurability = 34
				},
				new()
				{
					Equipment = new Equipment(GameId.BaseballHelmet, rarity: EquipmentRarity.LegendaryPlus,
						grade: EquipmentGrade.GradeIV, adjective: EquipmentAdjective.Divine, maxDurability: 100),
					CurrentDurability = 97
				},
			};

			var nonNftList = new List<EquipmentInfo>
			{
				new()
				{
					Equipment = new Equipment(GameId.FootballArmor, rarity: EquipmentRarity.Common,
						grade: EquipmentGrade.GradeIV, adjective: EquipmentAdjective.Regular, maxDurability: 100),
					CurrentDurability = 90
				},
				new()
				{
					Equipment = new Equipment(GameId.ModPistol, rarity: EquipmentRarity.Common,
						grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Regular, maxDurability: 100),
					CurrentDurability = 80
				},
				new()
				{
					Equipment = new Equipment(GameId.RiotAmulet, rarity: EquipmentRarity.Uncommon,
						grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Cool, maxDurability: 100),
					CurrentDurability = 65
				},
				new()
				{
					Equipment = new Equipment(GameId.WarriorShield, rarity: EquipmentRarity.CommonPlus,
						grade: EquipmentGrade.GradeIII, adjective: EquipmentAdjective.Regular, maxDurability: 100),
					CurrentDurability = 66
				},
				new()
				{
					Equipment = new Equipment(GameId.HockeyHelmet, rarity: EquipmentRarity.Rare,
						grade: EquipmentGrade.GradeV, adjective: EquipmentAdjective.Regular, maxDurability: 100),
					CurrentDurability = 100
				},
			};

			var bothList = new List<EquipmentInfo>();
			bothList.AddRange(nftList);
			bothList.AddRange(nonNftList);

			_poolConfig = new ResourcePoolConfig
			{
				Id = GameId.CS,
				PoolCapacity = 200,
				RestockIntervalMinutes = 180,
				TotalRestockIntervalMinutes = 720,
				BaseMaxTake = 16,
				ScaleMultiplier = 10,
				ShapeModifier = FP._1_50,
				MaxPoolCapacityDecreaseModifier = FP.FromString("0.9"),
				PoolCapacityDecreaseExponent = FP.FromString("0.3"),
				MaxTakeDecreaseModifier = FP.FromString("0.11"),
				TakeDecreaseExponent = FP.FromString("1.8")
			};

			GameLogic.PlayerLogic.Trophies.Returns(new ObservableField<uint>(1000));
			GameLogic.EquipmentLogic.Loadout.Count.Returns(nftList.Count);
			EquipmentLogic.GetInventoryEquipmentInfo(EquipmentFilter.NftOnlyNotOnCooldown).Returns(nftList);
			EquipmentLogic.GetLoadoutEquipmentInfo(EquipmentFilter.NftOnlyNotOnCooldown).Returns(nftList);
			EquipmentLogic.GetInventoryEquipmentInfo(EquipmentFilter.NoNftOnly).Returns(nonNftList);
			EquipmentLogic.GetLoadoutEquipmentInfo(EquipmentFilter.NoNftOnly).Returns(nonNftList);
			EquipmentLogic.GetInventoryEquipmentInfo(EquipmentFilter.All).Returns(bothList);
			EquipmentLogic.GetLoadoutEquipmentInfo(EquipmentFilter.All).Returns(bothList);

			InitConfigData(_poolConfig);
			InitConfigData(new QuantumGameConfig
			{
				NftAssumedOwned = 40, MinNftForPoolSizeBonus = 3,
				EarningsAugmentationStrengthSteepnessMod = FP.FromString("3"),
				EarningsAugmentationStrengthDropMod = FP.FromString("0.08")
			});
			InitConfigData(config => (int) config.Grade,
				new GradeDataConfig {Grade = EquipmentGrade.GradeI, PoolIncreaseModifier = FP.FromString("0.135")});
			InitConfigData(config => (int) config.Grade,
				new GradeDataConfig {Grade = EquipmentGrade.GradeII, PoolIncreaseModifier = FP.FromString("0.085")});
			InitConfigData(config => (int) config.Grade,
				new GradeDataConfig {Grade = EquipmentGrade.GradeIII, PoolIncreaseModifier = FP.FromString("0.05")});
			InitConfigData(config => (int) config.Grade,
				new GradeDataConfig {Grade = EquipmentGrade.GradeIV, PoolIncreaseModifier = FP.FromString("0.025")});
			InitConfigData(config => (int) config.Grade,
				new GradeDataConfig {Grade = EquipmentGrade.GradeV, PoolIncreaseModifier = FP.FromString("0")});

			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Regular, PoolCapacityModifier = FP.FromString("0")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Cool, PoolCapacityModifier = FP.FromString("0")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Ornate, PoolCapacityModifier = FP.FromString("0")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Posh, PoolCapacityModifier = FP.FromString("0.00125")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Exquisite, PoolCapacityModifier = FP.FromString("0.00125")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Majestic, PoolCapacityModifier = FP.FromString("0.0025")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Marvelous, PoolCapacityModifier = FP.FromString("0.0025")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Magnificent, PoolCapacityModifier = FP.FromString("0.005")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Royal, PoolCapacityModifier = FP.FromString("0.01")});
			InitConfigData(config => (int) config.Adjective,
				new AdjectiveDataConfig
					{Adjective = EquipmentAdjective.Divine, PoolCapacityModifier = FP.FromString("0.01")});

			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig {Rarity = EquipmentRarity.Common, PoolCapacityModifier = FP.FromString("0")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig {Rarity = EquipmentRarity.CommonPlus, PoolCapacityModifier = FP.FromString("0")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig
					{Rarity = EquipmentRarity.Uncommon, PoolCapacityModifier = FP.FromString("0.00125")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig
					{Rarity = EquipmentRarity.UncommonPlus, PoolCapacityModifier = FP.FromString("0.00125")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig {Rarity = EquipmentRarity.Rare, PoolCapacityModifier = FP.FromString("0.0025")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig
					{Rarity = EquipmentRarity.RarePlus, PoolCapacityModifier = FP.FromString("0.0025")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig {Rarity = EquipmentRarity.Epic, PoolCapacityModifier = FP.FromString("0.005")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig
					{Rarity = EquipmentRarity.EpicPlus, PoolCapacityModifier = FP.FromString("0.005")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig
					{Rarity = EquipmentRarity.Legendary, PoolCapacityModifier = FP.FromString("0.01")});
			InitConfigData(config => (int) config.Rarity,
				new RarityDataConfig
					{Rarity = EquipmentRarity.LegendaryPlus, PoolCapacityModifier = FP.FromString("0.01")});
		}
	}
}