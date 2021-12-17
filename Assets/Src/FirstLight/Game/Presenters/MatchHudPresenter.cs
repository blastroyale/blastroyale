using System;
using System.Collections;
using System.Collections.Generic;
using Circuit;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using TMPro;
using UnityEngine;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Match HUD UI by:
	/// - Showing the Game HUD visual status
	/// </summary>		
	public class MatchHudPresenter : UiPresenter
	{
		[SerializeField] private Animation _animation;
		[SerializeField] private AnimationClip _introAnimationClip;
		[SerializeField] private GameObject _connectionIcon;
		[SerializeField] private Button _quitButton;
		[SerializeField] private Button _standingsButton;
		[SerializeField] private Button _leaderButton;
		[SerializeField] private TextMeshProUGUI _currentRankText;
		[SerializeField] private TextMeshProUGUI _currentFragsText;
		[SerializeField] private TextMeshProUGUI _targetFragsText;
		[SerializeField] private Image _currentWeaponImage;
		[SerializeField] private TextMeshProUGUI _currentWeaponText;
		[SerializeField] private Slider _progressSlider;
		[SerializeField] private StandingsHolderView _standings;
		[SerializeField] private Animation _rankChangeAnimation;
		[SerializeField] private TextMeshProUGUI _mapStatusText;
		[SerializeField] private MapTimerView _mapTimerView;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private int _fragTarget;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			var matchId = _gameDataProvider.AdventureDataProvider.SelectedMapId.Value;
				
			_currentRankText.text = "1";
			_currentFragsText.text = "0";
			_mapStatusText.text = "";
			_progressSlider.value = 0;
			_currentWeaponText.text = ScriptLocalization.GameIds.Hammer;
			_fragTarget = _services.ConfigsProvider.GetConfig<MapConfig>(matchId).GameEndTarget;
			_targetFragsText.text = _fragTarget.ToString();
			
			_connectionIcon.SetActive(false);
			_standings.gameObject.SetActive(false);
			_standingsButton.onClick.AddListener(OnStandingsClicked);
			_leaderButton.onClick.AddListener(OnStandingsClicked);
			_quitButton.gameObject.SetActive(Debug.isDebugBuild);
			_quitButton.onClick.AddListener(OnQuitClicked);
			_services.NetworkService.HasLag.InvokeObserve(OnLag);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
			QuantumEvent.Subscribe<EventOnLocalPlayerWeaponChanged>(this, OnEventOnLocalPlayerWeaponChanged);
			QuantumEvent.Subscribe<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle, onlyIfActiveAndEnabled: true);
			
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.NetworkService?.HasLag?.StopObservingAll(this);
		}

		protected override void OnOpened()
		{
			_animation.clip = _introAnimationClip;
			_animation.Play();
		}

		private async void OnEventOnLocalPlayerWeaponChanged(EventOnLocalPlayerWeaponChanged callback)
		{
			_currentWeaponText.text = callback.WeaponGameId.GetTranslation();
			
			_currentWeaponImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(callback.WeaponGameId);
		}
		
		private void OnQuitClicked()
		{
			_services.MessageBrokerService.Publish(new QuitGameClickedMessage());
		}

		private void OnLag(bool previous, bool hasLag)
		{
			_connectionIcon.SetActive(hasLag);
		}

		private void OnMatchStarted(MatchStartedMessage message)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out _);
			
			_currentRankText.text = matchData[game.GetLocalPlayers()[0]].PlayerRank.ToString();

			if (container.GameMode == GameMode.BattleRoyale)
			{
				_mapTimerView.UpdateShrinkingCircle(game.Frames.Predicted, frame.GetSingleton<ShrinkingCircle>());
			}
		}
		
		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			_mapTimerView.UpdateShrinkingCircle(callback.Game.Frames.Predicted, callback.ShrinkingCircle);
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var killerData = callback.PlayersMatchData[callback.PlayerKiller];
			var localPlayer = callback.Game.GetLocalPlayers()[0];

			_currentRankText.text = callback.PlayersMatchData[localPlayer].PlayerRank.ToString();
			
			_rankChangeAnimation.Rewind();
			_rankChangeAnimation.Play();
			
			if (localPlayer != callback.PlayerKiller)
			{
				return;
			}

			var kills = killerData.Data.PlayersKilledCount;
			
			_currentFragsText.text = kills.ToString();
			_progressSlider.value = kills / (float)_fragTarget;
		}

		private void OnStandingsClicked()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = new List<QuantumPlayerMatchData>();

			for(var i = 0; i < container.PlayersData.Length; i++)
			{
				if (!container.PlayersData[i].IsValid)
				{
					continue;
				}
				
				var playerMatchData = new QuantumPlayerMatchData(frame,container.PlayersData[i]);
				
				playerData.Add(playerMatchData);
			}
			
			_standings.gameObject.SetActive(true);
			_standings.Initialise(playerData, false);
		}
	}
}