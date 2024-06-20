using System.Collections.Generic;
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
	public class UniqueIdLogicTest : MockedTestFixture<IdData>
	{
		private UniqueIdLogic _uniqueIdLogic;
		private readonly GameId _gameID = GameId.ModRifle;

		[SetUp]
		public void Init()
		{
			DataService.GetData<AppData>().Returns(new AppData());
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