using System.Collections;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.TestCases.Helpers;
using I2.Loc;
using Photon.Deterministic;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using Application = UnityEngine.Device.Application;

namespace FirstLight.Game.TestCases

{
	public class TutorialTestCase : PlayTestCase
	{
		private static readonly WaitForSeconds _tutorialTransitionWait = new(4f);
		private static readonly WaitForSeconds _oneSec = new(1f);

		public override void OnGameAwaken()
		{
			FeatureFlags.SetTutorial(true);
			PlayerConfigs.SetTargetServer("us");
			PlayerConfigs.SetFpsTarget(FpsTarget.Unlimited);
			Account.FreshGameInstallation();
		}

		public override IEnumerator Run()
		{
			yield return FinishInitialTutorialMatch();
			yield return UIGeneric.WaitForGenericInputDialogAndInput("Marvin", ScriptLocalization.UITHomeScreen.enter_your_name);
			yield return FinishBattlepassTutorial();
			yield return EquipTutorialItem();
			yield return PlayCasualMatch();
		}

		private IEnumerator FinishInitialTutorialMatch()
		{
			yield return Quantum.WaitForSimulationAndRunReplay("tutorial");

			yield return WaitForTutorialState(TutorialFirstMatchStates.EnterKill2Bots, 30);
			yield return new WaitForSeconds(5f);

			yield return Quantum.SendCommand(new CheatKillAllTutorialBots() { BehaviourType = BotBehaviourType.Dumb });

			yield return WaitForTutorialState(TutorialFirstMatchStates.EnterKillFinalBot, 30);
			// Let the player fall
			yield return new WaitForSeconds(12f);
			yield return Quantum.StopInputManipulator();
			yield return Quantum.SendCommand(new CheatKillAllTutorialBots() { BehaviourType = BotBehaviourType.Aggressive });
			yield return Quantum.WaitForGameToFinish();
			yield return UIGame.WaitForEndGameScreen();
			yield return UIGame.WaitForWinnerScreen();

			yield return UIGeneric.ClickNextButton();
		}

		private IEnumerator WaitForTutorialState(TutorialFirstMatchStates state, float timeout)
		{
			yield return MessageBroker.WaitForMessage<AdvancedFirstMatchMessage>(
				m => m.State == state,
				timeout
			);
		}

		private IEnumerator FinishBattlepassTutorial()
		{
			// Claim battle pass
			yield return _tutorialTransitionWait;
			yield return UIHome.ClickBattlePassButton();
			yield return _tutorialTransitionWait;
			yield return UIBattlepass.ClickToClaimFirstBattlePassReward();
			yield return UIBattlepass.WaitRewardDialogAndClaimIt();

			// Wait for automatic transitions and equipment tooltip
			yield return new WaitForSeconds(3);
		}

		private IEnumerator EquipTutorialItem()
		{
			yield return UIHome.ClickEquipmentButton();
			yield return _tutorialTransitionWait;
			yield return UIEquipment.OpenEquipmentSlot(GameIdGroup.Weapon);
			yield return _tutorialTransitionWait;
			var onlyEquipment = DataProvider.EquipmentDataProvider.GetInventoryEquipmentInfo(EquipmentFilter.All).First().Id;
			yield return UIEquipment.SelectEquipmentAtSelectionScreen(onlyEquipment);
			yield return _tutorialTransitionWait;
			yield return UIEquipment.ClickEquipButton();
			yield return _tutorialTransitionWait;
		}

		private IEnumerator PlayCasualMatch()
		{
			yield return UIHome.ClickPlayButton();
			yield return _tutorialTransitionWait;
			yield return Quantum.SetInputManipulator(new FixedManipulator(FPVector2.Right, 100));
			yield return UIGame.WaitDropZoneSelectScreen();
			yield return UIGame.SelectPosition(0.6f, 0.6f);


			yield return UIGame.WaitForEndGameScreen(60 * 8);
			yield return UIGame.WaitForSpectateScreen();
			yield return _oneSec;
			yield return UIGame.LeaveSpectator();
			yield return Quantum.WaitForGameToFinish();
			yield return _oneSec;
			yield return UIGame.EndOfGameFlow();
		}
	}
}