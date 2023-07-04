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
	public class TenMatchesInARow : PlayTestCase
	{
		public override void OnGameAwaken()
		{
			FeatureFlags.SetTutorial(false);
			Account.FreshGameInstallation();
		}

		public override IEnumerator Run()
		{
			
			yield return UIHome.WaitHomePresenter(60);
			yield return UIGeneric.WaitForGenericInputDialogAndInput("Chupacabra", ScriptLocalization.UITHomeScreen.enter_your_name);


			for (int i = 0; i < 10; i++)
			{
				yield return UIHome.WaitHomePresenter(10);
				yield return Quantum.UseBotBehaviourForNextMatch();
				yield return UIHome.ClickPlayButton();
				yield return UIGame.WaitDropZoneSelectScreen();
				yield return UIGame.SelectPosition(0.6f, 0.6f);
				yield return Quantum.WaitForSimulationToStart();
				yield return UIGame.WaitForGameToEndAndGoToMenu(6 * 60);

			}
		}
	}
}