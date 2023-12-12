using System;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.UiService;
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

		private IGameNetworkService _gameNetworkService;
		private IConfigsProvider _configsProvider;
		private bool _shouldUseMatchmaking;
		private LocalizedLabel _matchmakingText;
		private Label _timeLabel;
		private ImageButton _closeButton;

		private long _startTime;

		public event Action CloseClicked;


		public override void Attached(VisualElement element)
		{
			base.Attached(element);
			var services = MainInstaller.ResolveServices();
			_gameNetworkService = services.NetworkService;
			_configsProvider = services.ConfigsProvider;
			_timeLabel = element.Q<Label>("Time").Required();
			_matchmakingText = element.Q<LocalizedLabel>("MatchmakingText").Required();
			_closeButton = element.Q<ImageButton>("MatchmakingCloseButton").Required();
			_closeButton.clicked += () => CloseClicked?.Invoke();
			_gameNetworkService.LastUsedSetup.InvokeObserve(OnLastRoomSetupUpdate);
		}

		private void OnLastRoomSetupUpdate(MatchRoomSetup _, MatchRoomSetup setup)
		{
			if (setup != null)
			{
				var gamemodeConfig = _configsProvider.GetConfig<QuantumGameModeConfig>(setup.GameModeId);
				if (gamemodeConfig.ShouldUsePlayfabMatchmaking())
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