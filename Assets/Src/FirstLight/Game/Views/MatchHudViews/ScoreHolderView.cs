using FirstLight.Game.Logic;
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
		private IGameDataProvider _gameDataProvider;
		private int _fragTarget;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_currentRankText.text = "1";
			_currentFragsText.text = "0";
			_progressSlider.value = 0;
			_fragTarget = _services.NetworkService.CurrentRoomMapConfig.Value.GameEndTarget;
			_targetFragsText.text = _fragTarget.ToString();

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
		}
		
		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out _);

			_currentRankText.text = matchData[game.GetLocalPlayers()[0]].PlayerRank.ToString();
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var killerData = callback.PlayersMatchData.Find(data => data.Data.Player.Equals(callback.PlayerKiller));;

			var localPlayer = callback.PlayersMatchData.Find(data => data.IsLocalPlayer);
			_currentRankText.text = localPlayer.PlayerRank.ToString();
			
			_rankChangeAnimation.Rewind();
			_rankChangeAnimation.Play();
			
			if (localPlayer.Data.Player != callback.PlayerKiller)
			{
				return;
			}

			var kills = killerData.Data.PlayersKilledCount;
			
			_currentFragsText.text = kills.ToString();
			_progressSlider.value = kills / (float)_fragTarget;
		}

	}
}

