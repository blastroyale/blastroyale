
using FirstLight.FLogger;
using FirstLight.Game.Data;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class AccountHelper : TestHelper
	{
		public void FreshGameInstallation()
		{
			PlayerPrefs.DeleteKey(nameof(AccountData));
		}

		public AccountHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}
	}
}