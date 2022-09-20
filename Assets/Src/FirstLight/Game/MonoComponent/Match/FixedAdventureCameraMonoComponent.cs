using System.Threading.Tasks;
using Cinemachine;
using FirstLight.FLogger;
using FirstLight.Game.Input;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// This Mono Component controls the main camera behaviour throughout the game
	/// </summary>
	public class FixedAdventureCameraMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private CinemachineBrain _cinemachineBrain;
		[SerializeField, Required] private CinemachineVirtualCamera _spawnCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _adventureCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _deathCamera;
		[SerializeField, Required] private CinemachineVirtualCamera _specialAimCamera;
		[SerializeField] private CinemachineVirtualCamera[] _spectateCameras;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private bool _spectating;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();

			var input = _services.PlayerInputService.Input.Gameplay;

			input.SpecialButton0.started += SetActiveCamera;
			input.SpecialButton0.canceled += SetActiveCamera;
			input.SpecialButton1.started += SetActiveCamera;
			input.SpecialButton1.canceled += SetActiveCamera;
			input.CancelButton.canceled += SetActiveCamera;

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
			_services.MessageBrokerService.Subscribe<SpectateSetCameraMessage>(OnSpectateSetCameraMessage);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLand);

			gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			var input = _services?.PlayerInputService?.Input?.Gameplay;

			if (input.HasValue)
			{
				input.Value.SpecialButton0.started -= SetActiveCamera;
				input.Value.SpecialButton0.canceled -= SetActiveCamera;
				input.Value.SpecialButton1.started -= SetActiveCamera;
				input.Value.SpecialButton1.canceled -= SetActiveCamera;
				input.Value.CancelButton.canceled -= SetActiveCamera;
			}
			
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			if (!next.Entity.IsValid) return;
			
			RefreshSpectator(next.Transform);
			SnapCamera();
		}

		private void SetActiveCamera(InputAction.CallbackContext context)
		{
			SetActiveCamera(context.canceled ? _adventureCamera : _specialAimCamera);
		}

		private void OnSpectateSetCameraMessage(SpectateSetCameraMessage obj)
		{
			SetActiveCamera(_spectateCameras[obj.CameraId]);
		}

		private void OnMatchStarted(MatchStartedMessage obj)
		{
			gameObject.SetActive(true);
			
			if (obj.IsResync)
			{
				SetActiveCamera(_adventureCamera);
				SnapCamera();
			}
		}
		
		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (callback.Game.PlayerIsLocal(callback.Player))
			{
				SetActiveCamera(_spawnCamera);
			}
		}

		private void OnPlayerAlive(EventOnPlayerAlive callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Player != callback.Player) return;
			
			SetActiveCamera(_adventureCamera);
		}

		private void OnPlayerSkydiveLand(EventOnPlayerSkydiveLand callback)
		{
			if (_matchServices.SpectateService.SpectatedPlayer.Value.Player != callback.Player) return;
			
			SetActiveCamera(_adventureCamera);
		}

		private void RefreshSpectator(Transform t)
		{
			SetAudioListenerTransform(t);
			SetTargetTransform(t);
		}

		private void SetActiveCamera(CinemachineVirtualCamera virtualCamera)
		{
			if ( _cinemachineBrain.ActiveVirtualCamera != null &&
			     virtualCamera.gameObject == _cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject)
			{
				return;
			}

			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);

			virtualCamera.gameObject.SetActive(true);
		}

		private void SetTargetTransform(Transform t)
		{
			_spawnCamera.LookAt = t;
			_spawnCamera.Follow = t;
			_deathCamera.LookAt = t;
			_deathCamera.Follow = t;
			_adventureCamera.LookAt = t;
			_adventureCamera.Follow = t;
			_specialAimCamera.LookAt = t;
			_specialAimCamera.Follow = t;

			foreach (var cam in _spectateCameras)
			{
				cam.LookAt = t;
				cam.Follow = t;
			}
		}

		private void SetAudioListenerTransform(Transform t)
		{
			var eulerAngles = t.rotation.eulerAngles;
			var flatRotation = Quaternion.Euler(0, eulerAngles.y, 0);

			var audioListener = _services.AudioFxService.AudioListener;
			audioListener.SetFollowTarget(t, Vector3.up, flatRotation);
		}

		private void SnapCamera()
		{
			// Hacky way to force the camera to evaluate the blend to the next follow target (so we snap to it)
			_cinemachineBrain.ActiveVirtualCamera?.UpdateCameraState(Vector3.up, 10f);
		}
	}
}