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
			yield return _uiHelper.WaitForPresenter2<PreGameLoadingScreenPresenter>();
		}

		public IEnumerator SelectPosition(float x, float y)
		{
			var presenter = _uiHelper.GetPresenter2<PreGameLoadingScreenPresenter>();
			presenter.SelectDropZone(x, y);
			yield break;
		}

		public IEnumerator SelectRandomPosition()
		{
			yield return SelectPosition(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
		}


		public IEnumerator WaitForSpectateScreen()
		{
			yield return _uiHelper.WaitForPresenter2<SpectateScreenPresenter>();
		}

		public IEnumerator WaitForLeaderboardAndRewards()
		{
			yield return _uiHelper.WaitForPresenter2<LeaderboardAndRewardsScreenPresenter>();
		}

		public IEnumerator WaitForGameToEndAndGoToMenu(float gameTimeout = 60 * 5)
		{
			var oneSec = new WaitForSeconds(1);
			yield return WaitForEndGameScreen(gameTimeout);
			var possibleScreens = new[] { typeof(SpectateScreenPresenter), typeof(WinnerScreenPresenter) };

			yield return _uiHelper.WaitForAny(possibleScreens);
			var screen = _uiHelper.GetFirstOpenScreen(possibleScreens);

			if (screen is SpectateScreenPresenter)
			{
				yield return new WaitForSeconds(4);
				yield return LeaveSpectator();
			}
			else
			{
				yield return oneSec;
				yield return _uiHelper.ClickNextButton();
			}

			possibleScreens = new[] { typeof(WinnersScreenPresenter), typeof(LeaderboardAndRewardsScreenPresenter) };

			yield return _uiHelper.WaitForAny(possibleScreens);
			screen = _uiHelper.GetFirstOpenScreen(possibleScreens);
			yield return oneSec;
			if (screen is WinnersScreenPresenter)
			{
				yield return _uiHelper.ClickNextButton();

				yield return WaitForLeaderboardAndRewards();
				yield return oneSec;
			}

			yield return _uiHelper.ClickNextButton();
			yield return new WaitForSeconds(2);
			yield return _uiHelper.ClickNextButton();
		}

		public IEnumerator WaitForEndGameScreen(float timeout = 60)
		{
			yield return _uiHelper.WaitForPresenter2<MatchEndScreenPresenter>(0.5f, timeout);
		}

		public IEnumerator WaitForWinnerScreen()
		{
			yield return _uiHelper.WaitForPresenter2<WinnerScreenPresenter>();
		}

		public IEnumerator LeaveSpectator()
		{
			yield return WaitForSpectateScreen();
			yield return _uiHelper.TouchOnElementByName("LeaveButton");
		}
	}
}