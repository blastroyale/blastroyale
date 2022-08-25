using Cinemachine;
using FirstLight.FLogger;
using FirstLight.Game.Input;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

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

			input.SpecialButton0.started += _ => SetActiveCamera(_specialAimCamera);
			input.SpecialButton0.canceled += _ => SetActiveCamera(_adventureCamera);
			input.SpecialButton1.started += _ => SetActiveCamera(_specialAimCamera);
			input.SpecialButton1.canceled += _ => SetActiveCamera(_adventureCamera);

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedPlayerChanged);
			_services.MessageBrokerService.Subscribe<SpectateSetCameraMessage>(OnSpectateSetCameraMessage);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
			
			gameObject.SetActive(false);
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator() && !previous.Entity.IsValid)
			{
				// This sets the initial camera when we get the first spectated player in spectate mode
				SetActiveCamera(_spectateCameras[0]);
			}
			RefreshSpectator(next.Transform);
			SnapCamera();
		}

		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
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

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			SetActiveCamera(_spawnCamera);
		}
		
		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			if (callback.Game.Frames.Verified.Context.MapConfig.GameMode == GameMode.Deathmatch)
			{
				SetActiveCamera(_adventureCamera);
			}
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
		{
			SetActiveCamera(_adventureCamera);
		}

		private void RefreshSpectator(Transform t)
		{
			SetAudioListenerTransform(t);
			SetTargetTransform(t);
		}

		private void SetActiveCamera(CinemachineVirtualCamera virtualCamera)
		{
			if (virtualCamera.gameObject == _cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject)
			{
				return;
			}

			_cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject.SetActive(false);
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
			_cinemachineBrain.ActiveVirtualCamera.UpdateCameraState(Vector3.up, 10f);
		}
	}
}