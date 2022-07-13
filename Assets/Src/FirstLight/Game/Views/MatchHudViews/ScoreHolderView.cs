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
			
			_services.MessageBrokerService.Subscribe<SpectateTargetSwitchedMessage>(OnSpectateTargetSwitchedMessage);
			
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnEventOnLocalPlayerAlive);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || _currentlyFollowing != PlayerRef.None)
			{
				return;
			}
			
			UpdateFollowedPlayer(callback.Player, callback.Game.Frames.Verified);
		}

		private void OnEventOnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			UpdateFollowedPlayer(callback.Player, callback.Game.Frames.Verified);
		}

		private void OnSpectateTargetSwitchedMessage(SpectateTargetSwitchedMessage msg)
		{
			UpdateFollowedPlayer(msg.PlayerSpectated, QuantumRunner.Default.Game.Frames.Verified);
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.PlayerDead == _currentlyFollowing && _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				UpdateFollowedPlayer(callback.PlayerKiller, callback.Game.Frames.Verified);
			}
			else
			{
				var frame = callback.Game.Frames.Verified;
				var gameContainer = frame.GetSingleton<GameContainer>();
				var data = gameContainer.GetPlayersMatchData(frame, out _);
			
				UpdateValues(data[_currentlyFollowing]);
			}
		}

		private void UpdateFollowedPlayer(PlayerRef playerRef, Frame f)
		{
			_currentlyFollowing = playerRef;
			
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
			_progressSlider.value = kills / (float)_fragTarget;
		}

	}
}

