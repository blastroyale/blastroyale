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
			yield return _uiHelper.WaitForPresenter<PreGameLoadingScreenPresenter>();
		}


		public IEnumerator SelectWater()
		{
			var presenter = _uiHelper.GetPresenter<PreGameLoadingScreenPresenter>();
			presenter.SelectDropZone(0.9f, 0.5f);
			yield break;
		}

		public IEnumerator SelectPosition(float x, float y)
		{
			var presenter = _uiHelper.GetPresenter<PreGameLoadingScreenPresenter>();
			presenter.SelectDropZone(x, y);
			yield break;
		}

		public IEnumerator SelectRandomPosition()
		{
			yield return SelectPosition(Random.Range(0.2f, 0.8f), Random.Range(0.2f, 0.8f));
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
			if (screen is WinnerScreenPresenter)
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