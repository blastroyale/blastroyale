using System;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Ids;
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
		private static readonly int _enemiesPID = Shader.PropertyToID("_Enemies");
		private static readonly int _enemiesCountPID = Shader.PropertyToID("_EnemiesCount");
		private static readonly int _friendliesPID = Shader.PropertyToID("_Friendlies");
		private static readonly int _friendliesCountPID = Shader.PropertyToID("_FriendliesCount");
		private static readonly int _EnemiesOpacityPID = Shader.PropertyToID("_EnemiesOpacity");
		private static readonly int _FriendlyColorsPID = Shader.PropertyToID("_FriendliesColors");
		private static readonly int _PlayerCircleSizePID = Shader.PropertyToID("_PlayersSize");
		private static readonly int _PlayerCircleSizeFocusedPID = Shader.PropertyToID("_PlayersSizeFocused");

		private static readonly int _pingPositionPID = Shader.PropertyToID("_PingPosition");
		private static readonly int _pingProgressPID = Shader.PropertyToID("_PingProgress");

		private readonly TimeSpan RADAR_UPDATE_FREQ = TimeSpan.FromSeconds(2);

		[SerializeField, Required, Title("Minimap")] [ValidateInput("@!_minimapCamera.gameObject.activeSelf", "Camera should be disabled!")]
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
		[SerializeField, Required] private float _pingDuration = 1f;


		[SerializeField, Required, Title("Shrinking Circle")]
		private RawImage _minimapImage;

		[SerializeField, Required, Title("Indicators")]
		private RectTransform _playerIndicator;

		[SerializeField, Required] private AnimationCurve _playersFade;

		[SerializeField, Required] private MinimapAirdropView _airdropIndicatorRef;
		[SerializeField, Required] private RectTransform _safeAreaArrow;
		[SerializeField, Required] private MinimapPingView _pingIndicatorRef;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private QuantumShrinkingCircleConfig _config;
		private Transform _cameraTransform;
		private bool _safeAreaSet;
		private bool _opened;
		private float _animationModifier;
		private float _fullScreenMapSize;
		private float _smallMapSize;
		private Vector2 _smallMapPosition;
		private Tweener _tweenerSize;
		private Material _minimapMat;
		private IObjectPool<MinimapAirdropView> _airdropPool;
		private IObjectPool<MinimapPingView> _pingPool;
		private readonly Vector4[] _enemyPositions = new Vector4[30];
		private readonly Vector4[] _friendlyPositions = new Vector4[30];
		private readonly Color[] _friendlyColors = new Color[30];

		private Tweener _pingTweener;

		private bool _radarActive;
		private DateTime _radarEndTime;
		private float _radarRange;
		private float _defaultPlayerSize;
		private DateTime _radarStartTime;
		private DateTime _radarLastUpdate;
		private float _mapConfigCameraSize;
		private float _currentViewportSize = 0.2f;

		private void OnValidate()
		{
			_rectTransform ??= GetComponent<RectTransform>();
		}

		private void Awake()
		{
			var containerSize = _fullScreenContainer.rect.size;
			var mapSize = _rectTransform.rect.size;
			_currentViewportSize = _viewportSize;
			_fullScreenMapSize = Mathf.Min(containerSize.x, containerSize.y);
			_smallMapSize = Mathf.Min(mapSize.x, mapSize.y);
			_smallMapPosition = _rectTransform.anchoredPosition;
			_minimapMat = _minimapImage.material = Instantiate(_minimapImage.material);
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_services = MainInstaller.Resolve<IGameServices>();
			_airdropPool = new ObjectRefPool<MinimapAirdropView>(1, _airdropIndicatorRef,
				GameObjectPool<MinimapAirdropView>.Instantiator);
			_pingPool = new ObjectRefPool<MinimapPingView>(1, _pingIndicatorRef,
				GameObjectPool<MinimapPingView>.Instantiator);
			_defaultPlayerSize = _minimapImage.materialForRendering.GetFloat(_PlayerCircleSizePID);

			QuantumEvent.Subscribe<EventOnAirDropDropped>(this, OnAirDropDropped);
			QuantumEvent.Subscribe<EventOnAirDropLanded>(this, OnAirDropLanded);
			QuantumEvent.Subscribe<EventOnAirDropCollected>(this, OnAirDropCollected);
			QuantumEvent.Subscribe<EventOnRadarUsed>(this, OnRadarUsed);
			QuantumEvent.Subscribe<EventOnRadarUsed>(this, OnRadarUsed);
			QuantumEvent.Subscribe<EventOnTeamPositionPing>(this, OnTeamPositionPing);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);

			_button.onClick.AddListener(OnClick);
			_fullScreenButton.onClick.AddListener(OnClick);
			SetupCameraSize();
		}

		private void SetupCameraSize()
		{
			var room = _services.RoomService.CurrentRoom;

			var configValue = room.MapConfig.MinimapCameraSize;
			if (configValue != 0)
			{
				_mapConfigCameraSize = configValue;

				var currentSize = _minimapCamera.orthographicSize;
				var ratio = currentSize / _mapConfigCameraSize;
				_viewportSize *= ratio;
				_currentViewportSize = _viewportSize;
			}
		}

		private void OnTeamPositionPing(EventOnTeamPositionPing e)
		{
			if (e.TeamId < 0 || _matchServices.SpectateService.SpectatedPlayer.Value.Team == e.TeamId)
			{
				var pingPosition = _minimapCamera.WorldToViewportPoint(e.Position.ToUnityVector3());

				var ping = _pingPool.Spawn();
				ping.ViewportPosition = pingPosition;
				ping.LateCall(_pingDuration, () => _pingPool.Despawn(ping));

				_minimapImage.materialForRendering.SetVector(_pingPositionPID, pingPosition - Vector3.one / 2f);

				_pingTweener?.Kill();
				_pingTweener = DOVirtual.Float(0f, 1f, 1f,
					val => _minimapImage.materialForRendering.SetFloat(_pingProgressPID, val));

				// Maybe not the cleanest, that the Minimap spawns the VFX.
				var pingVfx = _services.VfxService.Spawn(VfxId.Ping);
				pingVfx.transform.position = e.Position.ToUnityVector3();
			}
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			Destroy(_minimapMat);
		}

		private void OnEnable()
		{
			_cameraTransform = FLGCamera.Instance.MainCamera.transform;

			_opened = false;
			_backgroundImage.raycastTarget = false;

			_tweenerSize?.Kill();
			UpdateMinimapSize(0);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, UpdateView);
		}

		private void OnDisable()
		{
			QuantumCallback.UnsubscribeListener(this);
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
			UpdatePingIndicators(playerViewportPoint);
			UpdateMap(f, playerViewportPoint, transform3D);
		}

		private void UpdateMinimapSize(float f)
		{
			_animationModifier = f;
			_rectTransform.anchorMin = Vector2.Lerp(Vector2.one, Vector2.one / 2f, f);
			_rectTransform.anchorMax = Vector2.Lerp(Vector2.one, Vector2.one / 2f, f);
			_rectTransform.anchoredPosition = Vector2.Lerp(_smallMapPosition, Vector2.zero, f);
			_rectTransform.sizeDelta = Vector2.Lerp(Vector2.one * _smallMapSize,
				Vector2.one * _fullScreenMapSize - Vector2.one * _fullScreenPadding, f);
			_rectTransform.pivot = Vector2.Lerp(Vector2.one, Vector2.one / 2f, f);
			_backgroundImage.color = Color.Lerp(Color.clear, new Color(0f, 0f, 0f, 0.78f), f);
			_currentViewportSize = Mathf.Lerp(_viewportSize, 1f, f);
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
				_config = f.Context.MapShrinkingCircleConfigs[circle.Step];
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
			var uvRect = new Vector4((playerViewportPoint.x - _currentViewportSize / 2f) * (1f - _animationModifier),
				(playerViewportPoint.y - _currentViewportSize / 2f) * (1f - _animationModifier),
				_currentViewportSize, _currentViewportSize);
			_minimapImage.materialForRendering.SetVector(_uvRectPID, uvRect);

			UpdateSafeAreaArrow(playerTransform3D, circle.TargetCircleCenter.ToUnityVector3(),
				circle.TargetRadius.AsFloat);
			UpdateRadar(f, playerTransform3D.Position.ToUnityVector3());
			UpdateFriendlies(f);
		}

		private void UpdateRadar(Frame f, Vector3 playerPosition)
		{
			if (!_radarActive) return;

			var now = DateTime.Now;

			if (now > _radarEndTime)
			{
				FLog.Info("Turning off radar!");

				// Radar ran out
				_radarActive = false;
				return;
			}

			var nextPing = _radarLastUpdate + RADAR_UPDATE_FREQ;

			var elapsedPingDuration = (now - _radarLastUpdate).TotalSeconds;
			var elapsedPing = (float) elapsedPingDuration / RADAR_UPDATE_FREQ.TotalSeconds;
			var pingOpacity = _playersFade.Evaluate((float) elapsedPing);

			_minimapImage.materialForRendering.SetFloat(_EnemiesOpacityPID, pingOpacity);

			if (now > nextPing)
			{
				// Update positions
				_radarLastUpdate = now;

				int index = 0;
				foreach (var (entity, apc) in f.GetComponentIterator<AlivePlayerCharacter>())
				{
					var pos = f.Get<Transform3D>(entity).Position.ToUnityVector3();

					// Check range
					if (Vector3.Distance(playerPosition, pos) > _radarRange)
					{
						continue;
					}

					// Don't show the local player
					if (f.Context.IsLocalPlayer(f.Get<PlayerCharacter>(entity).Player))
					{
						continue;
					}

					// Dont Show TeamMates
					if (f.TryGet<PlayerCharacter>(entity, out var player))
					{
						if (IsFriendly(f, entity, player))
						{
							continue;
						}
					}


					var viewportPos = _minimapCamera.WorldToViewportPoint(pos) - Vector3.one / 2f;
					_enemyPositions[index++] = new Vector4(viewportPos.x, viewportPos.y, 0, 0);
				}

				_minimapImage.materialForRendering.SetVectorArray(_enemiesPID, _enemyPositions);
				_minimapImage.materialForRendering.SetInteger(_enemiesCountPID, index);
			}
		}

		private void UpdateFriendlies(Frame f)
		{
			int index = 0;
			foreach (var (entity, t) in f.GetComponentIterator<PlayerCharacter>())
			{
				if (!IsFriendly(f, entity, t)) continue;

				var pos = f.Get<Transform3D>(entity).Position.ToUnityVector3();
				var viewportPos = _minimapCamera.WorldToViewportPoint(pos) - Vector3.one / 2f;

				var color = _matchServices.TeamService.GetTeamMemberColor(entity);
				if (color.HasValue) _friendlyColors[index] = color.Value;
				_friendlyPositions[index++] = new Vector4(viewportPos.x, viewportPos.y, 0, 0);
			}

			_minimapImage.materialForRendering.SetColorArray(_FriendlyColorsPID, _friendlyColors);
			_minimapImage.materialForRendering.SetVectorArray(_friendliesPID, _friendlyPositions);
			_minimapImage.materialForRendering.SetInteger(_friendliesCountPID, index);
			
		}

		private bool IsFriendly(Frame f, EntityRef entity, PlayerCharacter targetable)
		{
			var spectatePlayer = _matchServices.SpectateService.SpectatedPlayer.Value;
			return !f.Context.IsLocalPlayer(targetable.Player) &&
				targetable.TeamId == spectatePlayer.Team;
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

		private void UpdatePingIndicators(Vector3 playerViewportPoint)
		{
			for (var i = 0; i < _pingPool.SpawnedReadOnly.Count; i++)
			{
				var indicator = _pingPool.SpawnedReadOnly[i];
				indicator.SetPosition(ViewportToMinimapPosition(indicator.ViewportPosition, playerViewportPoint));
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
			_minimapImage.materialForRendering.SetFloat(_PlayerCircleSizePID, _minimapImage.materialForRendering.GetFloat(_PlayerCircleSizeFocusedPID));
			_opened = true;
			_tweenerSize = DOVirtual.Float(_animationModifier, 1f, _duration, UpdateMinimapSize)
				.SetEase(_openCloseEase);
			_backgroundImage.raycastTarget = true;
		}

		private void CloseMinimap()
		{
			_tweenerSize?.Kill();
			_minimapImage.materialForRendering.SetFloat(_PlayerCircleSizePID, _defaultPlayerSize);
			_opened = false;
			_tweenerSize = DOVirtual.Float(_animationModifier, 0f, _duration, UpdateMinimapSize)
				.SetEase(_openCloseEase);
			_backgroundImage.raycastTarget = false;
		}

		[Button, HideInEditorMode]
		private void RenderMinimap()
		{
			if (_mapConfigCameraSize != 0)
			{
				_minimapCamera.orthographicSize = _mapConfigCameraSize;
			}

			var ct = _minimapCamera.transform;
			ct.position = new Vector3(0, _cameraHeight, 0);
			_minimapCamera.Render();
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			RenderMinimap();

			if (msg.IsResync)
			{
				_airdropPool.DespawnAll();

				foreach (var (entity, airDrop) in msg.Game.Frames.Predicted.GetComponentIterator<AirDrop>())
				{
					SpawnAirdrop(entity, airDrop);
				}
			}
		}

		private void OnAirDropDropped(EventOnAirDropDropped callback)
		{
			SpawnAirdrop(callback.Entity, callback.AirDrop);
		}

		private void OnAirDropLanded(EventOnAirDropLanded callback)
		{
			var poolSpawnedReadOnly = _airdropPool.SpawnedReadOnly;

			foreach (var airdropView in poolSpawnedReadOnly)
			{
				if (airdropView.Entity != callback.Entity) continue;

				airdropView.OnLanded();
				break;
			}
		}

		private void OnAirDropCollected(EventOnAirDropCollected callback)
		{
			var poolSpawnedReadOnly = _airdropPool.SpawnedReadOnly;

			foreach (var airdropView in poolSpawnedReadOnly)
			{
				if (airdropView.Entity != callback.Entity) continue;

				_airdropPool.Despawn(airdropView);
				break;
			}
		}

		private void OnRadarUsed(EventOnRadarUsed callback)
		{
			FLog.Info($"Turning on radar, pings: {callback.Duration.AsInt}!");
			_radarActive = true;
			_radarStartTime = DateTime.Now;
			_radarEndTime = _radarStartTime + RADAR_UPDATE_FREQ * callback.Duration.AsFloat;
			_radarRange = callback.Range.AsFloat;
		}

		private void SpawnAirdrop(EntityRef entity, AirDrop airDrop)
		{
			var airdropView = _airdropPool.Spawn();

			airdropView.SetAirdrop(airDrop, entity,
				_minimapCamera.WorldToViewportPoint(airDrop.Position.ToUnityVector3()));
		}

		private Vector2 ViewportToMinimapPosition(Vector3 viewportPosition, Vector3 playerViewportPosition)
		{
			var rect = _rectTransform.rect;
			var minimapFullSize = new Vector2(rect.width / _currentViewportSize, rect.height / _currentViewportSize);

			return (viewportPosition - playerViewportPosition * (1f - _animationModifier)) * minimapFullSize -
				minimapFullSize / 2f * _animationModifier;
		}
	}
}