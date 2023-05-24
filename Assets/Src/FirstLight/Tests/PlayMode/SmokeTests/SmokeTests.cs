using System;
using System.Collections;
using System.IO;
using FirstLight.Game.Presenters;
using FirstLight.Game.TestCases;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.TestTools;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FirstLight.Tests.PlayTests
{
	[RequiresPlayMode, Category("PlayMode")]
	public class SmokeTests : ITestPlayerBuildModifier
	{
		private static IEnumerator Fail(string reason)
		{
			Debug.LogError(reason);
			yield return new ExitPlayMode();
		}

		[OneTimeSetUp]
		public void Setup()
		{
		}


		[UnityTest]
		[TestCase(typeof(TutorialTestCase), ExpectedResult = null)]
		[Timeout(1000 * 60 * 10)]
		public IEnumerator RunCase(Type testCase)
		{
			MainInstaller.Clean();
			var runner = FLGTestRunner.Instance;
			runner.FailInstruction = Fail;
			var instance = Activator.CreateInstance(testCase);
			yield return runner.RunInUnitTest((PlayTestCase)instance);
			yield return new ExitPlayMode();
		}
		


		public IEnumerator WhenEnterGameDieReturnToMainMenu_NoErrors()
		{
			var wait1Sec = new WaitForSeconds(1);

			yield return TestTools.LoadSceneAndWaitUntilDone("Boot");

			yield return FLGTestTools.WaitForPresenter<HomeScreenPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickGameModeSelectionButton();

			yield return FLGTestTools.WaitForPresenter<GameModeSelectionPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickCustomGameButton();

			yield return FLGTestTools.WaitForPresenter<RoomJoinCreateScreenPresenter>();

			FLGTestTools.ClickCreateRoom();

			yield return FLGTestTools.WaitForPresenter<CustomLobbyScreenPresenter>();

			FLGTestTools.ClickLockRoomAndPlay();

			FLGTestTools.SelectWaterPosition();

			yield return FLGTestTools.WaitForPresenter<MatchEndScreenPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickNextButton<MatchEndScreenPresenter>();

			yield return FLGTestTools.WaitForPresenter<SpectateScreenPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickLeaveButton<SpectateScreenPresenter>();

			yield return FLGTestTools.WaitForPresenter<LeaderboardAndRewardsScreenPresenter>();

			yield return new WaitForSeconds(2);

			FLGTestTools.ClickNextButton<LeaderboardAndRewardsScreenPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickNextButton<LeaderboardAndRewardsScreenPresenter>();

			yield return FLGTestTools.WaitForPresenter<HomeScreenPresenter>();
		}

		public IEnumerator CheckEquipment_NoErrors()
		{
			var wait1Sec = new WaitForSeconds(1);

			yield return TestTools.LoadSceneAndWaitUntilDone("Boot");

			yield return FLGTestTools.WaitForPresenter<HomeScreenPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentButton();

			yield return FLGTestTools.WaitForPresenter<EquipmentPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickWeaponCategory();

			yield return FLGTestTools.WaitForPresenter<EquipmentSelectionPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot2();

			yield return wait1Sec;

			FLGTestTools.ClickEquipButton();

			yield return wait1Sec;

			FLGTestTools.CLickBackButton();

			yield return FLGTestTools.WaitForPresenter<EquipmentPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickHelmetCategory();

			yield return FLGTestTools.WaitForPresenter<EquipmentSelectionPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot2();

			yield return wait1Sec;

			FLGTestTools.ClickEquipButton();

			yield return wait1Sec;

			FLGTestTools.CLickBackButton();

			yield return FLGTestTools.WaitForPresenter<EquipmentPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickShieldCategory();

			yield return FLGTestTools.WaitForPresenter<EquipmentSelectionPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot2();

			yield return wait1Sec;

			FLGTestTools.ClickEquipButton();

			yield return wait1Sec;

			FLGTestTools.CLickBackButton();

			yield return FLGTestTools.WaitForPresenter<EquipmentPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickAmuletCategory();

			yield return FLGTestTools.WaitForPresenter<EquipmentSelectionPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot2();

			yield return wait1Sec;

			FLGTestTools.ClickEquipButton();

			yield return wait1Sec;

			FLGTestTools.CLickBackButton();

			yield return FLGTestTools.WaitForPresenter<EquipmentPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickArmorCategory();

			yield return FLGTestTools.WaitForPresenter<EquipmentSelectionPresenter>();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot();

			yield return wait1Sec;

			FLGTestTools.ClickEquipmentSlot2();

			yield return wait1Sec;

			FLGTestTools.ClickEquipButton();

			yield return wait1Sec;

			FLGTestTools.ClickHomeButton();

			yield return FLGTestTools.WaitForPresenter<HomeScreenPresenter>();

			yield return wait1Sec;
		}

		public BuildPlayerOptions ModifyOptions(BuildPlayerOptions playerOptions)
		{
			playerOptions.scenes = new string[] { };
			return playerOptions;
		}
	}
}