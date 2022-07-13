using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.Match;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Realtime;
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
		private IGameDataProvider _gameDataProvider;
		private int _fragTarget;

		private PlayerRef _currentlyFollowing;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_currentRankText.text = "1";
			_currentFragsText.text = "0";
			_progressSlider.value = 0;
			_fragTarget = (int) _services.NetworkService.CurrentRoomMapConfig.Value.GameEndTarget;
			_targetFragsText.text = _fragTarget.ToString();

			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			_services.MessageBrokerService.Subscribe<SpectateTargetSwitchedMessage>(OnSpectateTargetSwitchedMessage);
			
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnEventOnLocalPlayerAlive);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}

			UpdateFollowedPlayer(callback.Player);
		}

		private void OnEventOnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			UpdateFollowedPlayer(callback.Player);
		}

		private void OnSpectateTargetSwitchedMessage(SpectateTargetSwitchedMessage msg)
		{
			UpdateFollowedPlayer(msg.PlayerSpectated);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchSimulationStartedMessage(MatchSimulationStartedMessage msg)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out _);

			_currentRankText.text = matchData[game.GetLocalPlayers()[0]].PlayerRank.ToString();
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.PlayerKiller == _currentlyFollowing)
			{
				UpdateValues(callback.PlayersMatchData[_currentlyFollowing]);
			}
		}

		private void UpdateFollowedPlayer(PlayerRef playerRef)
		{
			_currentlyFollowing = playerRef;
			
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var gameContainer = frame.GetSingleton<GameContainer>();
			var data = gameContainer.GetPlayersMatchData(frame, out _);

			UpdateValues(data[_currentlyFollowing]);
		}

		private void UpdateValues(QuantumPlayerMatchData playerMatchData)
		{
			_currentRankText.text = playerMatchData.PlayerRank.ToString();
			
			_rankChangeAnimation.Rewind();
			_rankChangeAnimation.Play();

			var kills = playerMatchData.Data.PlayersKilledCount;
			
			_currentFragsText.text = kills.ToString();
			_progressSlider.value = kills / (float)_fragTarget;
		}

	}
}

