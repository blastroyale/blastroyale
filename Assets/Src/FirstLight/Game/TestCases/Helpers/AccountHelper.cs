
using FirstLight.Game.Data;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class AccountHelper : TestHelper
	{
		public void FreshGameInstallation()
		{
			PlayerPrefs.DeleteKey(nameof(AppData));
		}

		public AccountHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}
	}
}