using System;
using System.Collections;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
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

		[SerializeField, Required] private RectTransform _safeAreaArrow;
		[SerializeField, Required] private RectTransform _airDropArrow;

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

		private Material _safeAreaRingMat;
		private Material _shrinkingCircleMat;
		private Coroutine _airDropCoroutine;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_rectTransform = GetComponent<RectTransform>();

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnAirDropCollected>(this, OnAirDropCollected);

			_safeAreaRing.gameObject.SetActive(false);

			_safeAreaRingMat = _safeAreaRingImage.material = Instantiate(_safeAreaRingImage.material);
			_shrinkingCircleMat = _shrinkingCircleRingImage.material = Instantiate(_shrinkingCircleRingImage.material);

			_button.onClick.AddListener(OnClick);
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
		}

		private void CloseMinimap()
		{
			_opened = false;
			DOVirtual.Float(_animationModifier, 0f, _duration, UpdateMinimap).SetEase(_openCloseEase);
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

		[Button, HideInEditorMode]
		private void RenderMinimap()
		{
			FLog.Verbose("Rendering MiniMap camera.");
			_minimapCamera.transform.position = new Vector3(0, _cameraHeight, 0);
			_minimapCamera.Render();
		}

		private void OnDestroy()
		{
			QuantumCallback.UnsubscribeListener(this);
			Destroy(_safeAreaRingMat);
			Destroy(_shrinkingCircleMat);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			QuantumCallback.UnsubscribeListener(this);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			RenderMinimap();
			_playerEntityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, UpdateMinimap);
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() || !isActiveAndEnabled)
			{
				return;
			}

			_airDropArrow.gameObject.SetActive(true);
			_airDropCoroutine = StartCoroutine(UpdateAirDropArrow(callback.AirDrop));
		}

		private void OnAirDropCollected(EventOnAirDropCollected callback)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}
			
			StopCoroutine(_airDropCoroutine);
			_airDropArrow.gameObject.SetActive(false);
		}

		private void UpdateMinimap(CallbackUpdateView callback)
		{
			var playerViewportPoint = _minimapCamera.WorldToViewportPoint(_playerEntityView.transform.position);

			UpdateViewport(playerViewportPoint);
			UpdatePlayerIndicator(playerViewportPoint);
			UpdateShrinkingCircle(callback.Game.Frames.Predicted);
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
			var containerSize = _rectTransform.sizeDelta;
			_playerIndicator.anchoredPosition =
				new Vector2(containerSize.x * playerViewportPoint.x - containerSize.x / 2,
				            containerSize.y * playerViewportPoint.y - containerSize.y / 2) *
				_animationModifier;
		}

		private void UpdateShrinkingCircle(Frame f)
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
			var playerViewportPoint = _minimapCamera.WorldToViewportPoint(_playerEntityView.transform.position);

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
			var distanceSqrt = (circleCenter - _cameraTransform.position).sqrMagnitude;

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

		private IEnumerator UpdateAirDropArrow(AirDrop airDrop)
		{
			// Calculate and Apply rotation
			while (true)
			{
				var targetPosLocal = _cameraTransform.InverseTransformPoint(airDrop.Position.ToUnityVector3());
				var targetAngle = -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg;

				_airDropArrow.anchoredPosition = _playerIndicator.anchoredPosition;
				_airDropArrow.eulerAngles = new Vector3(0, 0, targetAngle);
				yield return null;
			}
		}
	}
}