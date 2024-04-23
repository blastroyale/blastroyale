using System.Collections;
using FirstLight.Game.Services.RoomService;
using I2.Loc;

namespace FirstLight.Game.TestCases

{
	public class TwoMatchesInARow : PlayTestCase
	{
		public override void BeforeGameAwaken()
		{
			Account.FreshGameInstallation();
		}

		public override void AfterGameAwaken()
		{
			FeatureFlags.SetTutorial(false);
			PlayerConfigs.SetEnableFPSLimit(false);
			Account.FreshGameInstallation();
		}

		public override IEnumerator Run()
		{
			yield return UIGeneric.WaitForGenericInputDialogAndInput("Chupacabra", ScriptLocalization.UITHomeScreen.enter_your_name);
			yield return UIHome.WaitHomePresenter(60);

			for (int i = 0; i < 2; i++)
			{
				RoomService.AutoStartWhenLoaded = true;
				yield return GameConfigHelper.DecreaseMatchmakingTime();
				yield return Quantum.UseBotBehaviourForNextMatch();
				yield return Quantum.DecreaseCircleTimesForNextMatch();
				yield return UIHome.ClickPlayButton();
				yield return UIGame.WaitDropZoneSelectScreen();
				yield return Quantum.WaitForSimulationToStart();
				yield return UIGame.WaitForGameToEndAndGoToMenu(6 * 60);
			}
		}
	}
}