using System;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using TMPro;
using UnityEngine;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Game.Views.MainMenuViews;
using Quantum.Commands;
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
		[SerializeField] private Button[] _standingsButtons;
		[SerializeField] private Button _leaderButton;
		[SerializeField] private StandingsHolderView _standings;
		[SerializeField] private TextMeshProUGUI _mapStatusText;
		[SerializeField] private LeaderHolderView _leaderHolderView;
		[SerializeField] private ScoreHolderView _scoreHolderView;
		[SerializeField] private MapTimerView _mapTimerView;
		[SerializeField] private ContendersLeftHolderMessageView _contendersLeftHolderMessageView;
		[SerializeField] private ContendersLeftHolderView _contendersLeftHolderView;
		[SerializeField] private GameObject _weaponSlotsHolder;
		[SerializeField] private Button[] _weaponSlotButtons;
		[SerializeField] private GameObject _minimapHolder;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mapStatusText.text = "";

			foreach (var standingsButton in _standingsButtons)
			{
				standingsButton.onClick.AddListener(OnStandingsClicked);
			}

			_weaponSlotButtons[0].onClick.AddListener(() => OnWeaponSlotClicked(0));
			_weaponSlotButtons[1].onClick.AddListener(() => OnWeaponSlotClicked(1));
			_weaponSlotButtons[2].onClick.AddListener(() => OnWeaponSlotClicked(2));

			_connectionIcon.SetActive(false);
			_standings.gameObject.SetActive(false);
			_leaderButton.onClick.AddListener(OnStandingsClicked);
			_quitButton.gameObject.SetActive(Debug.isDebugBuild);
			_quitButton.onClick.AddListener(OnQuitClicked);
			_services.NetworkService.HasLag.InvokeObserve(OnLag);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnNewShrinkingCircle>(this, OnNewShrinkingCircle, onlyIfActiveAndEnabled: true);

			_mapTimerView.gameObject.SetActive(false);
			_leaderHolderView.gameObject.SetActive(false);
			_scoreHolderView.gameObject.SetActive(false);
			_contendersLeftHolderMessageView.gameObject.SetActive(false);
			_contendersLeftHolderView.gameObject.SetActive(false);
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
			var isBattleRoyale = frame.RuntimeConfig.GameMode == GameMode.BattleRoyale;

			_mapTimerView.gameObject.SetActive(isBattleRoyale);
			_contendersLeftHolderMessageView.gameObject.SetActive(isBattleRoyale);
			_contendersLeftHolderView.gameObject.SetActive(isBattleRoyale);
			_leaderHolderView.gameObject.SetActive(!isBattleRoyale);
			_scoreHolderView.gameObject.SetActive(!isBattleRoyale);
			_weaponSlotsHolder.gameObject.SetActive(isBattleRoyale);
			_minimapHolder.gameObject.SetActive(isBattleRoyale);

			if (isBattleRoyale)
			{
				_mapTimerView.UpdateShrinkingCircle(game.Frames.Predicted, frame.GetSingleton<ShrinkingCircle>());
			}
		}

		private void OnNewShrinkingCircle(EventOnNewShrinkingCircle callback)
		{
			_mapTimerView.UpdateShrinkingCircle(callback.Game.Frames.Predicted, callback.ShrinkingCircle);
		}

		private void OnStandingsClicked()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var playerData = new List<QuantumPlayerMatchData>(container.GetPlayersMatchData(frame, out _));

			_standings.gameObject.SetActive(true);
			_standings.Initialise(playerData, false);
		}

		private void OnWeaponSlotClicked(int weaponSlotIndex)
		{
			var command = new WeaponSlotSwitchCommand()
			{
				WeaponSlotIndex = weaponSlotIndex
			};

			QuantumRunner.Default.Game.SendCommand(command);
		}
	}
}