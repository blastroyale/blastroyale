using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode.Logic
{
	public class AppLogicTest : BaseTestFixture<AppData>
	{
		private AppLogic _appLogic;

		[SetUp]
		public void Init()
		{
			_appLogic = new AppLogic(GameLogic, DataService, AudioFxService);
		}

		[Test]
		public void GameReviewCheck()
		{
			Assert.False(_appLogic.IsGameReviewed);
			
			_appLogic.MarkGameAsReviewed();
			
			Assert.True(_appLogic.IsGameReviewed);
			Assert.Throws<LogicException>(() => _appLogic.MarkGameAsReviewed());
		}
	}
}