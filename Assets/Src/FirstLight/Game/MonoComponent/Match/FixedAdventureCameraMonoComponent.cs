using Cinemachine;
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
	/// TODO: Refactor this MonoComponent to work with the state machine instead. This Component is controlling the game
	/// TODO: state flow with other Views (ScoreHolderView) and that is dangerous
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
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private LocalInput _localInput;

		private bool _spectating;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_localInput = new LocalInput();

			_localInput.Gameplay.SpecialButton0.started += _ => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton0.canceled += _ => SetActiveCamera(_adventureCamera);
			_localInput.Gameplay.SpecialButton1.started += _ => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton1.canceled += _ => SetActiveCamera(_adventureCamera);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			_services.MessageBrokerService.Subscribe<SpectateSetCameraMessage>(OnSpectateSetCameraMessage);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);

			_matchServices.SpectateService.SpectatedPlayer.Observe(OnSpectatedTransformChanged);

			_localInput.Enable();
			// _services.MessageBrokerService.Subscribe<SpectateStartedMessage>(OnSpectateStartedMessage);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			gameObject.SetActive(false);
		}

		private void OnSpectatedTransformChanged(ObservedPlayer previous, ObservedPlayer next)
		{
			RefreshSpectator(next.Transform);

			// Hacky way to force the camera to evaluate the blend to the next follow target (so we snap to it)
			_cinemachineBrain.ActiveVirtualCamera.UpdateCameraState(Vector3.up, 10f);
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private void OnSpectateSetCameraMessage(SpectateSetCameraMessage obj)
		{
			SetActiveCamera(_spectateCameras[obj.CameraId]);
		}

		private void OnMatchSimulationStartedMessage(MatchSimulationStartedMessage obj)
		{
			gameObject.SetActive(true);
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync)
			{
				return;
			}

			SetActiveCamera(_adventureCamera);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			SetActiveCamera(_spawnCamera);
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
		{
			SetActiveCamera(_adventureCamera);
		}
		//
		// private void OnSpectateStartedMessage(SpectateStartedMessage message)
		// {
		// 	// TODO: Probably not needed
		// 	SetActiveCamera(_adventureCamera);
		// }

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
			Vector3 eulerAngles = t.rotation.eulerAngles;
			Quaternion flatRotation = Quaternion.Euler(0, eulerAngles.y, 0);

			var audioListener = _services.AudioFxService.AudioListener;
			audioListener.SetFollowTarget(t, Vector3.up, flatRotation);
		}
	}
}