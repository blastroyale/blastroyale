using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using Photon.Deterministic;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// View for controlling the minimap view.
	/// </summary>
	public class MiniMapView : MonoBehaviour
	{
		private static readonly int _thicknessPID = Shader.PropertyToID("_Thickness");

		[SerializeField, Required, Title("Minimap")]
		[ValidateInput("@!_minimapCamera.gameObject.activeSelf", "Camera should be disabled!")]
		private Camera _minimapCamera;

		[SerializeField, Required] private Image _backgroundImage;
		[SerializeField, Required] private RectTransform _fullScreenContainer;
		[SerializeField, Required] private Button _button;
		[SerializeField, Required] private Button _fullScreenButton;
		[SerializeField, Required] private float _fullScreenPadding = 50f;
		[SerializeField, Range(0f, 1f)] private float _viewportSize = 0.2f;
		[SerializeField] private int _cameraHeight = 10;

		[SerializeField, Title("Animation")] private Ease _openCloseEase = Ease.OutSine;
		[SerializeField, Required] private float _duration = 0.2f;

		[SerializeField, Required, Title("Shrinking Circle")]
		private RawImage _minimapImage;

		[SerializeField, Required] private RectTransform _shrinkingCircleRing;
		[SerializeField, Required] private Image _shrinkingCircleRingImage;
		[SerializeField, Required] private RectTransform _safeAreaRing;
		[SerializeField, Required] private Image _safeAreaRingImage;
		[SerializeField, Range(0f, 1f)] private float _ringWidth = 0.05f;

		[SerializeField, Required, Title("Indicators")]
		private RectTransform _playerIndicator;

		[SerializeField, Required] private MinimapAirdropView _airdropIndicatorRef;
		[SerializeField, Required] private RectTransform _safeAreaArrow;

		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private QuantumShrinkingCircleConfig _config;

		private RectTransform _rectTransform;
		private EntityView _playerEntityView;
		private Transform _cameraTransform;

		private bool _safeAreaSet;
		private bool _opened;
		private float _animationModifier = 0f;
		private float _lineWidthModifier = 1f;
		private float _fullScreenMapSize;
		private float _smallMapSize;
		private Vector2 _smallMapPosition;
		private bool _subscribedQuantumViewUpdate;

		private Material _safeAreaRingMat;
		private Material _shrinkingCircleMat;
		private Coroutine _airDropCoroutine;

		private IObjectPool<MinimapAirdropView> _airdropPool;
		private readonly Dictionary<EntityRef, MinimapAirdropView> _displayedAirdrops = new();

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_rectTransform = GetComponent<RectTransform>();

			_airdropPool = new ObjectRefPool<MinimapAirdropView>(1, _airdropIndicatorRef,
			                                                     GameObjectPool<MinimapAirdropView>.Instantiator);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_services.MessageBrokerService.Subscribe<SpectateTargetSwitchedMessage>(OnSpectateTargetSwitchedMessage);
			_services.MessageBrokerService.Subscribe<SpectateStartedMessage>(OnSpectateStartedMessage);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnAirDropLanded>(this, OnAirDropLanded);
			QuantumEvent.Subscribe<EventOnAirDropCollected>(this, OnAirDropCollected);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);

			_safeAreaRing.gameObject.SetActive(false);
			_fullScreenButton.gameObject.SetActive(false);

			_safeAreaRingMat = _safeAreaRingImage.material = Instantiate(_safeAreaRingImage.material);
			_shrinkingCircleMat = _shrinkingCircleRingImage.material = Instantiate(_shrinkingCircleRingImage.material);

			_button.onClick.AddListener(OnClick);
			_fullScreenButton.onClick.AddListener(OnClick);
		}

		private void OnClick()
		{
			if (_opened)
			{
				CloseMinimap();
			}
			else
			{
				OpenMinimap();
			}
		}

		private void OpenMinimap()
		{
			_opened = true;
			DOVirtual.Float(_animationModifier, 1f, _duration, UpdateMinimap).SetEase(_openCloseEase);
			_fullScreenButton.gameObject.SetActive(true);
		}

		private void CloseMinimap()
		{
			_opened = false;
			DOVirtual.Float(_animationModifier, 0f, _duration, UpdateMinimap).SetEase(_openCloseEase);
			_fullScreenButton.gameObject.SetActive(false);
		}

		private void UpdateMinimap(float f)
		{
			_animationModifier = f;
			_rectTransform.anchorMin = Vector2.Lerp(Vector2.one, Vector2.one / 2f, f);
			_rectTransform.anchorMax = Vector2.Lerp(Vector2.one, Vector2.one / 2f, f);
			_rectTransform.anchoredPosition = Vector2.Lerp(_smallMapPosition, Vector2.zero, f);
			_rectTransform.sizeDelta = Vector2.Lerp(Vector2.one * _smallMapSize,
			                                        Vector2.one * _fullScreenMapSize - Vector2.one * _fullScreenPadding,
			                                        f);
			_rectTransform.pivot = Vector2.Lerp(Vector2.one, Vector2.one / 2f, f);

			_backgroundImage.color = Color.Lerp(Color.clear, new Color(0f, 0f, 0f, 0.78f), f);

			_viewportSize = Mathf.Lerp(0.3f, 1f, f);
			_lineWidthModifier = Mathf.Lerp(1f, _smallMapSize / _fullScreenMapSize, f);
		}

		private void OnEnable()
		{
			if (Camera.main != null)
			{
				_cameraTransform = Camera.main.transform;
			}

			var containerSize = _fullScreenContainer.rect.size;
			var mapSize = _rectTransform.rect.size;

			_fullScreenMapSize = Mathf.Min(containerSize.x, containerSize.y);
			_smallMapSize = Mathf.Min(mapSize.x, mapSize.y);
			_smallMapPosition = _rectTransform.anchoredPosition;
		}

		private void SubscribeToQuantumViewUpdate()
		{
			if (!_subscribedQuantumViewUpdate)
			{
				_subscribedQuantumViewUpdate = true;
				QuantumCallback.Subscribe<CallbackUpdateView>(this, UpdateMinimap);
			}
		}

		[Button, HideInEditorMode]
		private void RenderMinimap()
		{
			FLog.Verbose("Rendering MiniMap camera.");
			_minimapCamera.transform.position = new Vector3(0, _cameraHeight, 0);
			_minimapCamera.Render();
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
			Destroy(_safeAreaRingMat);
			Destroy(_shrinkingCircleMat);
		}

		private void OnMatchStarted(MatchStartedMessage msg)
		{
			if (!msg.IsResync || _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			RenderMinimap();
			_playerEntityView = _entityViewUpdaterService.GetManualView(localPlayer.Entity);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || _playerEntityView != null)
			{
				return;
			}

			RenderMinimap();
			_playerEntityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			SubscribeToQuantumViewUpdate();
		}

		private void OnSpectateStartedMessage(SpectateStartedMessage obj)
		{
			RenderMinimap();
		}

		private void OnSpectateTargetSwitchedMessage(SpectateTargetSwitchedMessage msg)
		{
			if (_entityViewUpdaterService.TryGetView(msg.EntitySpectated, out var spectated))
			{
				_playerEntityView = spectated;
				SubscribeToQuantumViewUpdate();
			}
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			UpdateMinimap(_duration);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			RenderMinimap();
			_playerEntityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			SubscribeToQuantumViewUpdate();
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || !isActiveAndEnabled)
			{
				return;
			}

			var airdropView = _airdropPool.Spawn();
			airdropView.SetAirdrop(callback.AirDrop,
			                       _minimapCamera.WorldToViewportPoint(callback.AirDrop.Position.ToUnityVector3()));
			_displayedAirdrops.Add(callback.Entity, airdropView);
		}

		private void OnAirDropLanded(EventOnAirDropLanded callback)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || !isActiveAndEnabled)
			{
				return;
			}

			_displayedAirdrops[callback.Entity].OnLanded();
		}

		private void OnAirDropCollected(EventOnAirDropCollected callback)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}

			_airdropPool.Despawn(_displayedAirdrops[callback.Entity]);
			_displayedAirdrops.Remove(callback.Entity);
		}

		private void UpdateMinimap(CallbackUpdateView callback)
		{
			if (_playerEntityView == null)
			{
				return;
			}

			var playerViewportPoint = _minimapCamera.WorldToViewportPoint(_playerEntityView.transform.position);

			UpdateViewport(playerViewportPoint);
			UpdatePlayerIndicator(playerViewportPoint);
			UpdateAirdropIndicators(playerViewportPoint, callback.Game.Frames.Predicted.Time);
			UpdateShrinkingCircle(playerViewportPoint, callback.Game.Frames.Predicted);
		}

		private void UpdateViewport(Vector3 playerViewportPoint)
		{
			_minimapImage.uvRect = new Rect((playerViewportPoint.x - _viewportSize / 2f) * (1f - _animationModifier),
			                                (playerViewportPoint.y - _viewportSize / 2f) * (1f - _animationModifier),
			                                _viewportSize, _viewportSize);
		}

		private void UpdatePlayerIndicator(Vector3 playerViewportPoint)
		{
			// Rotation
			_playerIndicator.rotation =
				Quaternion.Euler(0, 0, 360f - _playerEntityView.transform.rotation.eulerAngles.y);

			// Position (only relevant in opened map)
			_playerIndicator.anchoredPosition = ViewportToMinimapPosition(playerViewportPoint, playerViewportPoint);
		}

		private void UpdateShrinkingCircle(Vector3 playerViewportPoint, Frame f)
		{
			if (!f.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				return;
			}

			circle.GetMovingCircle(f, out var centerFP, out var radiusFP);

			var radius = radiusFP.AsFloat;
			var center = centerFP.XOY.ToUnityVector3();
			var safeRadius = circle.TargetRadius.AsFloat;
			var safeCenter = circle.TargetCircleCenter.XOY.ToUnityVector3();

			var circleViewportPoint = _minimapCamera.WorldToViewportPoint(center);
			var safeViewportPoint = _minimapCamera.WorldToViewportPoint(safeCenter);

			var rect = _rectTransform.rect;
			var minimapFullSize = new Vector2(rect.width / _viewportSize, rect.height / _viewportSize);

			var cameraOrtoSize = _minimapCamera.orthographicSize;
			var circleUICenter =
				(circleViewportPoint - playerViewportPoint * (1f - _animationModifier)) * minimapFullSize -
				minimapFullSize / 2f * _animationModifier;
			var circleUISize = Vector2.one * radius / cameraOrtoSize * minimapFullSize;
			var safeUICenter = (safeViewportPoint - playerViewportPoint * (1f - _animationModifier)) * minimapFullSize -
			                   minimapFullSize / 2f * _animationModifier;
			var safeUISize = Vector2.one * safeRadius / cameraOrtoSize * minimapFullSize;

			var shrinkingCircleRingWidth =
				Math.Clamp(_ringWidth * (rect.width / circleUISize.x), 0f, 1f) * _lineWidthModifier;
			_shrinkingCircleRingImage.materialForRendering.SetFloat(_thicknessPID, shrinkingCircleRingWidth);

			_shrinkingCircleRing.anchoredPosition = circleUICenter;
			_shrinkingCircleRing.sizeDelta = circleUISize;

			if (!_safeAreaSet)
			{
				if (_config.Step != circle.Step)
				{
					_config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
				}

				if (f.Time < circle.ShrinkingStartTime - _config.WarningTime)
				{
					return;
				}

				_safeAreaSet = true;
				_safeAreaRing.gameObject.SetActive(true);
			}

			var safeAreaRingWidth = Math.Clamp(_ringWidth * (rect.width / safeUISize.x), 0f, 1f) * _lineWidthModifier;
			_safeAreaRingImage.materialForRendering.SetFloat(_thicknessPID, safeAreaRingWidth);

			_safeAreaRing.anchoredPosition = safeUICenter;
			_safeAreaRing.sizeDelta = safeUISize;

			UpdateSafeAreaArrow(circle.TargetCircleCenter.ToUnityVector3(), circle.TargetRadius.AsFloat);
		}

		private void UpdateSafeAreaArrow(Vector3 circleCenter, float circleRadius)
		{
			// Calculate and Apply rotation
			var targetPosLocal = _cameraTransform.InverseTransformPoint(circleCenter);
			var targetAngle = -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg;
			var isArrowActive = _safeAreaArrow.gameObject.activeSelf;
			var circleRadiusSq = circleRadius * circleRadius;
			var distanceSqrt = (circleCenter - _playerEntityView.transform.position).sqrMagnitude;

			_safeAreaArrow.anchoredPosition = _playerIndicator.anchoredPosition;
			_safeAreaArrow.eulerAngles = new Vector3(0, 0, targetAngle);

			if (distanceSqrt < circleRadiusSq && isArrowActive)
			{
				_safeAreaArrow.gameObject.SetActive(false);
			}
			else if (distanceSqrt > circleRadiusSq && !isArrowActive)
			{
				_safeAreaArrow.gameObject.SetActive(true);
			}
		}

		private void UpdateAirdropIndicators(Vector3 playerViewportPoint, FP time)
		{
			foreach (var indicator in _airdropPool.SpawnedReadOnly)
			{
				indicator.SetPosition(ViewportToMinimapPosition(indicator.ViewportPosition, playerViewportPoint));
				indicator.UpdateTime(time);
			}
		}

		private Vector2 ViewportToMinimapPosition(Vector3 viewportPosition, Vector3 playerViewportPosition)
		{
			var rect = _rectTransform.rect;
			var minimapFullSize = new Vector2(rect.width / _viewportSize, rect.height / _viewportSize);

			return (viewportPosition - playerViewportPosition * (1f - _animationModifier)) * minimapFullSize -
			       minimapFullSize / 2f * _animationModifier;
		}
	}
}