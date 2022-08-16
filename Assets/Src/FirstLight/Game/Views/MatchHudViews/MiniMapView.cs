using System;
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
		private static readonly int _safeAreaOffsetPID = Shader.PropertyToID("_SafeAreaOffset");
		private static readonly int _safeAreaSizePID = Shader.PropertyToID("_SafeAreaSize");
		private static readonly int _dangerAreaOffsetPID = Shader.PropertyToID("_DangerAreaOffset");
		private static readonly int _dangerAreaSizePID = Shader.PropertyToID("_DangerAreaSize");
		private static readonly int _uvRectPID = Shader.PropertyToID("_UvRect");
		private static readonly int _playersPID = Shader.PropertyToID("_Players");
		private static readonly int _playersCountPID = Shader.PropertyToID("_PlayersCount");

		[SerializeField, Required, Title("Minimap")]
		[ValidateInput("@!_minimapCamera.gameObject.activeSelf", "Camera should be disabled!")]
		private Camera _minimapCamera;

		[SerializeField, Required] private RectTransform _rectTransform;
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

		[SerializeField, Required, Title("Indicators")]
		private RectTransform _playerIndicator;

		[SerializeField, Required] private MinimapAirdropView _airdropIndicatorRef;
		[SerializeField, Required] private RectTransform _safeAreaArrow;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private QuantumShrinkingCircleConfig _config;
		private Transform _cameraTransform;
		private bool _safeAreaSet;
		private bool _opened;
		private float _animationModifier = 0f;
		private float _fullScreenMapSize;
		private float _smallMapSize;
		private Vector2 _smallMapPosition;
		private Tweener _tweenerSize;
		private Material _minimapMat;
		private IObjectPool<MinimapAirdropView> _airdropPool;
		private readonly List<Vector4> _playerPositions = new(30);

		private void OnValidate()
		{
			_rectTransform ??= GetComponent<RectTransform>();
		}

		private void Awake()
		{
			var containerSize = _fullScreenContainer.rect.size;
			var mapSize = _rectTransform.rect.size;

			_fullScreenMapSize = Mathf.Min(containerSize.x, containerSize.y);
			_smallMapSize = Mathf.Min(mapSize.x, mapSize.y);
			_smallMapPosition = _rectTransform.anchoredPosition;
			_minimapMat = _minimapImage.material = Instantiate(_minimapImage.material);
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();
			_airdropPool = new ObjectRefPool<MinimapAirdropView>(1, _airdropIndicatorRef,
			                                                     GameObjectPool<MinimapAirdropView>.Instantiator);

			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnAirDropLanded>(this, OnAirDropLanded);
			QuantumEvent.Subscribe<EventOnAirDropCollected>(this, OnAirDropCollected);
			QuantumCallback.Subscribe<CallbackGameResynced>(this, OnGameResync);
			QuantumCallback.Subscribe<CallbackGameStarted>(this, OnGameStarted);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, UpdateView);

			_button.onClick.AddListener(OnClick);
			_fullScreenButton.onClick.AddListener(OnClick);
		}

		private void OnDestroy()
		{
			QuantumCallback.UnsubscribeListener(this);
			QuantumEvent.UnsubscribeListener(this);
			Destroy(_minimapMat);
		}

		private void OnEnable()
		{
			if (Camera.main != null)
			{
				_cameraTransform = Camera.main.transform;
			}
			
			_opened = false;
			_backgroundImage.raycastTarget = false;
			
			_tweenerSize?.Kill();
			UpdateMinimapSize(0);
		}

		private void UpdateView(CallbackUpdateView callback)
		{
			var f = callback.Game.Frames.Predicted;
			var spectate = _matchServices.SpectateService.SpectatedPlayer.Value.Entity;

			if (!f.TryGet<Transform3D>(spectate, out var transform3D))
			{
				return;
			}
			
			var playerViewportPoint = _minimapCamera.WorldToViewportPoint(transform3D.Position.ToUnityVector3());

			UpdatePlayerIndicator(playerViewportPoint, transform3D);
			UpdateAirdropIndicators(playerViewportPoint, f.Time);
			UpdateMap(f, playerViewportPoint, transform3D);
		}

		private void UpdateMinimapSize(float f)
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
		}

		private void UpdatePlayerIndicator(Vector3 playerViewportPoint, Transform3D playerTransform)
		{
			// Rotation
			_playerIndicator.rotation = Quaternion.Euler(0, 0, 360f - playerTransform.Rotation.AsEuler.Y.AsFloat);

			// Position (only relevant in opened map)
			_playerIndicator.anchoredPosition = ViewportToMinimapPosition(playerViewportPoint, playerViewportPoint);
		}

		private void UpdateMap(Frame f, Vector3 playerViewportPoint, Transform3D playerTransform3D)
		{
			if (!f.TryGetSingleton<ShrinkingCircle>(out var circle))
			{
				return;
			}

			circle.GetMovingCircle(f, out var centerFp, out var radiusFp);

			var dangerRadius = radiusFp.AsFloat / _minimapCamera.orthographicSize;
			var dangerCenter = _minimapCamera.WorldToViewportPoint(centerFp.XOY.ToUnityVector3()) - Vector3.one / 2f;
			var safeRadius = circle.TargetRadius.AsFloat / _minimapCamera.orthographicSize;
			var safeCenter = _minimapCamera.WorldToViewportPoint(circle.TargetCircleCenter.XOY.ToUnityVector3()) -
			                 Vector3.one / 2f;

			if (_config.Step != circle.Step)
			{
				_config = _services.ConfigsProvider.GetConfig<QuantumShrinkingCircleConfig>(circle.Step);
			}
			
			// Check to only draw safe area after the warning / announcement
			_safeAreaSet = f.Time > circle.ShrinkingStartTime - _config.WarningTime;

			// Danger ring / area
			_minimapImage.materialForRendering.SetFloat(_dangerAreaSizePID, dangerRadius);
			_minimapImage.materialForRendering.SetVector(_dangerAreaOffsetPID, dangerCenter);

			// Safe area ring
			_minimapImage.materialForRendering.SetFloat(_safeAreaSizePID, _safeAreaSet ? safeRadius : 100);
			_minimapImage.materialForRendering.SetVector(_safeAreaOffsetPID, safeCenter);

			// UV Rect
			var uvRect = new Vector4((playerViewportPoint.x - _viewportSize / 2f) * (1f - _animationModifier),
			                         (playerViewportPoint.y - _viewportSize / 2f) * (1f - _animationModifier),
			                         _viewportSize, _viewportSize);
			_minimapImage.materialForRendering.SetVector(_uvRectPID, uvRect);

			// Players
			if (Shader.IsKeywordEnabled(GameConstants.Visuals.SHADER_MINIMAP_DRAW_PLAYERS))
			{
				_playerPositions.Clear();
				foreach (var (entity, _) in f.GetComponentIterator<AlivePlayerCharacter>())
				{
					var pos = f.Get<Transform3D>(entity).Position.ToUnityVector3();
					var viewportPos = _minimapCamera.WorldToViewportPoint(pos) - Vector3.one / 2f;

					_playerPositions.Add(new Vector4(viewportPos.x, viewportPos.y, 0, 0));
				}

				_minimapImage.materialForRendering.SetVectorArray(_playersPID, _playerPositions);
				_minimapImage.materialForRendering.SetInteger(_playersCountPID, _playerPositions.Count);
			}

			UpdateSafeAreaArrow(playerTransform3D, circle.TargetCircleCenter.ToUnityVector3(), circle.TargetRadius.AsFloat);
		}

		private void UpdateSafeAreaArrow(Transform3D playerTransform3D, Vector3 circleCenter, float circleRadius)
		{
			if (!_safeAreaSet) return;
			
			// Calculate and Apply rotation
			var targetPosLocal = _cameraTransform.InverseTransformPoint(circleCenter);
			var targetAngle = -Mathf.Atan2(targetPosLocal.x, targetPosLocal.y) * Mathf.Rad2Deg;
			var isArrowActive = _safeAreaArrow.gameObject.activeSelf;
			var circleRadiusSq = circleRadius * circleRadius;
			var distanceSqrt = (circleCenter - playerTransform3D.Position.ToUnityVector3()).sqrMagnitude;

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
			for (var i = 0; i < _airdropPool.SpawnedReadOnly.Count; i++)
			{
				var indicator = _airdropPool.SpawnedReadOnly[i];
				indicator.SetPosition(ViewportToMinimapPosition(indicator.ViewportPosition, playerViewportPoint));
				indicator.UpdateTime(time);
			}
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
			_tweenerSize?.Kill();
			
			_opened = true;
			_tweenerSize = DOVirtual.Float(_animationModifier, 1f, _duration, UpdateMinimapSize).SetEase(_openCloseEase);
			_backgroundImage.raycastTarget = true;
		}

		private void CloseMinimap()
		{
			_tweenerSize?.Kill();
			
			_opened = false;
			_tweenerSize = DOVirtual.Float(_animationModifier, 0f, _duration, UpdateMinimapSize).SetEase(_openCloseEase);
			_backgroundImage.raycastTarget = false;
		}

		[Button, HideInEditorMode]
		private void RenderMinimap()
		{
			var ct = _minimapCamera.transform;
			ct.SetParent(null);
			ct.position = new Vector3(0, _cameraHeight, 0);
			_minimapCamera.Render();
		}

		private void OnGameStarted(CallbackGameStarted callback)
		{
			RenderMinimap();
		}

		private void OnGameResync(CallbackGameResynced callback)
		{
			_airdropPool.DespawnAll();
			foreach (var (entity, airDrop) in callback.Game.Frames.Predicted.GetComponentIterator<AirDrop>())
			{
				SpawnAirdrop(entity, airDrop);
			}
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			SpawnAirdrop(callback.Entity, callback.AirDrop);
		}

		private void OnAirDropLanded(EventOnAirDropLanded callback)
		{
			var poolSpawnedReadOnly = _airdropPool.SpawnedReadOnly;

			for (var i = 0; i < poolSpawnedReadOnly.Count; i++)
			{
				if (poolSpawnedReadOnly[i].Entity == callback.Entity)
				{
					poolSpawnedReadOnly[i].OnLanded();
					break;
				}
			}
		}

		private void OnAirDropCollected(EventOnAirDropCollected callback)
		{
			var poolSpawnedReadOnly = _airdropPool.SpawnedReadOnly;

			for (var i = 0; i < poolSpawnedReadOnly.Count; i++)
			{
				if (poolSpawnedReadOnly[i].Entity == callback.Entity)
				{
					_airdropPool.Despawn(poolSpawnedReadOnly[i]);
					break;
				}
			}
		}

		private void SpawnAirdrop(EntityRef entity, AirDrop airDrop)
		{
			var airdropView = _airdropPool.Spawn();
			
			airdropView.SetAirdrop(airDrop, entity, _minimapCamera.WorldToViewportPoint(airDrop.Position.ToUnityVector3()));
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
