using System.Collections;
using System.Linq;
using FirstLight.Game.Presenters;
using FirstLight.Game.Views;
using FirstLight.Game.Views.MainMenuViews;
using FirstLight.UiService;
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

			yield return new WaitForSeconds(1);
			FLGTestTools.ClickGameModeSelectionButton();
			
			yield return FLGTestTools.WaitForGameModeSelectionScreen();

			FLGTestTools.ClickCreateRoom();

			yield return FLGTestTools.WaitForMatchMakingScreen();

			FLGTestTools.ClickLockRoomAndPlay();

			FLGTestTools.SelectWaterPosition();

			yield return FLGTestTools.WaitForBRDeadScreenScreen();

			FLGTestTools.ClickDeadScreenLeave();
			
			yield return FLGTestTools.WaitForGameCompleteScreen();

			var gameCompleteScreen = GameObject.FindObjectOfType<WinnerScreenPresenter>();
			yield return TestTools.UntilChildOfType<Button>(gameCompleteScreen.gameObject);

			FLGTestTools.ClickGameCompleteContinue();

			yield return FLGTestTools.WaitForResultsScreen();

			FLGTestTools.ClickResultsHome();
			
			yield return FLGTestTools.WaitForMainMenu();
		}

		[UnityTest]
		public IEnumerator CheckEquipment_NoErrors() 
		{
			yield return TestTools.LoadSceneAndWaitUntilDone("Boot");
			yield return FLGTestTools.WaitForMainMenu();

			TestTools.ClickUIToolKitButton(TestTools.GetUIDocument<HomeScreenPresenter>(),"EquipmentButton");
			TestTools.ClickUIToolKitButton(TestTools.GetUIDocument<EquipmentPresenter>(),"WeaponCategory");
			TestTools.ClickUIToolKitImageButton(TestTools.GetUIDocument<EquipmentSelectionPresenter>(),"back");
		}
	}
}