using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Handles logic for the Score Holder view in Deathmatch mode. IE: How many kills you have versus how many the leading player has 
	/// </summary>
	public class ScoreHolderView : MonoBehaviour
	{
		[SerializeField, Required] private TextMeshProUGUI _currentRankText;
		[SerializeField, Required] private TextMeshProUGUI _currentFragsText;
		[SerializeField, Required] private TextMeshProUGUI _targetFragsText;
		[SerializeField, Required] private Slider _progressSlider;
		[SerializeField, Required] private Animation _rankChangeAnimation;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private int _fragTarget;

		private PlayerRef _currentlyFollowing;

		private bool _matchSimulationStarted;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			_currentRankText.text = "1";
			_currentFragsText.text = "0";
			_progressSlider.value = 0;
			_fragTarget = (int) _services.NetworkService.CurrentRoomGameModeConfig.Value.CompletionKillCount;
			_targetFragsText.text = _fragTarget.ToString();
			
			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void OnMatchSimulationStartedMessage(MatchSimulationStartedMessage msg)
		{
			_matchSimulationStarted = true;

			if (_currentlyFollowing != PlayerRef.None)
			{
				UpdateFollowedPlayer(_currentlyFollowing, QuantumRunner.Default.Game.Frames.Predicted);
			}
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			UpdateFollowedPlayer(next.Player, QuantumRunner.Default.Game.Frames.Predicted);
		}
		
		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (callback.Player != _matchServices.SpectateService.SpectatedPlayer.Value.Player || !_matchSimulationStarted) return;
			
			var frame = callback.Game.Frames.Verified;
			var gameContainer = frame.GetSingleton<GameContainer>();
			var data = gameContainer.GetPlayersMatchData(frame, out _);
			
			UpdateValues(data[_currentlyFollowing]);
		}
		
		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.PlayerKiller != _matchServices.SpectateService.SpectatedPlayer.Value.Player) return;
			
			var data = callback.PlayersMatchData;
			
			UpdateValues(data[_currentlyFollowing]);
		}

		private void UpdateFollowedPlayer(PlayerRef playerRef, Frame f)
		{
			_currentlyFollowing = playerRef;

			if (!_matchSimulationStarted) return;
			
			var gameContainer = f.GetSingleton<GameContainer>();
			var data = gameContainer.GetPlayersMatchData(f, out _);

			UpdateValues(data[_currentlyFollowing]);
		}

		private void UpdateValues(QuantumPlayerMatchData playerMatchData)
		{
			_currentRankText.text = playerMatchData.PlayerRank.ToString();

			_rankChangeAnimation.Rewind();
			_rankChangeAnimation.Play();

			var kills = playerMatchData.Data.PlayersKilledCount;

			_currentFragsText.text = kills.ToString();
			_progressSlider.value = kills / (float) _fragTarget;
		}
	}
}