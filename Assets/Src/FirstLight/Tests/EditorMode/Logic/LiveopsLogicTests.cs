using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;
using Quantum;
using Assert = NUnit.Framework.Assert;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class TutorialDataTests : MockedTestFixture<TutorialData>
	{
		private PlayerLogic _logic;

		[SetUp]
		public void Init()
		{
			_logic = new PlayerLogic(GameLogic, DataService);
			_logic.Init();
		}

		[Test]
		public void TestLiveopsTutorialFlags()
		{
			Assert.IsFalse(_logic.HasTutorialSection(TutorialSection.FTUE_MAP));
			
			_logic.MarkTutorialSectionCompleted(TutorialSection.FTUE_MAP);
			
			Assert.IsTrue(_logic.HasTutorialSection(TutorialSection.FTUE_MAP));
		}
	}
}