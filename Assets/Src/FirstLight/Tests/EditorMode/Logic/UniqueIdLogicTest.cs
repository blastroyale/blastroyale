using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class UniqueIdLogicTest : BaseTestFixture<IdData>
	{
		private UniqueIdLogic _uniqueIdLogic;
		private readonly GameId _gameID = GameId.AssaultRifle;

		[SetUp]
		public void Init()
		{
			_uniqueIdLogic = new UniqueIdLogic(GameLogic, DataService);
			_uniqueIdLogic.Init();
		}

		[Test]
		public void GenerateUniqueIdTest()
		{
			var currentLastUniqueId = TestData.UniqueIdCounter;
			var newUniqueId = _uniqueIdLogic.GenerateNewUniqueId(_gameID);

			Assert.IsTrue(currentLastUniqueId + 1 == newUniqueId);
			Assert.AreEqual(_gameID, _uniqueIdLogic.Ids[newUniqueId]);

		}

		[Test]
		public void RemoveIdTest()
		{
			var uniqueId = new UniqueId(1);
			TestData.GameIds.Add(uniqueId, _gameID);
			_uniqueIdLogic.RemoveId(uniqueId);

			Assert.AreEqual(0, TestData.GameIds.Count);
		}

		[Test]
		public void RemoveId_NoId_ThrowsException()
		{
			Assert.Throws<LogicException>(() => _uniqueIdLogic.RemoveId(UniqueId.Invalid));
		}
	}
}