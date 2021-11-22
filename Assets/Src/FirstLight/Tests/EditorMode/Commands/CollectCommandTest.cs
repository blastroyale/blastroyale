using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using NSubstitute;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode.Commands
{
	public class CollectCommandTest : BaseTestFixture<PlayerData>
	{ //TODO: Enable it when playFab calls are not in the commands
		/*[Test]
		public void CollectRewardCommand_CurrencyReward_Check()
		{
			var reward = new RewardData { RewardId = GameIdGroup.Currency.GetIds()[0], Data = 1 };

			RewardLogic.CollectReward().Returns(reward);
			
			Execute(new CollectRewardCommand());

			RewardLogic.Received().CollectReward();
			CurrencyLogic.Received().AddCurrency(reward.RewardId, reward.Data);
			UniqueIdLogic.DidNotReceive().GenerateNewUniqueId(Arg.Any<GameId>());
			EquipmentLogic.DidNotReceive().AddToInventory(Arg.Any<EquipmentData>());
			LootBoxLogic.DidNotReceive().OpenLootBox(Arg.Any<uint>());
			MessageBrokerService.Received().Publish(Arg.Is<RewardCollectedMessage>(t => t.Reward.Equals(reward)));
		}
		
		[Test]
		public void CollectRewardCommand_EquipmentReward_Check()
		{
			var reward = new RewardData { RewardId = GameIdGroup.Equipment.GetIds()[0], Data = 1 };

			RewardLogic.CollectReward().Returns(reward);
			
			Execute(new CollectRewardCommand());

			RewardLogic.Received().CollectReward();
			CurrencyLogic.DidNotReceive().AddCurrency(Arg.Any<GameId>(), Arg.Any<uint>());
			UniqueIdLogic.Received().GenerateNewUniqueId(reward.RewardId);
			EquipmentLogic.Received().AddToInventory(Arg.Any<EquipmentData>());
			LootBoxLogic.DidNotReceive().OpenLootBox(Arg.Any<uint>());
			MessageBrokerService.Received().Publish(Arg.Is<RewardCollectedMessage>(t => t.Reward.Equals(reward)));
		}
		
		[Test]
		public void CollectRewardCommand_LootBoxReward_Check()
		{
			var reward = new RewardData { RewardId = GameIdGroup.LootBox.GetIds()[0], Data = 1 };
			var lootBoxReward = new RewardData { RewardId = GameIdGroup.Currency.GetIds()[0], Data = 1 };

			LootBoxLogic.OpenLootBox(Arg.Any<uint>()).Returns(lootBoxReward.RewardId);
			RewardLogic.CollectReward().Returns(reward);
			
			Execute(new CollectRewardCommand());

			RewardLogic.Received().CollectReward();
			CurrencyLogic.Received(1).AddCurrency(lootBoxReward.RewardId, lootBoxReward.Data);
			UniqueIdLogic.DidNotReceive().GenerateNewUniqueId(Arg.Any<GameId>());
			EquipmentLogic.DidNotReceive().AddToInventory(Arg.Any<EquipmentData>());
			LootBoxLogic.Received().OpenLootBox(reward.Data);
			MessageBrokerService.Received().Publish(Arg.Is<RewardCollectedMessage>(t => t.Reward.Equals(lootBoxReward)));
		}
		
		[Test]
		public void CollectRewardCommand_NotReward_ThrowsException()
		{
			var reward = new RewardData { RewardId = GameIdGroup.Projectile.GetIds()[0], Data = 1 };

			RewardLogic.CollectReward().Returns(reward);
			
			Assert.Throws<LogicException>(() => Execute(new CollectRewardCommand()));
		}

		private void Execute(IGameCommand command)
		{
			command.Execute(GameLogic, DataService);
		}*/
	}
}