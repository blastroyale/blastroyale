using System.Collections.Generic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Views
{
    public class RadarMapView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _radarBackground;

        [SerializeField]
        private GameObject _enemyPingPrefab;
        
        
        private const float RadiusWorldSpace = 15f;
        private readonly List<Transform> _players = new List<Transform>(30);
        private readonly List<Transform> _pingTransforms = new List<Transform>();
        private IGameServices _services;
        private EntityView _playerEntityView;
        
        private void Awake()
        {
            _services = MainInstaller.Resolve<IGameServices>();

            QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, HandlePlayerSpawned);
            QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
            QuantumEvent.Subscribe<EventOnPlayerLeft>(this, OnEventOnPlayerLeft);
            QuantumEvent.Subscribe<EventOnPlayerDead>(this, HandleOnPlayerDead);
        }

        private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
        {
            _playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
        }

        private void HandleOnPlayerDead(EventOnPlayerDead callback)
        {
            var playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);

            _players.Remove(playerEntityView.transform);
        }

        private void OnEventOnPlayerLeft(EventOnPlayerLeft callback)
        {
            var playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);

            _players.Remove(playerEntityView.transform);
        }

        private void HandlePlayerSpawned(EventOnPlayerSpawned callback)
        {
            var playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
            
            var frame = callback.Game.Frames.Verified;
            var player = frame.Get<PlayerCharacter>(playerEntityView.EntityRef).Player;

            if (callback.Game.PlayerIsLocal(player))
            {
                return;
            }

            _players.Add(playerEntityView.transform);
        }
        
        
        private void Update()
        {
            if (_playerEntityView == null)
            {
                return;
            }

            RegulatePingInstances();
            PlotPingPositions();
        }

        private void PlotPingPositions()
        {
            var count = _players.Count;
            var originWorldSpace = _playerEntityView.transform.position;
            for (var i = 0; i < count; i++)
            {
                var enemyActor = _players[i];
                var pingTransform = _pingTransforms[i];
                
                pingTransform.gameObject.SetActive(true);
                SetPingPosition(pingTransform, originWorldSpace, enemyActor.transform.position);
            }
        }

        private void SetPingPosition(Transform pingTransform, Vector3 originWorldSpace, Vector3 positionWorldSpace)
        {
            var worldDelta = positionWorldSpace - originWorldSpace;
            worldDelta.y = 0f;
            worldDelta /= RadiusWorldSpace;
            if (worldDelta.sqrMagnitude > 1f)
            {
                //worldDelta.Normalize();
            }

            var screenDelta = new Vector2(worldDelta.x, worldDelta.z);
            screenDelta.Scale(_radarBackground.rect.size * 0.5f);
            pingTransform.localPosition = screenDelta;
        }

        private void RegulatePingInstances()
        {
            var required = _players.Count;
            while (_pingTransforms.Count < required)
            {
                AddPingInstance();
            }

            while (_pingTransforms.Count > required)
            {
                RemovePingInstance();
            }
        }

        private void AddPingInstance()
        {
            _pingTransforms.Add(Instantiate(_enemyPingPrefab, _radarBackground).transform);
        }

        private void RemovePingInstance()
        {
            var lastIndex = _pingTransforms.Count - 1;
            var last = _pingTransforms[lastIndex];
            _pingTransforms.RemoveAt(lastIndex);
            Destroy(last.gameObject);
        }
	}
}