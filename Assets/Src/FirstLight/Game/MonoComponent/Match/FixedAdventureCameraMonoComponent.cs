using System;
using System.Collections.Generic;
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
		[SerializeField] private CinemachineVirtualCamera[] _spectateCameras;

		private IGameServices _services;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private LocalInput _localInput;
		private EntityView _playerView;

		private EntityRef _spectatingPlayer;
		private EntityRef _leader;

		private Transform _targetTransform;

		private bool _spectating;
		private FP _visionRangeRadius;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_localInput = new LocalInput();
			_visionRangeRadius = _services.ConfigsProvider.GetConfig<QuantumGameConfig>().PlayerVisionRange;

			_localInput.Gameplay.SpecialButton0.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton0.canceled += ctx => SetActiveCamera(_adventureCamera);
			_localInput.Gameplay.SpecialButton1.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton1.canceled += ctx => SetActiveCamera(_adventureCamera);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);
			_services.MessageBrokerService.Subscribe<SpectateSetCameraMessage>(OnSpectateSetCameraMessage);
			_services.MessageBrokerService.Subscribe<SpectateNextPlayerMessage>(OnSpectateNextPlayerMessage);
			_services.MessageBrokerService.Subscribe<SpectatePreviousPlayerMessage>(OnSpectatePreviousPlayerMessage);

			QuantumEvent.Subscribe<EventOnLocalPlayerSpawned>(this, OnLocalPlayerSpawned);
			QuantumEvent.Subscribe<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.Subscribe<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnQuantumUpdateView, onlyIfActiveAndEnabled: true);

			_localInput.Enable();
			_services.MessageBrokerService.Subscribe<SpectateKillerMessage>(OnSpectate);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			gameObject.SetActive(false);
		}

		private void OnSpectatePreviousPlayerMessage(SpectatePreviousPlayerMessage obj)
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			_spectatingPlayer = players[currentIndex - 1 >= 0 ? currentIndex - 1 : players.Count - 1];
			RefreshSpectator(frame);
		}

		private void OnSpectateNextPlayerMessage(SpectateNextPlayerMessage obj)
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			_spectatingPlayer = players[currentIndex + 1 < players.Count ? currentIndex + 1 : 0];
			RefreshSpectator(frame);
		}

		private void OnSpectateSetCameraMessage(SpectateSetCameraMessage obj)
		{
			SetActiveCamera(_spectateCameras[obj.CameraId]);
		}

		private void OnMatchSimulationStartedMessage(MatchSimulationStartedMessage obj)
		{
			gameObject.SetActive(true);
		}

		private void OnQuantumUpdateView(CallbackUpdateView callback)
		{
			if (_targetTransform != null)
			{
				callback.Game.SetPredictionArea(_targetTransform.position.ToFPVector3(), _visionRangeRadius);
			}
		}

		private void OnDestroy()
		{
			_localInput?.Dispose();
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync)
			{
				return;
			}

			SetActiveCamera(_adventureCamera);

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			if (!localPlayer.Entity.IsAlive(f))
			{
				gameContainer.GetPlayersMatchData(game.Frames.Verified, out PlayerRef leader);
				var leaderPlayer = playersData[leader];
				_leader = leaderPlayer.Entity;
				_spectatingPlayer = leaderPlayer.Entity;

				SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
				OnSpectate();

				return;
			}

			_playerView = _entityViewUpdaterService.GetManualView(localPlayer.Entity);

			// We place audio listener roughly "in the player character's head"
			SetAudioListenerTransform(_playerView.transform, Vector3.up, Quaternion.identity);
			SetTargetTransform(_playerView.transform);
		}

		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var follow = _entityViewUpdaterService.GetManualView(callback.Entity);

			// We place audio listener roughly "in the player character's head"
			SetAudioListenerTransform(follow.transform, Vector3.up, Quaternion.identity);
			SetTargetTransform(follow.transform);
			SetActiveCamera(_spawnCamera);
		}

		private void OnLocalPlayerSkydiveLand(EventOnLocalPlayerSkydiveLand callback)
		{
			SetActiveCamera(_adventureCamera);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			// We place audio listener back to main camera
			SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
			SetTargetTransform(_playerView.GetComponentInChildren<PlayerCharacterViewMonoComponent>().RootTransform);

			_spectatingPlayer = callback.EntityKiller;
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			_leader = callback.EntityLeader;

			if (callback.EntityDead == _spectatingPlayer)
			{
				_spectatingPlayer = callback.EntityKiller;
				RefreshSpectator(callback.Game.Frames.Verified);
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

		private void OnSpectate()
		{
			_spectating = true;
			RefreshSpectator(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void OnSpectate(SpectateKillerMessage message)
		{
			_spectating = true;
			RefreshSpectator(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void RefreshSpectator(Frame f)
		{
			if (_spectating)
			{
				var nextPlayerView = GetNextPlayerView(f);
				SetTargetTransform(nextPlayerView.transform);
			}
		}

		private EntityView GetNextPlayerView(Frame f)
		{
			EntityView nextPlayer = null;

			if (f.Exists(_spectatingPlayer))
			{
				nextPlayer = _entityViewUpdaterService.GetManualView(_spectatingPlayer);
			}
			else if (_leader.IsValid)
			{
				nextPlayer = _entityViewUpdaterService.GetManualView(_leader);
			}

			return nextPlayer;
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
			_spawnCamera.LookAt = entityViewTransform;
			_spawnCamera.Follow = entityViewTransform;
			_deathCamera.LookAt = entityViewTransform;
			_deathCamera.Follow = entityViewTransform;
			_adventureCamera.LookAt = entityViewTransform;
			_adventureCamera.Follow = entityViewTransform;
			_specialAimCamera.LookAt = entityViewTransform;
			_specialAimCamera.Follow = entityViewTransform;

			foreach (var cam in _spectateCameras)
			{
				cam.LookAt = entityViewTransform;
				cam.Follow = entityViewTransform;
			}
		}

		private void SetAudioListenerTransform(Transform t, Vector3 pos, Quaternion rot)
		{
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;
			audioListenerTransform.SetParent(t);
			audioListenerTransform.localPosition = pos;
			audioListenerTransform.rotation = rot;
		}

		private List<EntityRef> GetPlayerList(Frame f, out int currentIndex)
		{
			var players = new List<EntityRef>();


			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var container = frame.GetSingleton<GameContainer>();
			var matchData = container.GetPlayersMatchData(frame, out _);

			foreach (var data in matchData)
			{
				if (data.Data.IsValid && data.Data.Entity.IsAlive(f))
				{
					players.Add(data.Data.Entity);
				}
			}

			currentIndex = players.IndexOf(_spectatingPlayer);
			return players;
		}
	}
}