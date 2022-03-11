using System.Collections.Generic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using TMPro;
using UnityEngine;
using FirstLight.Game.Logic;
using FirstLight.Game.Views;
using FirstLight.Game.Views.AdventureHudViews;
using FirstLight.Game.Views.MainMenuViews;
using Quantum.Commands;
using Button = UnityEngine.UI.Button;
using LayerMask = UnityEngine.LayerMask;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Match HUD UI by:
	/// - Showing the Game HUD visual status
	/// - Showing the MiniMap
	/// </summary>		
	public class MatchHudPresenter : UiPresenter
	{
		[Header("HUD")] [SerializeField] private Animation _animation;
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
		[SerializeField] private Button[] _weaponSlotButtons;

		// MiniMap
		[Header("MiniMap")] [SerializeField] private RenderTexture _shrinkingCircleRenderTexture;
		[SerializeField] private Transform _playerRadarPing;
		[SerializeField] private Camera _camera;
		[SerializeField] private RectTransform _defaultImageRectTransform;
		[SerializeField] private RectTransform _circleImageRectTransform;
		[SerializeField] private Animation _miniMapAnimation;
		[SerializeField] private AnimationClip _smallMiniMapClip;
		[SerializeField] private AnimationClip _extendedMiniMapClip;
		[SerializeField] private UiButtonView _closeButton;
		[SerializeField] private Button _toggleMiniMapViewButton;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private Transform _cameraTransform;
		private EntityView _playerEntityView;
		private const float CameraHeight = 10;
		private bool _smallMapActivated = true;
		private RenderTextureMode _renderTextureMode = RenderTextureMode.None;

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

			_cameraTransform = _camera.transform;

			_closeButton.onClick.AddListener(ToggleMiniMapView);
			_toggleMiniMapViewButton.onClick.AddListener(ToggleMiniMapView);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.NetworkService?.HasLag?.StopObservingAll(this);
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
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

		private void ToggleMiniMapView()
		{
			_miniMapAnimation.clip = _smallMapActivated ? _extendedMiniMapClip : _smallMiniMapClip;
			_miniMapAnimation.Play();

			_smallMapActivated = !_smallMapActivated;
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			_playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);

			_services.TickService.SubscribeOnUpdate(UpdateTick);
		}

		private void UpdateTick(float deltaTime)
		{
			_cameraTransform.position = new Vector3(0, CameraHeight, 0);

			if (_smallMapActivated)
			{
				var viewportPoint = _camera.WorldToViewportPoint(_playerEntityView.transform.position);
				var screenDelta = new Vector2(viewportPoint.x, viewportPoint.y);

				screenDelta.Scale(_defaultImageRectTransform.rect.size);
				screenDelta -= _defaultImageRectTransform.rect.size * 0.5f;

				_defaultImageRectTransform.localPosition = -screenDelta;
				_circleImageRectTransform.localPosition = -screenDelta;

				_playerRadarPing.localPosition = Vector3.zero;
			}
			else
			{
				_defaultImageRectTransform.localPosition = Vector3.zero;
				_circleImageRectTransform.localPosition = Vector3.zero;

				SetPingPosition(_playerRadarPing, _playerEntityView.transform.position);
			}

			if (_renderTextureMode == RenderTextureMode.Default)
			{
				_camera.targetTexture = _shrinkingCircleRenderTexture;
				_camera.cullingMask = LayerMask.GetMask("Mini Map Object");
				_renderTextureMode = RenderTextureMode.ShrinkingCircle;
			}
			else if (_renderTextureMode == RenderTextureMode.None)
			{
				_renderTextureMode = RenderTextureMode.Default;
			}
		}

		private void SetPingPosition(Transform pingTransform, Vector3 positionWorldSpace)
		{
			var viewportPoint = _camera.WorldToViewportPoint(positionWorldSpace);
			var screenDelta = new Vector2(viewportPoint.x, viewportPoint.y);

			screenDelta.Scale(_defaultImageRectTransform.rect.size);

			screenDelta -= _defaultImageRectTransform.rect.size * 0.5f;
			pingTransform.localPosition = screenDelta;
		}

		private enum RenderTextureMode
		{
			None,
			Default,
			ShrinkingCircle
		}
	}
}