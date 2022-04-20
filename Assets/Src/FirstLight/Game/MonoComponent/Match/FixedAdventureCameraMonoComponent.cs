using Cinemachine;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent.EntityViews;
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

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private LocalInput _localInput;
		private EntityView _playerView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_localInput = new LocalInput();

			_localInput.Gameplay.SpecialButton0.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton0.canceled += ctx => SetActiveCamera(_adventureCamera);
			_localInput.Gameplay.SpecialButton1.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton1.canceled += ctx => SetActiveCamera(_adventureCamera);

			_localInput.Enable();

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var follow = _entityViewUpdaterService.GetManualView(callback.Entity);
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;

			SetTargetTransform(follow.transform);
			SetActiveCamera(_spawnCamera);

			// We place audio listener roughly "in the player character's head"
			audioListenerTransform.SetParent(follow.transform);
			audioListenerTransform.localPosition = Vector3.up;
			audioListenerTransform.rotation = Quaternion.identity;
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
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

			SetTargetTransform(_playerView.GetComponentInChildren<PlayerCharacterViewMonoComponent>().RootTransform);
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			_playerView = _entityViewUpdaterService.GetManualView(callback.Entity);
			SetTargetTransform(_playerView.transform);

			if (_dataProvider.AppDataProvider.SelectedGameMode.Value == GameMode.Deathmatch)
			{
				SetActiveCamera(_adventureCamera);
			}
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
