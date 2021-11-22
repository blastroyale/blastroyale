using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using UnityEngine;
using Assert = NUnit.Framework.Assert;


namespace FirstLight.Tests.EditorMode.Logic
{
	public class RewardLogicTest : BaseTestFixture<PlayerData>
	{
		/*private RewardLogic _rewardLogic;

		[SetUp]
		public void Init()
		{
			_rewardLogic = new RewardLogic(GameLogic, DataService);
		}

		[Test]
		public void GenerateRewardsCheck()
		{
			const int enemiesKilled = 3;
			const int coinsPerEnemy = 50;
			const int xpPerEnemy = 20;
			const int lootCollected = 1;
			const GameId loot = GameId.CommonBox;

			SetupLootConfig(lootCollected, loot);
			SetupQuestConfig(coinsPerEnemy, xpPerEnemy);
			
			_rewardLogic.GenerateRewards(SetupMatchData(enemiesKilled, lootCollected), Arg.Any<uint>());
			
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == GameId.SC && !x.IsLevelUpReward && 
			                                                      x.Quantity == enemiesKilled * coinsPerEnemy), 0);
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == GameId.XP && !x.IsLevelUpReward && 
			                                                      x.Quantity == enemiesKilled * xpPerEnemy), 0);
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == loot && !x.IsLevelUpReward && 
			                                                      x.Data == lootCollected), 0);
			Assert.AreEqual(3,TestData.Rewards.Count);
		}
		
		[Test]
		public void GenerateRewards_OnlyLootCollected_RewardsOnlyLoot()
		{
			const int lootCollected = 1;
			const GameId loot = GameId.CommonBox;
			
			SetupLootConfig(lootCollected, loot);
			
			_rewardLogic.GenerateRewards(SetupMatchData(0, lootCollected), Arg.Any<uint>());
			
			Assert.Less(TestData.Rewards.FindIndex(x => x.RewardId == GameId.SC), 0);
			Assert.Less(TestData.Rewards.FindIndex(x => x.RewardId == GameId.XP), 0);
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == loot && !x.IsLevelUpReward && 
			                                                      x.Data == lootCollected), 0);
			Assert.AreEqual(1,TestData.Rewards.Count);
		}
		
		[Test]
		public void GenerateRewards_OnlyEnemiesKilled_RewardsLoot()
		{
			const int enemiesKilled = 3;
			const int coinsPerEnemy = 50;
			const int xpPerEnemy = 20;
			
			SetupQuestConfig(coinsPerEnemy, xpPerEnemy);
			
			_rewardLogic.GenerateRewards(SetupMatchData(enemiesKilled), Arg.Any<uint>());
			
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == GameId.SC && !x.IsLevelUpReward && 
			                                                      x.Quantity == enemiesKilled * coinsPerEnemy), 0);
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == GameId.XP && !x.IsLevelUpReward && 
			                                                      x.Quantity == enemiesKilled * xpPerEnemy), 0);
			Assert.AreEqual(2,TestData.Rewards.Count);
		}
		
		[Test]
		public void GenerateRewards_EmptyMatchData_RewardsNothing()
		{
			_rewardLogic.GenerateRewards(new QuantumPlayerMatchData(), Arg.Any<uint>());
			
			Assert.AreEqual(0,TestData.Rewards.Count);
		}

		[TestCase(1u, 2u)]
		[TestCase(1u, 3u)]
		[TestCase(1u, 10u)]
		public void AddLevelUpRewardCheck(uint startLevel, uint endLevel)
		{
			const int coinsPerLevel = 50;

			SetupPlayerLevelConfig(coinsPerLevel);

			_rewardLogic.AddLevelUpRewards(startLevel, endLevel);
			
			Assert.GreaterOrEqual(TestData.Rewards.FindIndex(x => x.RewardId == GameId.SC && x.Quantity == coinsPerLevel), 0);
			Assert.AreEqual(endLevel - startLevel,TestData.Rewards.Count);
		}

		[Test]
		public void AddLevelUpReward_InverseLevels_DoesNothing()
		{
			const int coinsPerLevel = 50;
			const int startLevel = 2;
			const int endLevel = 1;

			SetupPlayerLevelConfig(coinsPerLevel);

			_rewardLogic.AddLevelUpRewards(startLevel, endLevel);
			
			Assert.AreEqual(0,TestData.Rewards.Count);
		}

		[Test]
		public void CollectRewardCheck()
		{ 
			var testReward1 = new RewardData { RewardId = GameId.AssaultRifle, IsLevelUpReward = false };
			var testReward2 = new RewardData { RewardId = GameId.XP, IsLevelUpReward = true };
			var testReward3 = new RewardData { RewardId = GameId.SC, IsLevelUpReward = true };
			
			TestData.Rewards.Add(testReward1);
			TestData.Rewards.Add(testReward2);
			TestData.Rewards.Add(testReward3);

			var rewards = _rewardLogic.CollectRewards();

			Assert.Contains(testReward1, rewards);
			Assert.Contains(testReward2, rewards);
			Assert.Contains(testReward3, rewards);
			Assert.AreEqual(3, rewards.Count);
		}

		[Test]
		public void CollectReward_WithLootBox_OnlyCollectsFirstLootBox()
		{ 
			var testReward1 = new RewardData { RewardId = GameId.CommonBox, IsLevelUpReward = false };
			var testReward2 = new RewardData { RewardId = GameId.XP, IsLevelUpReward = true };
			var testReward3 = new RewardData { RewardId = GameId.SC, IsLevelUpReward = true };
			var testReward4 = new RewardData { RewardId = GameId.UncommonBox, IsLevelUpReward = true };
			
			TestData.Rewards.Add(testReward1);
			TestData.Rewards.Add(testReward2);
			TestData.Rewards.Add(testReward3);
			TestData.Rewards.Add(testReward4);

			var rewards = _rewardLogic.CollectRewards();

			Assert.Contains(testReward1, rewards);
			Assert.AreEqual(1, rewards.Count);
		}

		[Test]
		public void CollectRewards_Empty_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _rewardLogic.CollectRewards());
		}

		private QuantumPlayerMatchData SetupMatchData(uint enemiesKilled, params uint[] lootCollected)
		{
			var killed = new List<uint>((int) enemiesKilled);
			var questEndKillCount = new List<uint>(Constants.QUEST_COUNT);

			for (uint i = 0; i < enemiesKilled; i++)
			{
				killed.Add(i);
			}

			// all enemies killed in quest 0
			questEndKillCount.Add(enemiesKilled);

			return new QuantumPlayerMatchData
			{
				EnemiesKilled = killed,
				LootCollected = new List<uint>(lootCollected),
				TotalKillsAtQuestEnd = questEndKillCount,
			};
		}

		private void SetupQuestConfig(uint coinsPerEnemy, uint xpPerEnemy)
		{
			var config = new QuantumQuestBucketConfig { Quests = new List<QuantumQuestConfig>() };

			for (var i = 0; i < Constants.QUEST_COUNT; i++)
			{
				config.Quests.Add(new QuantumQuestConfig
				{
					Coins = coinsPerEnemy, 
					MetaXp = xpPerEnemy
				});
			}
			
			InitConfigData(config);
		}

		private void SetupLootConfig(int lootId, GameId boxId)
		{
			var config = new QuantumLootBoxConfig { Id =  lootId, LootBoxId = boxId };
			
			InitConfigData(x => x.Id, config);
		}

		private void SetupPlayerLevelConfig(uint coinsReward)
		{
			var config = new QuantumPlayerLevelConfig
			{
				RewardGameId = GameId.SC,
				RewardCoins = coinsReward
			};
			
			InitConfigData(config);
		}*/
	}
}