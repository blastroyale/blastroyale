using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using LayerMask = UnityEngine.LayerMask;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// View for controlling small and extended map views.
	/// </summary>
	public class MiniMapView : MonoBehaviour
	{
		[SerializeField, Required] private RenderTexture _shrinkingCircleRenderTexture;
		[SerializeField, Required] private Transform _playerRadarPing;
		[SerializeField, Required] private Camera _camera;
		[SerializeField, Required] private RectTransform _defaultImageRectTransform;
		[SerializeField, Required] private RectTransform _circleImageRectTransform;
		[SerializeField, Required] private Animation _animation;
		[SerializeField, Required] private AnimationClip _smallMiniMapClip;
		[SerializeField, Required] private AnimationClip _extendedMiniMapClip;
		[SerializeField, Required] private UiButtonView _closeButton;
		[SerializeField, Required] private UiButtonView _toggleMiniMapViewButton;

		private enum RenderTextureMode
		{
			None,
			Default,
			ShrinkingCircle
		}

		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private Transform _cameraTransform;
		private EntityView _playerEntityView;
		private const float CameraHeight = 10;
		private bool _smallMapActivated = true;
		private RenderTextureMode _renderTextureMode = RenderTextureMode.None;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_cameraTransform = _camera.transform;

			_closeButton.onClick.AddListener(ToggleMiniMapView);
			_toggleMiniMapViewButton.onClick.AddListener(ToggleMiniMapView);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			_services.MessageBrokerService.Subscribe<MatchReadyForResyncMessage>(OnMatchReadyForResyncMessage);
		}

		private void OnDestroy()
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}
		
		private void ToggleMiniMapView()
		{
			_animation.clip = _smallMapActivated ? _extendedMiniMapClip : _smallMiniMapClip;
			_animation.Play();

			_smallMapActivated = !_smallMapActivated;
		}
		
		private void OnMatchReadyForResyncMessage(MatchReadyForResyncMessage obj)
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			var gameContainer = frame.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];
			//_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();

			if (localPlayer.Entity.IsAlive(frame))
			{
				_playerEntityView = _entityViewUpdaterService.GetManualView(localPlayer.Entity);
				_services.TickService.SubscribeOnUpdate(UpdateTick);
			}
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			_playerEntityView = _entityViewUpdaterService.GetManualView(callback.Entity);

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
	}
}