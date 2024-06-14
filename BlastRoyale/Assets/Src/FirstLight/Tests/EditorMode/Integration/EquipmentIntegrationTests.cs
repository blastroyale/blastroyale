using System;
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
			var equip = new Equipment()
			{
				GameId = GameId.HockeyHelmet,
				Level = 1
			};
			var itemUniqueId = TestLogic.EquipmentLogic.AddToInventory(equip);

			TestServices.CommandService.ExecuteCommand(new UpdateLoadoutCommand()
			{
				SlotsToUpdate = new Dictionary<GameIdGroup, UniqueId>()
				{
					{GameIdGroup.Helmet, itemUniqueId}
				}
			});

			var data = TestData.GetData<PlayerData>();

			Assert.AreEqual(itemUniqueId, data.Equipped[GameIdGroup.Helmet]);
		}
	}
}