using System.Collections;
using FirstLight.Game.Presenters;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class HomeUIHelper : TestHelper
	{
		private UIHelper _uiHelper;

		public HomeUIHelper(FLGTestRunner testRunner, UIHelper uiHelper) : base(testRunner)
		{
			_uiHelper = uiHelper;
		}

		public IEnumerator WaitHomePresenter(float timeout = 30)
		{
			yield return _uiHelper.WaitForPresenter<HomeScreenPresenter>(0.5f, timeout);
		}

		public IEnumerator ClickBattlePassButton()
		{
			yield return WaitHomePresenter();
			yield return _uiHelper.TouchOnElementByName("BattlePassButton");
		}


		public IEnumerator ClickEquipmentButton()
		{
			yield return WaitHomePresenter();
			yield return _uiHelper.TouchOnElementByName("EquipmentButton");
		}

		public IEnumerator ClickPlayButton()
		{
			yield return WaitHomePresenter();
			yield return _uiHelper.TouchOnElementByName("PlayButton");
		}
	}
}