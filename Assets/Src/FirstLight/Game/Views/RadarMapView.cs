using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views
{
    public class RadarMapView : MonoBehaviour
    {
        [SerializeField] private Transform _playerRadarPing;
        [SerializeField] private Camera _camera;
        [SerializeField] private RectTransform _radarBackground;
        [SerializeField] private Animation _animation;
        [SerializeField] private AnimationClip _smallMiniMapClip;
        [SerializeField] private AnimationClip _extendedMiniMapClip;
        
        private const float CameraHeight = 10;
        private Transform _cameraTransform;
        private IGameServices _services;
        private EntityView _playerEntityView;
        private bool _miniMapActivated = true;
        
        
        private void Awake()
        {
            _services = MainInstaller.Resolve<IGameServices>();

            _cameraTransform = _camera.transform;
            
            QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
        }
        

        private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
        {
            _playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
            
            _services.TickService.SubscribeOnUpdate(UpdateTick);
        }
        
        private void OnDestroy()
        {
            _services?.TickService?.UnsubscribeOnUpdate(UpdateTick);
        }

        private void UpdateTick(float deltaTime)
        {
            if (_miniMapActivated)
            {
                _playerRadarPing.localPosition = Vector3.zero;
            }
            else
            {
                SetPingPosition(_playerRadarPing, _playerEntityView.transform.position);
            }

            var position =  _miniMapActivated ? _playerEntityView.transform.position : Vector3.zero;
            _cameraTransform.position = new Vector3(position.x, CameraHeight, position.z);
        }
        
        public void ToggleMiniMapView()
        {
            _animation.clip = !_miniMapActivated ? _smallMiniMapClip : _extendedMiniMapClip;
            _animation.Play();

            _miniMapActivated = !_miniMapActivated;
        }
        

        private void SetPingPosition(Transform pingTransform, Vector3 positionWorldSpace)
        {
            var viewportPoint = _camera.WorldToViewportPoint(positionWorldSpace);

            var screenDelta = new Vector2(viewportPoint.x, viewportPoint.y);
            screenDelta.Scale(_radarBackground.rect.size);
            screenDelta -= _radarBackground.rect.size * 0.5f;
            
            pingTransform.localPosition = screenDelta;
        }
    }
}