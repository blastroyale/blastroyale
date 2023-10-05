using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Integration
{
	public class CollectionIntegrationTests : IntegrationTestFixture
	{
		[Test]
		public void TestEquipSkinCommand()
		{
			var cmd = new EquipCollectionItemCommand() {Item = ItemFactory.Collection(GameId.FemaleAssassin)};
			TestServices.CommandService.ExecuteCommand(new GiveDefaultCollectionItemsCommand());
			TestServices.CommandService.ExecuteCommand(cmd);

			Assert.IsTrue(TestData.GetData<CollectionData>().Equipped[CollectionCategories.PLAYER_SKINS].Id == GameId.FemaleAssassin);
			Assert.IsTrue(TestLogic.CollectionLogic.GetEquipped(CollectionCategories.PLAYER_SKINS).Id == GameId.FemaleAssassin);
		}

		[Test]
		public void TestGiveDefaultSkins()
		{
			var hasNoSkin = TestLogic.CollectionLogic.GetCollectionsCategories().All(category => TestLogic.CollectionLogic.GetOwnedCollection(category).Count == 0);
			Assert.IsTrue(hasNoSkin, "Data should be empty on creation!");

			TestServices.CommandService.ExecuteCommand(new GiveDefaultCollectionItemsCommand());

			Assert.IsTrue(TestLogic.CollectionLogic.HasAllDefaultCollectionItems(), "Do not have all default skins after running command!");
		}
	}
}