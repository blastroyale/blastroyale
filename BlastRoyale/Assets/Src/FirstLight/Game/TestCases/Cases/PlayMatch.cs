using System.Collections;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;

namespace FirstLight.Game.TestCases

{
	public class PlayMatch : PlayTestCase
	{
		private int _matches;
		private bool _speedUp;
		private GameId _map;

		public PlayMatch(int matches, bool speedUp = true, GameId map = GameId.IslandOne)
		{
			_matches = matches;
			_speedUp = speedUp;
			_map = map;
		}


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
			yield return UIHome.WaitHomePresenter(60);

			for (int i = 0; i < _matches; i++)
			{
				if (_speedUp)
				{
					RoomService.AutoStartWhenLoaded = true;
					yield return GameConfigHelper.DecreaseMatchmakingTime();
					yield return Quantum.DecreaseCircleTimesForNextMatch();
				}
				
				MainInstaller.ResolveServices().GameModeService.SelectedMap = _map;
				
				yield return Quantum.UseBotBehaviourForNextMatch();
				yield return UIHome.ClickPlayButton();
				yield return UIGame.WaitDropZoneSelectScreen();
				yield return Quantum.WaitForSimulationToStart();
				yield return UIGame.WaitForGameToEndAndGoToMenu(6 * 60);
			}
		}
	}
}