using System.Collections;
using Cinemachine;
using FirstLight.Game.Input;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Adventure
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
		private CinemachineBasicMultiChannelPerlin _channelPerlinNoise;
		private LocalInput _localInput;
		private EntityView _playerEntityView;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_localInput = new LocalInput();
			_channelPerlinNoise = _adventureCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
			
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
			_playerEntityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
			
			SetTargetTransform(_playerEntityView.transform);
			SetActiveCamera(_spawnCamera);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			var follow = _playerEntityView.GetComponentInChildren<PlayerCharacterViewMonoComponent>().RootTransform;
			
			SetTargetTransform(follow);
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			var entityView = _services.EntityViewUpdaterService.GetManualView(callback.Entity);
			
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