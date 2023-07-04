using System;
using System.Linq;
using Cinemachine;
using FirstLight.FLogger;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityPrototypes;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
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
		[SerializeField, Required] private CinemachineVirtualCamera _winnerCamera;
		[SerializeField] private CinemachineVirtualCamera[] _spectateCameras;
		//this object is locked to the player and used for more intriacate camera control
		[SerializeField, Required] private GameObject _followObject;

		private IGameServices _services;
		private IMatchServices _matchServices;
		private IGameDataProvider _gameDataProvider;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_matchServices.MatchCameraService.SetCameras(_adventureCamera);

			_matchServices.SpectateService.SpectatedPlayer.InvokeObserve(OnSpectatedPlayerChanged);
			_services.MessageBrokerService.Subscribe<SpectateSetCameraMessage>(OnSpectateSetCameraMessage);
			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStarted);
			_services.MessageBrokerService.Subscribe<WinnerSetCameraMessage>(OnWinnerSetCamera);
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerAlive>(this, OnPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerSkydiveLand>(this, OnPlayerSkydiveLand);
			gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumCallback.UnsubscribeListener(this);
		}
		
		private static GameObject GetFollowObject(SpectatedPlayer player)
		{
			var component = player.Transform.gameObject.GetComponent<PlayerCharacterMonoComponent>();
			if (component == null)
			{
				FLog.Warn("Camera following something that is not a player :L");
				return player.Transform.gameObject;
			}
			return component.Instance.transform.gameObject;
		}

		private void OnSpectatedPlayerChanged(SpectatedPlayer previous, SpectatedPlayer next)
		{
			if (!next.Entity.IsValid) return;

			// If local player died and camera is in spawn mode, reset back to adventure (death upon landing fix)
			if (!_services.NetworkService.LocalPlayer.IsSpectator() && ReferenceEquals(_cinemachineBrain.ActiveVirtualCamera, _spawnCamera))
			{
				SetActiveCamera(_adventureCamera);
			}

			_followObject = GetFollowObject(next);
			RefreshSpectator(_followObject.transform);
			
			//when becoming a spectator, disable camera panning and set the follow target to the spectated player's transform
			if (!_services.NetworkService.LocalPlayer.IsSpectator())
			{
				return;
			}
			QuantumCallback.UnsubscribeListener(this);
			_cinemachineBrain.ActiveVirtualCamera?.SnapCamera();
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
			
			var mainOverlayCamera = Camera.allCameras.FirstOrDefault(go => go.CompareTag("MainOverlayCamera"));
			if (mainOverlayCamera != null)
			{
				mainOverlayCamera.gameObject.SetActive(false);
			}
			
			if (obj.IsResync)
			{
				SetActiveCamera(_adventureCamera);
				_adventureCamera.SnapCamera();
			}
		}

		private void OnWinnerSetCamera(WinnerSetCameraMessage obj)
		{
			_winnerCamera.Follow = obj.WinnerTrasform;
			_winnerCamera.LookAt = obj.WinnerTrasform;
			
			SetActiveCamera(_winnerCamera);
			
			_matchServices?.SpectateService?.SpectatedPlayer?.StopObserving(OnSpectatedPlayerChanged);
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (callback.Game.PlayerIsLocal(callback.Player))
			{
				SetActiveCamera(_spawnCamera);
				_spawnCamera.SnapCamera();
				_cinemachineBrain.ManualUpdate();
				
				SetActiveCamera(_adventureCamera);
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
			if (_cinemachineBrain.ActiveVirtualCamera != null &&
				 virtualCamera.gameObject == _cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject)
			{
				return;
			}

			_cinemachineBrain.ActiveVirtualCamera?.VirtualCameraGameObject.SetActive(false);

			virtualCamera.gameObject.SetActive(true);
		}

		private void SetTargetTransform(Transform t)
		{
			foreach (var cam in _spectateCameras)
			{
				cam.LookAt = t;
				cam.Follow = t;
			}

			_spawnCamera.LookAt = t;
			_spawnCamera.Follow = t;
			_deathCamera.LookAt = t;
			_deathCamera.Follow = t;
			_adventureCamera.LookAt = t;
			_adventureCamera.Follow = _followObject.transform;
			_specialAimCamera.LookAt = t;
			_specialAimCamera.Follow = t;
		}

		private void SetAudioListenerTransform(Transform t)
		{
			var eulerAngles = t.rotation.eulerAngles;
			var flatRotation = Quaternion.Euler(0, eulerAngles.y, 0);

			var audioListener = _services.AudioFxService.AudioListener;
			audioListener.SetFollowTarget(t, Vector3.up, flatRotation);
		}
	}
}