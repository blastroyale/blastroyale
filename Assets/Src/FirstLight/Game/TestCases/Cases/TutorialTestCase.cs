using System.Collections;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using I2.Loc;
using Quantum;
using Quantum.Commands;
using UnityEngine;

namespace FirstLight.Game.TestCases

{
	public class TutorialTestCase : PlayTestCase
	{
		private static readonly WaitForSeconds _tutorialTransitionWait = new(4f);
		private static readonly WaitForSeconds _oneSec = new(1f);

		public override void BeforeGameAwaken()
		{
			FeatureFlags.SetTutorial(true);
			PlayerConfigs.SetServerRegion("us");
			PlayerConfigs.SetEnableFPSLimit(false);
			Account.FreshGameInstallation();
		}

		public override IEnumerator Run()
		{
			yield return FinishInitialTutorialMatch();
			yield return UIGeneric.WaitForGenericInputDialogAndInput("Marvin", ScriptLocalization.UITHomeScreen.enter_your_name);
			yield return FinishBattlepassTutorial();
			yield return PlayCasualMatch();
		}

		private IEnumerator FinishInitialTutorialMatch()
		{
			yield return Quantum.WaitForSimulationAndRunReplay("tutorial");

			yield return WaitForTutorialState(TutorialFirstMatchStates.EnterKill2Bots, 30);
			yield return new WaitForSeconds(5f);

			yield return Quantum.SendCommand(new CheatKillAllTutorialBots() { BehaviourType = BotBehaviourType.Static });

			yield return WaitForTutorialState(TutorialFirstMatchStates.EnterKillFinalBot, 30);
			// Let the player fall
			yield return new WaitForSeconds(12f);
			yield return Quantum.StopInputManipulator();
			yield return Quantum.SendCommand(new CheatKillAllTutorialBots() { BehaviourType = BotBehaviourType.WanderAndShoot });
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

		private IEnumerator PlayCasualMatch()
		{
			yield return UIHome.ClickPlayButton();
			yield return _tutorialTransitionWait;
			yield return Quantum.UseBotBehaviourForNextMatch();
			yield return UIGame.WaitDropZoneSelectScreen();
			yield return UIGame.SelectRandomPosition();
			yield return UIGame.WaitForGameToEndAndGoToMenu(60 * 8);
		
		}
	}
}