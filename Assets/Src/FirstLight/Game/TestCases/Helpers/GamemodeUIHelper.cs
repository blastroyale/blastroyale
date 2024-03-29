using System.Collections;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class GamemodeUIHelper : TestHelper
	{
		private UIHelper _uiHelper;

		public GamemodeUIHelper(FLGTestRunner testRunner, UIHelper uiHelper) : base(testRunner)
		{
			_uiHelper = uiHelper;
		}



		public IEnumerator ClickCustomRoom()
		{
			yield return _uiHelper.WaitForPresenter2<GameModeScreenPresenter>(0.5f, 10);
			yield return _uiHelper.TouchOnElementByName("CustomGameButton");
		}

		public IEnumerator SelectGamemodeWithName(string name)
		{
			yield return _uiHelper.WaitForPresenter<RoomJoinCreateScreenPresenter>(0.5f, 10f);
			var el = _uiHelper.SearchForElementGlobally("GameMode");
			var dropdown = (LocalizedDropDown) el.Value.Item2;

			dropdown.index = 0;
			dropdown.value = name;
		}

		public IEnumerator ClickCreate()
		{
			yield return _uiHelper.WaitForPresenter<RoomJoinCreateScreenPresenter>(0.5f, 10f);
			yield return _uiHelper.TouchOnElementByName("CreateButton");
		}
	}
}