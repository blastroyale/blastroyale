using System.Collections;
using FirstLight.Game.Presenters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

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

			FLGTestTools.ClickCustomGameButton();
			
			yield return FLGTestTools.WaitForCustomGameMenu();

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

		
	}
}
