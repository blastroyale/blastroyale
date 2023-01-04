using System.Collections;
using FirstLight.Game.Presenters;
using FirstLight.Game.UIElements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

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

			yield return new WaitForSeconds(2);
			FLGTestTools.ClickGameModeSelectionButton();
			
			yield return FLGTestTools.WaitForGameModeSelectionScreen();
			yield return new WaitForSeconds(2);
			FLGTestTools.ClickCustomGameButton();

			yield return FLGTestTools.WaitForCustomGameMenu();
			
			FLGTestTools.ClickCreateRoom();

			yield return FLGTestTools.WaitForMatchMakingScreen();

			FLGTestTools.ClickLockRoomAndPlay();

			FLGTestTools.SelectWaterPosition();

			yield return FLGTestTools.WaitForMatchEndScreen();

			FLGTestTools.ClickNextButton<MatchEndScreenPresenter>();

			yield return FLGTestTools.WaitForMainMenu();
		}

		[UnityTest]
		public IEnumerator CheckEquipment_NoErrors() 
		{
			yield return TestTools.LoadSceneAndWaitUntilDone("Boot");
			yield return FLGTestTools.WaitForMainMenu();

			TestTools.ClickUIToolKitButton<Button>(TestTools.GetUIDocument<HomeScreenPresenter>(),"EquipmentButton");
			TestTools.ClickUIToolKitButton<Button>(TestTools.GetUIDocument<EquipmentPresenter>(),"WeaponCategory");
			TestTools.ClickUIToolKitButton<ImageButton>(TestTools.GetUIDocument<EquipmentSelectionPresenter>(),"back");
		}
	}
}