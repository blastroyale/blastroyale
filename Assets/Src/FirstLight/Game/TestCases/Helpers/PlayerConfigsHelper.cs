using FirstLight.Game.Data;

namespace FirstLight.Game.TestCases.Helpers
{
	public class PlayerConfigsHelper : TestHelper
	{
		public PlayerConfigsHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}

		public void SetEnableFPSLimit(bool enabled)
		{
			RunWhenAuthenticated(() => { Services.LocalPrefsService.IsFPSLimitEnabled.Value = enabled; });
		}

		public void SetTargetServer(string server)
		{
			RunWhenAuthenticated(() => { DataProviders.AppDataProvider.ConnectionRegion.Value = server; });
		}
	}
}