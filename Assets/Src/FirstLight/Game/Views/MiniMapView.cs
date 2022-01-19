using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using LayerMask = UnityEngine.LayerMask;

namespace FirstLight.Game.Views
{
    /// <summary>
    /// This Mono Component controls the visual behaviour for mini map graphics display
    /// </summary>
    public class MiniMapView : MonoBehaviour
    {
        [SerializeField] private RenderTexture _shrinkingCircleRenderTexture;
        [SerializeField] private Transform _playerRadarPing;
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _defaultImageRectTransform;
        [SerializeField] private RectTransform _circleImageRectTransform;
        [SerializeField] private Animation _animation;
        [SerializeField] private AnimationClip _smallMiniMapClip;
        [SerializeField] private AnimationClip _extendedMiniMapClip;
        [SerializeField] private UiButtonView _closeButton;
        
        private enum RenderTextureMode
        {
            None,
            Default,
            ShrinkingCircle
        }
        
        private IGameServices _services;
        private Transform _cameraTransform;
        private EntityView _playerEntityView;
        private const float CameraHeight = 10;
        private bool _smallMapActivated = true;
        private RenderTextureMode _renderTextureMode = RenderTextureMode.None;
        
        private void Awake()
        {
            _services = MainInstaller.Resolve<IGameServices>();

            _cameraTransform = _camera.transform;
            
            _closeButton.onClick.AddListener(ToggleMiniMapView);
            
            QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
        }

        private void OnDestroy()
        {
            _services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
        }
        
        private void ToggleMiniMapView()
        {
            _animation.clip = _smallMapActivated ? _extendedMiniMapClip : _smallMiniMapClip;
            _animation.Play();

            _smallMapActivated = !_smallMapActivated;
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
    }
}