using System;
using Cinemachine;
using FirstLight.Game.Input;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Photon.Deterministic;
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

		private EntityView _spectatePlayerView;
		private EntityRef _latestKiller;

		private bool _hasTarget;
		private Transform _targetTransform;

		private bool _spectating;
		private FP _visionRangeRadius;

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

			_services.MessageBrokerService.Subscribe<MatchReadyForResyncMessage>(OnMatchReadyForResyncMessage);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnQuantumUpdateView);

			_services.MessageBrokerService.Subscribe<SpectateKillerMessage>(OnSpectate);
			
			_visionRangeRadius = _services.ConfigsProvider.GetConfig<QuantumGameConfig>().PlayerVisionRange;
		}

		private void OnQuantumUpdateView(CallbackUpdateView callback)
		{
			if (_hasTarget)
			{
				callback.Game.SetPredictionArea(_targetTransform.position.ToFPVector3(), _visionRangeRadius);
			}
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
			_services.MessageBrokerService.UnsubscribeAll();
		}

		private void OnMatchReadyForResyncMessage(MatchReadyForResyncMessage msg)
		{
			// This method is only for when rejoining rooms
			if (_services.NetworkService.IsJoiningNewRoom)
			{
				return;
			}

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];
			
			_playerView = _entityViewUpdaterService.GetManualView(localPlayer.Entity);
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;

			SetTargetTransform(_playerView.transform);
			SetActiveCamera(_adventureCamera);
			
			// We place audio listener roughly "in the player character's head"
			audioListenerTransform.SetParent(_playerView.transform);
			audioListenerTransform.localPosition = Vector3.up;
			audioListenerTransform.rotation = Quaternion.identity;
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

			_latestKiller = callback.EntityKiller;
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			if (callback.EntityDead == _latestKiller)
			{
				_latestKiller = callback.EntityKiller;
				RefreshSpectator();
			}
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			_playerView = _entityViewUpdaterService.GetManualView(callback.Entity);
			SetTargetTransform(_playerView.transform);

			if (callback.Game.Frames.Verified.Context.MapConfig.GameMode == GameMode.Deathmatch)
			{
				SetActiveCamera(_adventureCamera);
			}
		}

		private void OnSpectate(SpectateKillerMessage message)
		{
			_spectating = true;
			RefreshSpectator();
		}

		private void RefreshSpectator()
		{
			if (_spectating)
			{
				SetTargetTransform(_entityViewUpdaterService.GetManualView(_latestKiller).transform);
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
			_targetTransform = entityViewTransform;
			_hasTarget = true;
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
