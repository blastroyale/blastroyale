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
		[SerializeField, Required]
		[ValidateInput("@!_minimapCamera.gameObject.activeSelf", "Camera should be disabled!")]
		private Camera _minimapCamera;

		[SerializeField, Required] private RectTransform _playerIndicator;
		[SerializeField, Required] private RawImage _minimapImage;
		[SerializeField, Required] private RectTransform _shrinkingCircleRing;
		[SerializeField, Required] private RectTransform _safeAreaRing;
		[SerializeField, Range(0f, 1f)] private float _viewportSize = 0.2f;
		[SerializeField] private int _cameraHeight = 10;

		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private QuantumShrinkingCircleConfig _config;

		private RectTransform _rectTransform;
		private EntityView _playerEntityView;

		private bool _safeAreaSet;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_rectTransform = GetComponent<RectTransform>();

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);

			_safeAreaRing.gameObject.SetActive(false);
		}

		[Button, HideInEditorMode]
		private void RenderMinimap()
		{
			FLog.Verbose("Rendering MiniMap camera.");
			_minimapCamera.transform.position = new Vector3(0, _cameraHeight, 0);
			_minimapCamera.Render();
		}

		private void UpdateMinimap(float _)
		{
			UpdateViewport();
			UpdatePlayerIndicator();
			UpdateShrinkingCircle();
		}

		private void OnDestroy()
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateMinimap);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateMinimap);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			RenderMinimap();
			_playerEntityView = _entityViewUpdaterService.GetManualView(callback.Entity);
			_services.TickService.SubscribeOnUpdate(UpdateMinimap);
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

		private void UpdateShrinkingCircle()
		{
			var frame = QuantumRunner.Default.Game.Frames.Predicted;

			if (!frame.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				return;
			}

			var radius = circle.MovingRadius.AsFloat;
			var center = circle.MovingCircleCenter.XOY.ToUnityVector3();
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

			_shrinkingCircleRing.anchoredPosition = circleUICenter;
			_shrinkingCircleRing.sizeDelta = circleUISize;

			if (!_safeAreaSet)
			{
				if (_config.Step != circle.Step)
				{
					_config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
				}

				if (frame.Time < circle.ShrinkingStartTime - _config.WarningTime)
				{
					return;
				}

				_safeAreaSet = true;
				_safeAreaRing.gameObject.SetActive(true);
			}

			_safeAreaRing.anchoredPosition = safeUICenter;
			_safeAreaRing.sizeDelta = safeUISize;
		}
	}
}