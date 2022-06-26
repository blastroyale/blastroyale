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
		private RewardLogic _rewardLogic;

		[SetUp]
		public void Init()
		{
			_rewardLogic = new RewardLogic(GameLogic, DataService);
		}

		[Test]
		public void GiveMatchRewardsCheck()
		{
			// TODO:
		}

		[Test]
		public void GiveMatchRewards_EmptyPool_RewardsNothing()
		{
			// TODO:
		}

		[Test]
		public void GiveMatchRewards_PlayerQuit_RewardsNothing()
		{
			// TODO:
		}
		
		[Test]
		public void GenerateRewards_EmptyMatchData_RewardsNothing()
		{
			_rewardLogic.GiveMatchRewards(new QuantumPlayerMatchData(), false);
			
			Assert.AreEqual(0,TestData.Rewards.Count);
		}

		[Test]
		public void CollectRewardsCheck()
		{ 
			var testReward1 = new RewardData { RewardId = GameId.AssaultRifle, IsLevelUpReward = false };
			var testReward2 = new RewardData { RewardId = GameId.XP, IsLevelUpReward = true };
			var testReward3 = new RewardData { RewardId = GameId.CS, IsLevelUpReward = true };
			
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
		public void CollectRewards_Empty_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _rewardLogic.CollectRewards());
		}

		[Test]
		public void CollectRewardCheck()
		{ 
			var testReward = new RewardData { RewardId = GameId.CS, IsLevelUpReward = true };
			
			TestData.Rewards.Add(testReward);

			Assert.AreEqual(testReward, _rewardLogic.CollectReward(testReward));
		}

		[Test]
		public void CollectReward_Empty_ThrowsException()
		{
			var testReward = new RewardData { RewardId = GameId.CS, IsLevelUpReward = true };
			
			Assert.Throws<LogicException>(() => _rewardLogic.CollectReward(testReward));
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
	}
}