using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;
using Equipment = Quantum.Equipment;

namespace FirstLight.Tests.EditorMode.Integration
{
	public class RewardIntegrationTests : IntegrationTestFixture
	{
		[Test]
		public void TestTutorialStepRewardList()
		{
			var tutorialRewards = TestConfigs.GetConfigsList<TutorialRewardConfig>().First();
			var rewardConfigs = TestConfigs.GetConfigsList<EquipmentRewardConfig>().Where(c => tutorialRewards.RewardIds.Contains((uint)c.Id));

			var itemsToBeRewarded = TestLogic.RewardLogic.GetRewardsFromTutorial(tutorialRewards._section);
			
			Assert.AreEqual(itemsToBeRewarded.Count, rewardConfigs.Count());
			Assert.IsTrue(itemsToBeRewarded.Select(i => i.Id).SequenceEqual(rewardConfigs.Select(c => c.GameId)));
		}
		
		[Test]
		public void TestGenericEquipmentReward()
		{

			var items = new List<ItemData>();
			items.Add(new ItemData()
			{
				Id = GameId.MouseShield,
				Amount = 1,
				ItemObject = new Equipment(GameId.MouseShield)
			});

			var equipsBefore = TestData.GetData<EquipmentData>().Inventory.Count;
			
			TestLogic.RewardLogic.GiveItems(items);
			
			var equipsAfter = TestData.GetData<EquipmentData>().Inventory.Count;
			
			Assert.AreEqual(equipsAfter, equipsBefore + 1);
		}
		
		[Test]
		public void TestGenericUnclaimedReward()
		{
			var items = new List<ItemData>();
			items.Add(new ItemData()
			{
				Id = GameId.COIN,
				Amount = 500,
			});
			
			TestLogic.RewardLogic.GiveItems(items);
			
			var rewardsAfter = TestData.GetData<PlayerData>().UncollectedRewards;

			Assert.IsTrue(rewardsAfter.Any(r => r.Value == 500 && r.RewardId == GameId.COIN));
		}
		
		[Test]
		public void TestTutorialCompletingCommandRewards()
		{
			var tutorialRewards = TestConfigs.GetConfigsList<TutorialRewardConfig>().First();

			Assert.False(TestLogic.PlayerLogic.HasTutorialStep(tutorialRewards._section));
			
			TestServices.CommandService.ExecuteCommand(new CompleteTutorialStepCommand()
			{
				Section = tutorialRewards._section
			});
			
			Assert.IsTrue(TestLogic.PlayerLogic.HasTutorialStep(tutorialRewards._section));
		}
	}
}