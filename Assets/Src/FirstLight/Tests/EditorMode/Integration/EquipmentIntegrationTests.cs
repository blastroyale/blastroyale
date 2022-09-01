using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Logic.RPC;
using NSubstitute;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class EquipmentIntegrationLogicTest : IntegrationTestFixture
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
	}
}