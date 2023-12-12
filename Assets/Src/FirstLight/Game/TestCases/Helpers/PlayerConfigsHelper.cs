using FirstLight.Game.Data;

namespace FirstLight.Game.TestCases.Helpers
{
	public class PlayerConfigsHelper : TestHelper
	{
		public PlayerConfigsHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}


		public void SetFpsTarget(FpsTarget target)
		{
			RunWhenAuthenticated(() => { DataProviders.AppDataProvider.FpsTarget = target; });
		}

		public void SetTargetServer(string server)
		{
			RunWhenAuthenticated(() => { DataProviders.AppDataProvider.ConnectionRegion.Value = server; });
		}
	}
}