using System;
using Cinemachine;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Timeline;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter shows the match winner.
	/// </summary>
	public class WinnerScreenPresenter : UiToolkitPresenterData<WinnerScreenPresenter.StateData>, INotificationReceiver
	{
		public struct StateData
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

		protected override void QueryElements(VisualElement root)
		{
			_nameLabel = root.Q<Label>("NameLabel").Required();
			_winnerBanner = root.Q("WinnerBanner");

			root.Q<LocalizedButton>("NextButton").clicked += OnNextClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			if (!QuantumRunner.Default.IsDefinedAndRunning())
			{
				FLog.Error("Screen was about to read simulation data while it's not in memory.");
				return;
			}
			var game = QuantumRunner.Default.Game;
			var playerData = game.GeneratePlayersMatchDataLocal(out var leader, out var localWinner);
			var playerWinner = localWinner ? playerData[game.GetLocalPlayerRef()] : playerData[leader];

			if (playerWinner.Data.IsValid)
			{
				_playerWinnerEntity = playerWinner.Data.Entity;
				_nameLabel.text = playerWinner.GetPlayerName();
				
				var nameColor = _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked, (int)playerWinner.LeaderboardRank);
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
