using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Presenters;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.TestCases.Helpers
{
	public class BattlePassUIHelper : TestHelper
	{
		private UIHelper _uiHelper;

		public BattlePassUIHelper(FLGTestRunner testRunner, UIHelper uiHelper) : base(testRunner)
		{
			_uiHelper = uiHelper;
		}


		public IEnumerator ClickToClaimFirstBattlePassReward()
		{
			yield return _uiHelper.WaitForPresenter2<BattlePassScreenPresenter>();
			var searchResult = _uiHelper.SearchForElementGlobally("RewardsScroll", builder => builder.Children<VisualElement>());
			if (searchResult == null)
			{
				Fail("Not found button first battle pass to click!");
				yield break;
			}

			yield return _uiHelper.TouchOnElement(searchResult.Value.Item1.rootVisualElement, searchResult.Value.Item2);
		}

		public IEnumerator WaitRewardDialogAndClaimIt()
		{
			yield return _uiHelper.WaitForPresenter2<EquipmentRewardDialogPresenter>();
			yield return new WaitForSeconds(1f);
			yield return _uiHelper.TouchOnElementByName("ConfirmButton");
			yield return new WaitForSeconds(1f);
		}
	}
}