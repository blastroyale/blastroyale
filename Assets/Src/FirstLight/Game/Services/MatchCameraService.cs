using Cinemachine;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Service to abstract several camera features
	/// </summary>
	public interface IMatchCameraService
	{
		void StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes shape, float duration, float strength,
							  Vector3 position = default);
	}
	
	/// <inheritdoc />
	public class MatchCameraService : IMatchCameraService, MatchServices.IMatchService
	{
		private IGameDataProvider _gameDataProvider;
		private IMatchServices _matchServices;
		private IGameServices _services;
		
		private CinemachineImpulseSource _impulseSource;
		private GameObject _cameraServiceObject;
		private GameObject _followObject;
		
		public MatchCameraService(IGameDataProvider gameDataProvider, IMatchServices matchServices, IGameServices services)
		{
			_gameDataProvider = gameDataProvider;
			_matchServices = matchServices;
			_services = services;
			_cameraServiceObject = new GameObject("CameraService", typeof(CinemachineImpulseSource));
			_impulseSource = _cameraServiceObject.GetComponent<CinemachineImpulseSource>();
		}

		public void StartScreenShake(CinemachineImpulseDefinition.ImpulseShapes shape, float duration, float strength, Vector3 position = default)
		{
			if(!_gameDataProvider.AppDataProvider.UseScreenShake)
				return;

			var newImpulse = new CinemachineImpulseDefinition
			{
				m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform,
				m_DissipationRate = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_RATE_DEFAULT,
				m_ImpulseShape = shape,
				m_ImpulseDuration = duration,
				m_DissipationDistance = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_DISTANCE_MAX,
				m_ImpactRadius = GameConstants.Screenshake.SCREENSHAKE_DISSAPATION_DISTANCE_MIN,
			};

			var vel = Random.insideUnitCircle.normalized;
			_impulseSource.m_ImpulseDefinition = newImpulse;
			
			if(position == default && _followObject != null & _followObject != null)
			{
				position = _followObject.transform.position;
			}

			_impulseSource.GenerateImpulseAtPositionWithVelocity(position, new Vector3(vel.x, 0, vel.y) * strength);
		}
		
		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			if (_services.NetworkService.LocalPlayer.IsSpectator())
			{
				_followObject = next.Transform.gameObject;
			}
		}

		public void Dispose()
		{
			Object.Destroy(_cameraServiceObject);
		}

		public void OnMatchStarted(QuantumGame game, bool isReconnect)
		{
			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
		}

		public void OnMatchEnded(QuantumGame game, bool isDisconnected)
		{
			_matchServices.SpectateService.SpectatedPlayer.StopObserving(OnSpectatedPlayerChanged);
		}
	}
}