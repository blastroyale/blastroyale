using System.Collections;
using FirstLight.Game.Presenters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using System.Linq;
using FirstLight.Game.Views;
using FirstLight.Game.Views.MainMenuViews;

namespace FirstLight.Tests.PlayTests
{
	[RequiresPlayMode]
	public class SmokeTests
	{
		[OneTimeSetUp]
		public void Setup()
		{
		}

		[UnityTest]
		public IEnumerator WhenEnterGameDieReturnToMainMenu_NoErrors()
		{
			yield return TestTools.LoadSceneAndWaitUntilDone("Boot");
			yield return FLGTestTools.WaitForMainMenu();

			//FLGTestTools.ClickCustomGameButton();
			//yield return FLGTestTools.WaitForCustomGameMenu();

			FLGTestTools.ClickCreateRoom();
			yield return FLGTestTools.WaitForMatchMakingScreen();
			FLGTestTools.ClickLockRoomAndPlay();
			FLGTestTools.SelectWaterPosition();
			yield return FLGTestTools.WaitForBRDeadScreenScreen();
			FLGTestTools.ClickDeadScreenLeave();
			yield return FLGTestTools.WaitForGameCompleteScreen();
			// Wait for Game Complete Screen continue button to appear
			var gameCompleteScreen = GameObject.FindObjectOfType<GameCompleteScreenPresenter>();
			yield return TestTools.UntilChildOfType<Button>(gameCompleteScreen.gameObject);
			FLGTestTools.ClickGameCompleteContinue();
			yield return FLGTestTools.WaitForResultsScreen();
			FLGTestTools.ClickResultsHome();
			yield return FLGTestTools.WaitForMainMenu();
		}

		[UnityTest]
		public IEnumerator CheckHeroes_NoErrors() 
		{
			yield return TestTools.LoadSceneAndWaitUntilDone("Boot");

			yield return FLGTestTools.WaitForMainMenu();
			//Heroes
			FLGTestTools.ClickHeroSelectionButton();
			yield return FLGTestTools.WaitForHeroesMenu();

			//Get a list of all heroes and order them
			var buttons = GameObject.FindObjectsOfType<PlayerSkinGridItemView>().ToList();
			buttons = buttons.FindAll(o => o.name.Contains("(Clone)"));
			buttons.Reverse();

			//Select any character that isn't #1
			FLGTestTools.ClickHeroButton(buttons[1].GetComponent<UiButtonView>());
			FLGTestTools.ClickSelectButton_OldUI();

			//Run through each hero and select
			foreach(var btn in buttons) {
				FLGTestTools.ClickHeroButton(btn.GetComponent<UiButtonView>());
				FLGTestTools.ClickSelectButton_OldUI();
				yield return new WaitForSeconds(1f);
			}
			FLGTestTools.ClickBackButton_OldUI();
		}
	}
}
