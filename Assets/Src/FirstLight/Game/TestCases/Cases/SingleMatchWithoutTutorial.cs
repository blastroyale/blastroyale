using System.Collections;
using System.Linq;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.TestCases.Helpers;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace FirstLight.Game.TestCases

{
	public class SingleMatchWithoutTutorial : PlayTestCase
	{
		public override void OnGameAwaken()
		{
			FeatureFlags.SetTutorial(false);
			Account.FreshGameInstallation();
		}

		public override IEnumerator Run()
		{
			var oneSec = new WaitForSeconds(1);


			yield return UIHome.WaitHomePresenter(60);

			yield return UIGeneric.WaitForGenericInputDialogAndInput("Chupacabra", ScriptLocalization.UITHomeScreen.enter_your_name);

			yield return UIHome.ClickPlayButton();
			yield return UIGame.WaitDropZoneSelectScreen();
			yield return UIGame.SelectPosition(0.6f, 0.6f);
			yield return Quantum.WaitForSimulationToStart();

			yield return new WaitForSeconds(8);
			yield return Quantum.SetInputManipulator(new FixedManipulator(new FPVector2(FP.Minus_1, FP.Minus_1 * FP._0_75).Normalized, 100));


			yield return UIGame.WaitForEndGameScreen(6 * 60);
			yield return oneSec;
			yield return UIGame.WaitForSpectateScreen();
			yield return new WaitForSeconds(4);
			yield return UIGame.LeaveSpectator();
			yield return oneSec;
			yield return UIGame.EndOfGameFlow();
			yield return UIHome.WaitHomePresenter();
			yield return new WaitForSeconds(10);
		}
	}
}