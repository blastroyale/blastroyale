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
			Assert.IsFalse(_logic.HasTutorialSection(TutorialSection.FIRST_GUIDE_MATCH));
			
			_logic.MarkTutorialSectionCompleted(TutorialSection.FIRST_GUIDE_MATCH);
			
			Assert.IsTrue(_logic.HasTutorialSection(TutorialSection.FIRST_GUIDE_MATCH));
		}
	}
}