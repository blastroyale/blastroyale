using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class CollectionLogicTests : IntegrationTestFixture
	{
		[Test]
		public void TestOwnedChecks()
		{
			var skin = ItemFactory.Collection(GameId.Corpo);
			
			Assert.IsFalse(TestLogic.CollectionLogic.IsItemOwned(skin));
		}
		
		[Test]
		public void TestOwnedAfterAdding()
		{
			TestLogic.CollectionLogic.UnlockCollectionItem(ItemFactory.Collection(GameId.Corpo));
			
			var skin = ItemFactory.Collection(GameId.Corpo);
			
			Assert.IsTrue(TestLogic.CollectionLogic.IsItemOwned(skin));
		}
		
		[Test]
		public void TestOwnedDefault()
		{
			var skin = ItemFactory.Collection(GameId.FemaleAssassin);
			
			Assert.IsTrue(TestLogic.CollectionLogic.IsItemOwned(skin));
		}
		
	}
}