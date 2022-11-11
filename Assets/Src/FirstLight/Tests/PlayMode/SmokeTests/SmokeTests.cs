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
			yield return new WaitForSeconds(1f);
			yield return FLGTestTools.WaitForMainMenu();
			yield return new WaitForSeconds(1f);
			FLGTestTools.ClickCustomGameButton();
			yield return new WaitForSeconds(1f);
			yield return FLGTestTools.WaitForCustomGameMenu();
			yield return new WaitForSeconds(1f);
			FLGTestTools.ClickCreateRoom();
			yield return new WaitForSeconds(1f);
			yield return FLGTestTools.WaitForMatchMakingScreen();
			yield return new WaitForSeconds(1f);
			FLGTestTools.ClickLockRoomAndPlay();
			yield return new WaitForSeconds(1f);
			FLGTestTools.SelectWaterPosition();
			yield return new WaitForSeconds(1f);
			yield return FLGTestTools.WaitForBRDeadScreenScreen();
			yield return new WaitForSeconds(1f);
			FLGTestTools.ClickDeadScreenLeave();
			yield return new WaitForSeconds(1f);
			yield return FLGTestTools.WaitForGameCompleteScreen();
			// Wait for Game Complete Screen continue button to appear
			var gameCompleteScreen = GameObject.FindObjectOfType<GameCompleteScreenPresenter>();
			yield return TestTools.UntilChildOfType<Button>(gameCompleteScreen.gameObject);
			yield return new WaitForSeconds(1f);
			FLGTestTools.ClickGameCompleteContinue();
			yield return new WaitForSeconds(1f);
			yield return FLGTestTools.WaitForResultsScreen();
			yield return new WaitForSeconds(1f);
			FLGTestTools.ClickResultsHome();
			yield return new WaitForSeconds(1f);
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
