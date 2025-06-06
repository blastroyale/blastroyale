using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;
using Equipment = Quantum.Equipment;

namespace FirstLight.Tests.EditorMode.Integration
{
	public class RewardIntegrationTests : IntegrationTestFixture
	{
		[Test]
		public void TestTutorialSectionRewardList()
		{
			var tutorialRewards = TestConfigs.GetConfig<TutorialConfig>().Rewards.First();
			var rewardConfigs = TestConfigs.GetConfigsList<EquipmentRewardConfig>().Where(c => tutorialRewards.RewardIds.Contains((uint) c.Id));

			var itemsToBeRewarded = TestLogic.RewardLogic.GetRewardsFromTutorial(tutorialRewards.Section);

			Assert.AreEqual(itemsToBeRewarded.Count(), rewardConfigs.Count());
			Assert.IsTrue(itemsToBeRewarded.Select(i => i.Id).SequenceEqual(rewardConfigs.Select(c => c.GameId)));
		}

		[Test]
		public void TestGenericEquipmentReward()
		{
			var items = new List<ItemData>();
			items.Add(ItemFactory.Equipment(new Equipment(GameId.MouseShield)));
			var equipsBefore = TestData.GetData<EquipmentData>().Inventory.Count;
			TestLogic.RewardLogic.Reward(items);
			var equipsAfter = TestData.GetData<EquipmentData>().Inventory.Count;
			Assert.AreEqual(equipsAfter, equipsBefore + 1);
		}

		[Test]
		public void TestGenericUnclaimedReward()
		{
			var items = new List<ItemData>();
			items.Add(ItemFactory.Currency(GameId.COIN, 500));
			TestLogic.RewardLogic.RewardToUnclaimedRewards(items);
			var rewardsAfter = TestData.GetData<PlayerData>().UncollectedRewards;
			Assert.IsTrue(rewardsAfter.Any(r => r.GetMetadata<CurrencyMetadata>().Amount == 500 && r.Id == GameId.COIN));
		}

		[Test]
		public void TestGenericToInventory()
		{
			var items = new List<ItemData>();
			items.Add(ItemFactory.Currency(GameId.COIN, 500));
			TestLogic.RewardLogic.Reward(items);
			var rewardsAfter = TestData.GetData<PlayerData>().Currencies[GameId.COIN];
			Assert.IsTrue(rewardsAfter == 500);
		}

		[Test]
		public void TestCollectionReward()
		{
			var item = ItemFactory.Collection(GameId.Avatar5);

			Assert.IsFalse(TestLogic.CollectionLogic.IsItemOwned(item));
			TestLogic.RewardLogic.Reward(new[] {item});
			Assert.True(TestLogic.CollectionLogic.IsItemOwned(item));
		}

		// [Test]
		// public void TestCoreReward()
		// {
		// 	var item = ItemFactory.Simple(GameId.CoreRare);
		//
		// 	var equipsBefore = TestLogic.EquipmentLogic.Inventory.Count;
		// 	
		// 	TestLogic.RewardLogic.Reward(new [] {item});
		//
		// 	var equipsAfter = TestLogic.EquipmentLogic.Inventory.Count;
		// 	Assert.True(equipsAfter > equipsBefore);
		// }

		[Test]
		public void TestXPReward()
		{
			var item = ItemFactory.Currency(GameId.XP, 1);

			var xpBefore = TestLogic.PlayerLogic.XP.Value;

			TestLogic.RewardLogic.Reward(new[] {item});

			var xpAfter = TestLogic.PlayerLogic.XP.Value;
			Assert.True(xpAfter > xpBefore);
		}

		[Test]
		public void TesTrophyReward()
		{
			var item = ItemFactory.Currency(GameId.Trophies, 1);

			var before = TestLogic.PlayerLogic.Trophies.Value;

			TestLogic.RewardLogic.Reward(new[] {item});

			var after = TestLogic.PlayerLogic.Trophies.Value;
			Assert.True(after > before);
		}

		[Test]
		public void TestTutorialCompletingCommandRewards()
		{
			var tutorialRewards = TestConfigs.GetConfig<TutorialConfig>().Rewards.First();

			Assert.False(TestLogic.PlayerLogic.HasTutorialSection(tutorialRewards.Section));

			TestServices.CommandService.ExecuteCommand(new CompleteTutorialSectionCommand()
			{
				Sections = new[] {tutorialRewards.Section},
			});

			Assert.IsTrue(TestLogic.PlayerLogic.HasTutorialSection(tutorialRewards.Section));
		}
	}
}