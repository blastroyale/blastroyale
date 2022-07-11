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
		private IGameDataProvider _gameDataProvider;
		private IEntityViewUpdaterService _entityViewUpdaterService;
		private LocalInput _localInput;

		private EntityView _followedPlayerView;
		private EntityRef _followedPlayerEntity;
		private PlayerRef _followedPlayerRef;
		private EntityRef _leaderPlayer;

		private Transform _targetTransform;

		private bool _spectating;
		private FP _visionRangeRadius;

		/// <summary>
		/// EntityView for the player that the camera is currently following
		/// </summary>
		public EntityView FollowedPlayerView => _followedPlayerView;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
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
			QuantumEvent.Subscribe<EventOnPlayerSpawned>(this, OnPlayerSpawned);
			QuantumEvent.Subscribe<EventOnPlayerKilledPlayer>(this, OnPlayerKilledPlayer);
			QuantumEvent.Subscribe<EventOnLocalPlayerSkydiveLand>(this, OnLocalPlayerSkydiveLand);
			QuantumCallback.Subscribe<CallbackUpdateView>(this, OnQuantumUpdateView, onlyIfActiveAndEnabled: true);

			_localInput.Enable();
			_services.MessageBrokerService.Subscribe<SpectateKillerMessage>(OnSpectateKillerMessage);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			gameObject.SetActive(false);
		}

		private void SetFollowedPlayerRef(EntityRef spectatingPlayer)
		{
			_followedPlayerEntity = spectatingPlayer;
			_followedPlayerView = _entityViewUpdaterService.GetManualView(_followedPlayerEntity);
			_followedPlayerRef = GetPlayerFromEntity(QuantumRunner.Default.Game.Frames.Verified, _followedPlayerEntity);
		}

		private void OnSpectatePreviousPlayerMessage(SpectatePreviousPlayerMessage msg)
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			SetFollowedPlayerRef(players[currentIndex - 1 >= 0 ? currentIndex - 1 : players.Count - 1]);
			RefreshSpectator(frame);

			// Hacky way to force the camera to evaluate the blend to the next follow target (so we snap to it)
			_cinemachineBrain.ActiveVirtualCamera.UpdateCameraState(Vector3.up, 10f);
		}

		private void OnSpectateNextPlayerMessage(SpectateNextPlayerMessage msg)
		{
			SpectateNextPlayer();
		}

		private void SpectateNextPlayer()
		{
			var frame = QuantumRunner.Default.Game.Frames.Verified;
			var players = GetPlayerList(frame, out var currentIndex);

			SetFollowedPlayerRef(players[currentIndex + 1 < players.Count ? currentIndex + 1 : 0]);
			RefreshSpectator(frame);

			// Hacky way to force the camera to evaluate the blend to the next follow target (so we snap to it)
			_cinemachineBrain.ActiveVirtualCamera.UpdateCameraState(Vector3.up, 10f);
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

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;

			SetActiveCamera(_adventureCamera);
			ResetLeaderAndKiller(f);

			if (_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				_spectating = true;
				SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
				SpectateNextPlayer();
			}
			else
			{
				var localPlayer = playersData[game.GetLocalPlayers()[0]];

				if (!localPlayer.Entity.IsAlive(f))
				{
					_spectating = true;
					SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
					SpectateNextPlayer();
					return;
				}

				SetFollowedPlayerRef(localPlayer.Entity);
				SetAudioListenerTransform(_followedPlayerView.transform, Vector3.up, Quaternion.identity);
				SetTargetTransform(_followedPlayerView.transform);
			}
		}

		private void ResetLeaderAndKiller(Frame f)
		{
			var game = QuantumRunner.Default.Game;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			gameContainer.GetPlayersMatchData(game.Frames.Verified, out PlayerRef leader);
			var leaderPlayer = playersData[leader];

			_leaderPlayer = leaderPlayer.Entity;
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
			SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
			SetTargetTransform(_followedPlayerView.GetComponentInChildren<PlayerCharacterViewMonoComponent>()
			                                      .RootTransform);
			SetFollowedPlayerRef(callback.EntityKiller);
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			_leaderPlayer = callback.EntityLeader;

			if (callback.EntityDead == _followedPlayerEntity &&
			    _gameDataProvider.AppDataProvider.SelectedGameMode.Value != GameMode.Deathmatch)
			{
				SetFollowedPlayerRef(callback.EntityKiller);
				RefreshSpectator(callback.Game.Frames.Verified);
			}
		}

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			SetFollowedPlayerRef(callback.Entity);
			SetTargetTransform(_followedPlayerView.transform);

			if (callback.Game.Frames.Verified.Context.MapConfig.GameMode == GameMode.Deathmatch)
			{
				SetActiveCamera(_adventureCamera);
			}
		}

		private void OnPlayerSpawned(EventOnPlayerSpawned callback)
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				return;
			}

			ResetLeaderAndKiller(callback.Game.Frames.Verified);
			SetActiveCamera(_adventureCamera);
			SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
			SpectateNextPlayer();
		}

		private void OnSpectateKillerMessage(SpectateKillerMessage message)
		{
			OnSpectate();
		}

		private void OnSpectate()
		{
			_spectating = true;
			RefreshSpectator(QuantumRunner.Default.Game.Frames.Verified);
		}

		private void RefreshSpectator(Frame f)
		{
			if (_spectating)
			{
				if (TryGetNextPlayerView(f, out var entityView))
				{
					SetTargetTransform(entityView.transform);
				}
			}
		}

		private bool TryGetNextPlayerView(Frame f, out EntityView entity)
		{
			if (f.Exists(_followedPlayerEntity))
			{
				entity = _entityViewUpdaterService.GetManualView(_followedPlayerEntity);
				return true;
			}
			else if (_leaderPlayer.IsValid)
			{
				entity = _entityViewUpdaterService.GetManualView(_leaderPlayer);
				return true;
			}

			entity = null;
			return false;
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

			if (_followedPlayerRef != PlayerRef.None)
			{
				_services.MessageBrokerService.Publish(new SpectateTargetSwitchedMessage()
				{
					PlayerFollowed = _followedPlayerRef
				});
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
			var container = f.GetSingleton<GameContainer>();
			var playersData = container.PlayersData;
			currentIndex = -1;

			for (int i = 0; i < playersData.Length; i++)
			{
				var data = playersData[i];
				if (data.IsValid && data.Entity.IsAlive(f))
				{
					players.Add(data.Entity);
				}

				if (_followedPlayerEntity == data.Entity)
				{
					currentIndex = players.Count - 1;
				}
			}

			return players;
		}

		public PlayerRef GetPlayerFromEntity(Frame f, EntityRef entity)
		{
			var players = new List<EntityRef>();
			var container = f.GetSingleton<GameContainer>();
			var playersData = container.PlayersData;

			for (int i = 0; i < playersData.Length; i++)
			{
				var data = playersData[i];

				if (_followedPlayerEntity == data.Entity)
				{
					return i;
				}
			}

			return -1;
		}
	}
}