using System.Collections;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;

namespace FirstLight.Game.TestCases.Helpers
{
	public class GameConfigHelper : TestHelper
	{
		public GameConfigHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}

		public IEnumerator DecreaseMatchmakingTime()
		{
			RunWhenAuthenticated(() =>
			{
				foreach (var slotWrapper in Services.GameModeService.Slots)
				{
					slotWrapper.Entry.PlayfabQueue.TimeoutTimeInSeconds = 5;
				}
			});
			yield break;
		}
	}
}