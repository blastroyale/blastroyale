using System.Collections.Generic;
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
using FirstLight.Game.Views.MatchHudViews;
using Quantum.Commands;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Match HUD UI by:
	/// - Showing the Game HUD visual status
	/// </summary>		
	public class MatchHudPresenter : UiPresenter
	{
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _introAnimationClip;
		[SerializeField, Required] private GameObject _connectionIcon;
		[SerializeField, Required] private Button _quitButton;
		[SerializeField] private Button[] _standingsButtons;
		[SerializeField, Required] private Button _leaderButton;
		[SerializeField, Required] private StandingsHolderView _standings;
		[SerializeField, Required] private TextMeshProUGUI _mapStatusText;
		[SerializeField, Required] private LeaderHolderView _leaderHolderView;
		[SerializeField, Required] private ScoreHolderView _scoreHolderView;
		[SerializeField, Required] private MapTimerView _mapTimerView;
		[SerializeField, Required] private ContendersLeftHolderMessageView _contendersLeftHolderMessageView;
		[SerializeField, Required] private ContendersLeftHolderView _contendersLeftHolderView;
		[SerializeField, Required] private GameObject _weaponSlotsHolder;
		[SerializeField] private Button[] _weaponSlotButtons;
		[SerializeField, Required] private GameObject _minimapHolder;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
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
			var isBattleRoyale = frame.RuntimeConfig.GameMode == GameMode.BattleRoyale;

			_standings.gameObject.SetActive(true);
			_standings.Initialise(playerData, isBattleRoyale);
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