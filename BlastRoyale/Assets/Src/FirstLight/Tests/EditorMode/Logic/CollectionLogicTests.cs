using FirstLight.Game.Data.DataTypes;
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
			var skin = ItemFactory.Collection(GameId.Avatar5);
			
			Assert.IsFalse(TestLogic.CollectionLogic.IsItemOwned(skin));
		}
		
		[Test]
		public void TestOwnedAfterAdding()
		{
			TestLogic.CollectionLogic.UnlockCollectionItem(ItemFactory.Collection(GameId.Avatar1));
			
			var skin = ItemFactory.Collection(GameId.Avatar1);
			
			Assert.IsTrue(TestLogic.CollectionLogic.IsItemOwned(skin));
		}
		
		
	}
}