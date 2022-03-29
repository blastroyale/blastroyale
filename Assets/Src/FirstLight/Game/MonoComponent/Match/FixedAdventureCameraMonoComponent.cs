using Cinemachine;
using FirstLight.Game.Input;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Match
{
	/// <summary>
	/// This Mono Component controls the main camera behaviour throughout the game
	/// </summary>
	public class FixedAdventureCameraMonoComponent : MonoBehaviour
	{
		[SerializeField] private CinemachineBrain _cinemachineBrain;
		[SerializeField] private CinemachineVirtualCamera _spawnCamera;
		[SerializeField] private CinemachineVirtualCamera _adventureCamera;
		[SerializeField] private CinemachineVirtualCamera _deathCamera;
		[SerializeField] private CinemachineVirtualCamera _specialAimCamera;

		private IGameServices _services;
		private LocalInput _localInput;
		private PlayerCharacterViewMonoComponent _playerCharacterView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_localInput = new LocalInput();

			_localInput.Gameplay.SpecialButton0.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton0.canceled += ctx => SetActiveCamera(_adventureCamera);
			_localInput.Gameplay.SpecialButton1.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton1.canceled += ctx => SetActiveCamera(_adventureCamera);

			_localInput.Enable();

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.Subscribe<EventOnLocalPlayerLanded>(this, OnLocalPlayerLanded);
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var follow = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;

			SetTargetTransform(follow.transform);
			SetActiveCamera(_spawnCamera);

			// We place audio listener roughly "in the player character's head"
			audioListenerTransform.SetParent(follow.transform);
			audioListenerTransform.localPosition = Vector3.up;
			audioListenerTransform.rotation = Quaternion.identity;
		}

		private void OnLocalPlayerLanded(EventOnLocalPlayerLanded callback)
		{
			SetActiveCamera(_adventureCamera);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			// We place audio listener back to main camera
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;
			audioListenerTransform.SetParent(Camera.main.transform);
			audioListenerTransform.position = Vector3.zero;
			audioListenerTransform.rotation = Quaternion.identity;

			SetTargetTransform(_playerCharacterView.RootTransform);
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			var entityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);

			if (_playerCharacterView == null)
			{
				_playerCharacterView = entityView.GetComponentInChildren<PlayerCharacterViewMonoComponent>();
			}

			SetTargetTransform(entityView.transform);
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

		private void SetTargetTransform(Transform entityViewTransform)
		{
			_spawnCamera.LookAt = entityViewTransform;
			_spawnCamera.Follow = entityViewTransform;
			_deathCamera.LookAt = entityViewTransform;
			_deathCamera.Follow = entityViewTransform;
			_adventureCamera.LookAt = entityViewTransform;
			_adventureCamera.Follow = entityViewTransform;
			_specialAimCamera.LookAt = entityViewTransform;
			_specialAimCamera.Follow = entityViewTransform;
		}
	}
}