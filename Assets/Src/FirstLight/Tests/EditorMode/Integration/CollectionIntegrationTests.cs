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
	public class CollectionIntegrationTests : IntegrationTestFixture
	{
		[Test]
		public void TestEquipSkinCommand()
		{
			var cmd = new EquipCollectionItemCommand() { Item = ItemFactory.Collection(GameId.FemaleAssassin) };
			
			TestServices.CommandService.ExecuteCommand(cmd);
			
			Assert.IsTrue(TestData.GetData<CollectionData>().Equipped[CollectionCategories.PLAYER_SKINS].Id == GameId.FemaleAssassin);
			Assert.IsTrue(TestLogic.CollectionLogic.GetEquipped(CollectionCategories.PLAYER_SKINS).Id == GameId.FemaleAssassin);
		}
	}
}