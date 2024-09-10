using System.Collections;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data;
using FirstLight.Game.Utils;

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
				foreach (var queuesConfigValue in MainInstaller.ResolveData().RemoteConfigProvider.GetConfig<MatchmakingQueuesConfig>().Values)
				{
					queuesConfigValue.QueueTimeoutTimeInSeconds = 5;
				}
			});
			yield break;
		}
	}
}