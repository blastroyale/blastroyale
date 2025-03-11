using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class Web3LogicTests : MockedTestFixture<Web3PlayerData>
	{
		[Test]
		public void TestPackingResources()
		{
			var logic = new Web3Logic(GameLogic, DataService);
			
			var item = ItemFactory.Currency(GameId.XP, 100);

			var packed = Web3Logic.PackItem(item);
			var unpacked = Web3Logic.UnpackItem(packed);
			
			Assert.AreEqual(unpacked.GetMetadata<CurrencyMetadata>().Amount, item.GetMetadata<CurrencyMetadata>().Amount);
		}
	}
}