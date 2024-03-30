using System;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Timeline;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using Quantum;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter shows the match winner.
	/// </summary>
	public class WinnerScreenPresenter : UIPresenterData<WinnerScreenPresenter.StateData>, INotificationReceiver
	{
		public class StateData
		{
			public Action ContinueClicked;
		}

		private EntityRef _playerWinnerEntity;
		private IMatchServices _matchService;
		private IGameServices _services;

		private VisualElement _winnerBanner;
		private Label _nameLabel;
		private Button _nextButton;

		private Transform _entityViewTransform;

		private bool _isSpectator;

		private void Awake()
		{
			_matchService = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_nameLabel = Root.Q<Label>("NameLabel").Required();
			_winnerBanner = Root.Q("WinnerBanner");

			Root.Q<LocalizedButton>("NextButton").clicked += OnNextClicked;
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			if (!QuantumRunner.Default.IsDefinedAndRunning())
			{
				FLog.Error("Screen was about to read simulation data while it's not in memory.");
				return base.OnScreenOpen(reload);
			}

			var game = QuantumRunner.Default.Game;
			var playerData = game.GeneratePlayersMatchDataLocal(out var leader, out var localWinner);
			var playerWinner = localWinner ? playerData[game.GetLocalPlayerRef()] : playerData[leader];

			if (playerWinner.Data.IsValid)
			{
				_playerWinnerEntity = playerWinner.Data.Entity;
				_nameLabel.text = playerWinner.GetPlayerName();

				var nameColor = _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked, (int) playerWinner.LeaderboardRank);
				_nameLabel.style.color = nameColor;
			}
			else
			{
				_nameLabel.text = "No one"; // TODO: Localize!!!!
			}

			_winnerBanner.SetDisplay(_services.TutorialService.CurrentRunningTutorial.Value != TutorialSection.FIRST_GUIDE_MATCH);

			if (_matchService.EntityViewUpdaterService.TryGetView(_playerWinnerEntity, out var entityView))
			{
				_entityViewTransform = entityView.transform;

				_services.MessageBrokerService.Publish(new WinnerSetCameraMessage {WinnerTrasform = _entityViewTransform});
			}

			return base.OnScreenOpen(reload);
		}

		public void OnNotify(Playable origin, INotification notification, object context)
		{
			var playVfxMarker = notification as PlayVfxMarker;

			if (playVfxMarker != null && !_entityViewTransform.IsDestroyed())
			{
				_services.VfxService.Spawn(playVfxMarker.Vfx).transform.position = _entityViewTransform.position;
			}
		}

		private void OnNextClicked()
		{
			Data.ContinueClicked.Invoke();
		}
	}
}