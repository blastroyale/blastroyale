using System;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Shows the matchmaking status on the HomeScreen
	/// </summary>
	public class MatchmakingStatusView : UIView
	{
		private const string UssContainerHidden = "matchmaking-container--hidden";

		private InGameNotificationService _inGameNotificationService;
		private IGameNetworkService _gameNetworkService;
		private LocalizedLabel _matchmakingText;
		private Label _timeLabel;
		private ImageButton _closeButton;

		private long _startTime;

		public event Action CloseClicked;

		protected override void Attached()
		{
			var services = MainInstaller.ResolveServices();
			_gameNetworkService = services.NetworkService;
			_inGameNotificationService = services.InGameNotificationService;
			_timeLabel = Element.Q<Label>("Time").Required();
			_matchmakingText = Element.Q<LocalizedLabel>("MatchmakingText").Required();
			_closeButton = Element.Q<ImageButton>("MatchmakingCloseButton").Required();
			_closeButton.clicked += () => CloseClicked?.Invoke();
			_gameNetworkService.LastUsedSetup.InvokeObserve(OnLastRoomSetupUpdate);
		}

		public override void OnScreenOpen(bool reload)
		{
			var services = MainInstaller.ResolveServices();
			services.MessageBrokerService.Subscribe<MatchmakingLeftMessage>(OnMatchmakingLeft);
		}

		public override void OnScreenClose()
		{
			var services = MainInstaller.ResolveServices();
			services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OnMatchmakingLeft(MatchmakingLeftMessage msg)
		{
			if (msg.Error)
			{
				_inGameNotificationService.QueueNotification(msg.Reason, InGameNotificationStyle.Error, InGameNotificationDuration.Long);
			}
		}

		private void OnLastRoomSetupUpdate(MatchRoomSetup _, MatchRoomSetup setup)
		{
			if (setup != null)
			{
				if (setup.SimulationConfig.MatchType == MatchType.Matchmaking)
				{
					_matchmakingText.Localize(ScriptTerms.UITHomeScreen.matchmaking);
					_closeButton.SetDisplay(true);
					return;
				}
			}

			_closeButton.SetDisplay(false);
			_matchmakingText.Localize(ScriptTerms.UITHomeScreen.joining);
		}

		public void Show(bool show)
		{
			Element.EnableInClassList(UssContainerHidden, !show);
			if (!show) return;

			_startTime = 0;
			_timeLabel.schedule.Execute(ts =>
			{
				if (_startTime == 0L) _startTime = ts.now;

				_timeLabel.text = $"{TimeSpan.FromMilliseconds(_startTime - ts.start):mm\\:ss}";
			}).Every(200); // Triggered every 200ms so the second counting is smooth and consistent.
		}
	}
}