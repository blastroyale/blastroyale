using System;
using System.Collections;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine;
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
			public Action OnTimeToLeave;
		}

		private VisualElement _matchEndTitle;
		private VisualElement _blastedTitle;
		private VisualElement _youWinTitle;
		private VisualElement _youChoseDeathTitle;
		
		private IMatchServices _matchServices;

		private void Awake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}
		
		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_matchEndTitle = root.Q<VisualElement>("MatchEndedTitle").Required();
			_blastedTitle = root.Q<VisualElement>("BlastedTitle").Required();
			_youWinTitle = root.Q<VisualElement>("YouWinTitle").Required();
			_youChoseDeathTitle = root.Q<VisualElement>("YouChoseDeathTitle").Required();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var container = f.GetSingleton<GameContainer>();
			var playersData = container.GeneratePlayersMatchData(f, out var leader);
			var localWinner = game.PlayerIsLocal(leader);
			var localPlayer = playersData[game.GetLocalPlayerRef()];
			var playerDead = localPlayer.Data.Entity.IsAlive(f);

			_matchEndTitle.SetDisplay(false);
			_blastedTitle.SetDisplay(false);
			_youWinTitle.SetDisplay(false);
			_youChoseDeathTitle.SetDisplay(false);
			
			if (localWinner)
			{
				_youWinTitle.SetDisplay(true);
			}
			else if (_matchServices.MatchEndDataService.LocalPlayerKiller == localPlayer.Data.Player)
			{
				_youChoseDeathTitle.SetDisplay(true);
			}
			else if (_matchServices.MatchEndDataService.LocalPlayerKiller != PlayerRef.None || playerDead)
			{
				_blastedTitle.SetDisplay(true);
			}
			else
			{
				_matchEndTitle.SetDisplay(true);
			}

			StartCoroutine(WaitToLeave());
		}

		private IEnumerator WaitToLeave()
		{
			yield return new WaitForSeconds(2);
			Data.OnTimeToLeave?.Invoke();
		}
	}
}