using System;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Utils;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class PlayerConfigsHelper : TestHelper
	{
		private Action OverwriteFeatureFlags;

		private bool _authenticated = false;

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