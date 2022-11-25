using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;
using Equipment = Quantum.Equipment;

namespace FirstLight.Tests.EditorMode.Integration
{
	public class EquipmentIntegrationTest : IntegrationTestFixture
	{
		/// <summary>
		/// Ensure logic adds item to inventory
		/// </summary>
		[Test]
		public void TestAddingEquipment()
		{
			var equip = new Equipment() {GameId = GameId.HockeyHelmet};
			var itemUniqueId = TestLogic.EquipmentLogic.AddToInventory(equip);
			
			var data = TestData.GetData<EquipmentData>();
			
			Assert.IsTrue(data.Inventory.ContainsKey(itemUniqueId));
		}
		
		/// <summary>
		/// Ensuring set loadout command equips the given item
		/// </summary>
		[Test]
		public void TestSetLoadoutCommand()
		{
			var equip = new Equipment() {GameId = GameId.HockeyHelmet};
			var itemUniqueId = TestLogic.EquipmentLogic.AddToInventory(equip);

			TestServices.CommandService.ExecuteCommand(new UpdateLoadoutCommand()
			{
				SlotsToUpdate = new Dictionary<GameIdGroup, UniqueId>()
				{
					{ GameIdGroup.Helmet, itemUniqueId }
				}
			});

			var data = TestData.GetData<PlayerData>();
			
			Assert.AreEqual(itemUniqueId, data.Equipped[GameIdGroup.Helmet]);
		}
		
		/// <summary>
		/// Ensuring scrap item command rewards the correct ammount of te scrap result
		/// </summary>
		[Test]
		public void ScrapItemCommand()
		{
			var equip = new Equipment() {GameId = GameId.HockeyHelmet};
			var itemUniqueId = TestLogic.EquipmentLogic.AddToInventory(equip);
			var info = TestLogic.EquipmentLogic.GetInfo(itemUniqueId);
			var data = TestData.GetData<PlayerData>();

			TestServices.CommandService.ExecuteCommand(new ScrapItemCommand()
			{
				Item = itemUniqueId
			});

			
			Assert.AreEqual(info.ScrappingValue.Value, data.Currencies[info.ScrappingValue.Key]);
		}
		
		/// <summary>
		/// Ensuring upgrade item command deducts the correct ammount of of the upgrade cost
		/// </summary>
		[Test]
		public void UpgradeItemCommand()
		{
			var equip = new Equipment() {GameId = GameId.HockeyHelmet, MaxLevel = 1};
			var itemUniqueId = TestLogic.EquipmentLogic.AddToInventory(equip);
			var info = TestLogic.EquipmentLogic.GetInfo(itemUniqueId);
			var data = TestData.GetData<PlayerData>();

			data.Currencies[info.UpgradeCost.Key] = info.UpgradeCost.Value;

			TestServices.CommandService.ExecuteCommand(new UpgradeItemCommand()
			{
				Item = itemUniqueId
			});

			Assert.AreEqual(0, data.Currencies[info.UpgradeCost.Key]);
			Assert.AreEqual(1, TestLogic.EquipmentLogic.Inventory[itemUniqueId].Level);
		}
		
		/// <summary>
		/// Ensuring repair item command deducts the correct ammount of the repair cost
		/// </summary>
		[Test]
		public void RepairItemCommand()
		{
			var equip = new Equipment() {GameId = GameId.HockeyHelmet, MaxDurability = 2};
			var itemUniqueId = TestLogic.EquipmentLogic.AddToInventory(equip);
			var cost = TestLogic.EquipmentLogic.GetRepairCost(equip, false);
			var data = TestData.GetData<PlayerData>();

			data.Currencies[cost.Key] = cost.Value;

			TestServices.CommandService.ExecuteCommand(new RepairItemCommand()
			{
				Item = itemUniqueId
			});

			Assert.AreEqual(0, data.Currencies[cost.Key]);
			Assert.AreEqual(equip.MaxDurability, TestLogic.EquipmentLogic.Inventory[itemUniqueId].Durability);
		}
	}
}