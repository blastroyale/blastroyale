using System;
using FirstLight.FLogger;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This screen is shown when a player is killed / the match ends.
	/// </summary>
	public class MatchEndScreenPresenter : UiToolkitPresenterData<MatchEndScreenPresenter.StateData>
	{
		public struct StateData
		{
			public PlayerRef Killer;
			public Action OnNextClicked;
		}

		private VisualElement _matchEndTitle;
		private VisualElement _blastedTitle;
		private VisualElement _youWinTitle;
		private VisualElement _youChoseDeathTitle;

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_matchEndTitle = root.Q<VisualElement>("MatchEndedTitle").Required();
			_blastedTitle = root.Q<VisualElement>("BlastedTitle").Required();
			_youWinTitle = root.Q<VisualElement>("YouWinTitle").Required();
			_youChoseDeathTitle = root.Q<VisualElement>("YouChoseDeathTitle").Required();

			root.Q<LocalizedButton>("NextButton").clicked += Data.OnNextClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var container = f.GetSingleton<GameContainer>();
			var localPlayer = (PlayerRef) game.GetLocalPlayers()[0];
			container.GetPlayersMatchData(f, out var leader);
			var localWinner = game.PlayerIsLocal(leader);

			if (localWinner)
			{
				_matchEndTitle.SetDisplay(false);
				_blastedTitle.SetDisplay(false);
				_youWinTitle.SetDisplay(true);
				_youChoseDeathTitle.SetDisplay(false);
			}
			else if (Data.Killer == PlayerRef.None)
			{
				_matchEndTitle.SetDisplay(true);
				_blastedTitle.SetDisplay(false);
				_youWinTitle.SetDisplay(false);
				_youChoseDeathTitle.SetDisplay(false);
			}
			else if (Data.Killer == localPlayer)
			{
				_matchEndTitle.SetDisplay(false);
				_blastedTitle.SetDisplay(false);
				_youWinTitle.SetDisplay(false);
				_youChoseDeathTitle.SetDisplay(true);
			}
			else
			{
				_matchEndTitle.SetDisplay(false);
				_blastedTitle.SetDisplay(true);
				_youWinTitle.SetDisplay(false);
				_youChoseDeathTitle.SetDisplay(false);
			}
		}
	}
}