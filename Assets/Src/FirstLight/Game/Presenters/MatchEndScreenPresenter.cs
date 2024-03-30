using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This screen is shown when a player is killed / the match ends.
	/// </summary>
	public class MatchEndScreenPresenter : UIPresenterData<MatchEndScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action OnTimeToLeave;
		}

		[SerializeField, Required] private PlayableDirector _winDirector;
		[SerializeField, Required] private PlayableDirector _blastedDirector;

		private VisualElement _matchEndTitle;
		private VisualElement _blastedTitle;
		private VisualElement _bustedTitle;
		private VisualElement _youWinTitle;
		private VisualElement _youChoseDeathTitle;

		private IMatchServices _matchServices;

		private float _waitTime = 2f;

		private void Awake()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
		}

		protected override void QueryElements()
		{
			_matchEndTitle = Root.Q<VisualElement>("MatchEndedTitle").Required();
			_blastedTitle = Root.Q<VisualElement>("BlastedTitle").Required();
			_bustedTitle = Root.Q<VisualElement>("BustedTitle").Required();
			_youWinTitle = Root.Q<VisualElement>("YouWinTitle").Required();
			_youChoseDeathTitle = Root.Q<VisualElement>("YouChoseDeathTitle").Required();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_matchEndTitle.SetDisplay(false);
			_youChoseDeathTitle.SetDisplay(false);
			_bustedTitle.SetDisplay(false);
			_waitTime = 2f;

			if (!QuantumRunner.Default.IsDefinedAndRunning())
			{
				Data.OnTimeToLeave?.Invoke();
				return base.OnScreenOpen(reload); // reconnection edge case to avoid soft-lock
			}

			var game = QuantumRunner.Default.Game;
			var gameOver = game.IsGameOver();
			var playersData = game.GeneratePlayersMatchDataLocal(out var leader, out var localWinner);


			var localPlayerRef = game.GetLocalPlayerRef();
			var localPlayer = localPlayerRef == PlayerRef.None ? playersData[leader] : playersData[localPlayerRef];
			if (localWinner && gameOver)
			{
				_winDirector.Play();
				_waitTime = (float) _winDirector.duration;
			}
			else if (gameOver)
			{
				_matchEndTitle.SetDisplay(true);
			}
			else if (_matchServices.MatchEndDataService.DiedFromRoofDamage)
			{
				_bustedTitle.SetDisplay(true);
			}
			else if (_matchServices.MatchEndDataService.LocalPlayerKiller == localPlayer.Data.Player)
			{
				_youChoseDeathTitle.SetDisplay(true);
			}
			else
			{
				_blastedDirector.Play();
				_waitTime = (float) _blastedDirector.duration;
			}

			StartCoroutine(WaitToLeave());
			
			return base.OnScreenOpen(reload);
		}

		private IEnumerator WaitToLeave()
		{
			yield return new WaitForSeconds(_waitTime);
			Data.OnTimeToLeave?.Invoke();
		}
	}
}