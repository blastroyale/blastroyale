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
	public class FixedAdventureCameraMonoComponent: MonoBehaviour
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
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
		}
		
		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var follow = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
			
			SetTargetTransform(follow.transform);
			SetActiveCamera(_spawnCamera);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
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
			SetActiveCamera(_adventureCamera);
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