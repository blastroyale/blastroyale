
using FirstLight.FLogger;
using FirstLight.Game.Data;
using Unity.Services.Authentication;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class AccountHelper : TestHelper
	{
		public void FreshGameInstallation()
		{
			AuthenticationService.Instance.SignOut(true);
			PlayerPrefs.DeleteKey(nameof(AccountData));
		}

		public AccountHelper(FLGTestRunner testRunner) : base(testRunner)
		{
		}
	}
}