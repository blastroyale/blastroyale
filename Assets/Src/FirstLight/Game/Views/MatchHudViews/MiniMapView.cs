using System;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// View for controlling the minimap view.
	/// </summary>
	public class MiniMapView : MonoBehaviour
	{
		private static readonly int _thicknessPID = Shader.PropertyToID("_Thickness");

		[SerializeField, Required]
		[ValidateInput("@!_minimapCamera.gameObject.activeSelf", "Camera should be disabled!")]
		private Camera _minimapCamera;

		[SerializeField, Required] private RectTransform _playerIndicator;
		[SerializeField, Required] private RawImage _minimapImage;
		[SerializeField, Required] private RectTransform _shrinkingCircleRing;
		[SerializeField, Required] private Image _shrinkingCircleRingImage;
		[SerializeField, Required] private RectTransform _safeAreaRing;
		[SerializeField, Required] private Image _safeAreaRingImage;
		[SerializeField, Range(0f, 1f)] private float _viewportSize = 0.2f;
		[SerializeField, Range(0f, 1f)] private float _ringWidth = 0.05f;
		[SerializeField] private int _cameraHeight = 10;

		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private QuantumShrinkingCircleConfig _config;

		private RectTransform _rectTransform;
		private EntityView _playerEntityView;

		private bool _safeAreaSet;

		private Material _safeAreaRingMat;
		private Material _shrinkingCircleMat;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_rectTransform = GetComponent<RectTransform>();

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);

			_safeAreaRing.gameObject.SetActive(false);

			_safeAreaRingMat = _safeAreaRingImage.material = Instantiate(_safeAreaRingImage.material);
			_shrinkingCircleMat = _shrinkingCircleRingImage.material = Instantiate(_shrinkingCircleRingImage.material);
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

		private void UpdateMinimap(CallbackUpdateView callback)
		{
			UpdateViewport();
			UpdatePlayerIndicator();
			UpdateShrinkingCircle(callback.Game.Frames.Predicted);
		}

		private void UpdateViewport()
		{
			var viewportPoint = _minimapCamera.WorldToViewportPoint(_playerEntityView.transform.position);
			_minimapImage.uvRect = new Rect(viewportPoint.x - _viewportSize / 2f,
			                                viewportPoint.y - _viewportSize / 2f,
			                                _viewportSize, _viewportSize);
		}

		private void UpdatePlayerIndicator()
		{
			_playerIndicator.rotation =
				Quaternion.Euler(0, 0, 360f - _playerEntityView.transform.rotation.eulerAngles.y);
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
			var circleUICenter = (circleViewportPoint - playerViewportPoint) * minimapFullSize;
			var circleUISize = Vector2.one * radius / cameraOrtoSize * minimapFullSize;
			var safeUICenter = (safeViewportPoint - playerViewportPoint) * minimapFullSize;
			var safeUISize = Vector2.one * safeRadius / cameraOrtoSize * minimapFullSize;

			var shrinkingCircleRingWidth = Math.Clamp(_ringWidth * (rect.width / circleUISize.x), 0f, 1f);
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

			var safeAreaRingWidth = Math.Clamp(_ringWidth * (rect.width / safeUISize.x), 0f, 1f);
			_safeAreaRingImage.materialForRendering.SetFloat(_thicknessPID, safeAreaRingWidth);

			_safeAreaRing.anchoredPosition = safeUICenter;
			_safeAreaRing.sizeDelta = safeUISize;
		}
	}
}