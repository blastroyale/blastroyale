using System.Collections;
using FirstLight.Game.Configs;
using FirstLight.Game.Presenters;
using UnityEngine;

namespace FirstLight.Game.TestCases.Helpers
{
	public class GameUIHelper : TestHelper
	{
		private UIHelper _uiHelper;

		public GameUIHelper(FLGTestRunner testRunner, UIHelper uiHelper) : base(testRunner)
		{
			_uiHelper = uiHelper;
		}
		

		public IEnumerator WaitDropZoneSelectScreen()
		{
			yield return _uiHelper.WaitForPresenter<MatchmakingScreenPresenter>();
		}
		

		public IEnumerator SelectWater()
		{
			var presenter = _uiHelper.GetPresenter<MatchmakingScreenPresenter>();
			presenter.SelectDropZone(0.9f,0.5f);
			yield break;
		}
		public IEnumerator SelectPosition(float x, float y)
		{
			var presenter = _uiHelper.GetPresenter<MatchmakingScreenPresenter>();
			presenter.SelectDropZone(x,y);
			yield break;
		}
		public IEnumerator SelectRandomPosition()
		{
			yield return SelectPosition(Random.Range(0.1f,0.9f),Random.Range(0.1f,0.9f));
		}


		public IEnumerator WaitForSpectateScreen()
		{
			yield return _uiHelper.WaitForPresenter<SpectateScreenPresenter>();
		}

		public IEnumerator WaitForWinnersScreen()
		{
			yield return _uiHelper.WaitForPresenter<WinnersScreenPresenter>();
		}

		public IEnumerator WaitForLeaderboardAndRewards()
		{
			yield return _uiHelper.WaitForPresenter<LeaderboardAndRewardsScreenPresenter>();
		}

		public IEnumerator EndOfGameFlow(bool earlyExit = true)
		{
			var oneSec = new WaitForSeconds(1);

			if (!earlyExit)
			{
				yield return WaitForWinnersScreen();
				yield return oneSec;
				yield return _uiHelper.ClickNextButton();
			}

	
			yield return WaitForLeaderboardAndRewards();
			yield return oneSec;
			yield return _uiHelper.ClickNextButton();
			yield return new WaitForSeconds(2);
			yield return _uiHelper.ClickNextButton();
		}

		public IEnumerator WaitForEndGameScreen(float timeout = 60)
		{
			yield return _uiHelper.WaitForPresenter<MatchEndScreenPresenter>(0.5f, timeout);
		}

		public IEnumerator WaitForWinnerScreen()
		{
			yield return _uiHelper.WaitForPresenter<WinnerScreenPresenter>();
		}

		public IEnumerator LeaveSpectator()
		{
			yield return WaitForSpectateScreen();
			yield return _uiHelper.TouchOnElementByName("LeaveButton");
		}
	}
}