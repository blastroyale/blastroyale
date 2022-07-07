using System;
using System.Threading.Tasks;
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
		private EntityRef _leader;

		private Transform _targetTransform;

		private bool _spectating;
		private FP _visionRangeRadius;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_entityViewUpdaterService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			_localInput = new LocalInput();
			_visionRangeRadius = _services.ConfigsProvider.GetConfig<QuantumGameConfig>().PlayerVisionRange;

			_localInput.Gameplay.SpecialButton0.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton0.canceled += ctx => SetActiveCamera(_adventureCamera);
			_localInput.Gameplay.SpecialButton1.started += ctx => SetActiveCamera(_specialAimCamera);
			_localInput.Gameplay.SpecialButton1.canceled += ctx => SetActiveCamera(_adventureCamera);

			_services.MessageBrokerService.Subscribe<MatchStartedMessage>(OnMatchStartedMessage);

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

		private async void OnMatchStartedMessage(MatchStartedMessage msg)
		{
			if (!msg.IsResync && !msg.IsSpectator)
			{
				return;
			}

			SetActiveCamera(_adventureCamera);

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			var localPlayer = playersData[game.GetLocalPlayers()[0]];

			if (msg.IsSpectator)
			{
				await Task.Delay(3000);
				ResetLeaderAndKiller();

				_playerView = _entityViewUpdaterService.GetManualView(_leader);
				
				// We place audio listener roughly "in the player character's head"
				SetAudioListenerTransform(_playerView.transform, Vector3.up, Quaternion.identity);
				SetTargetTransform(_playerView.transform);
			}
			else
			{
				if (!localPlayer.Entity.IsAlive(f))
				{
					ResetLeaderAndKiller();
					SetAudioListenerTransform(Camera.main.transform, Vector3.zero, Quaternion.identity);
					OnSpectate();
					return;
				}

				_playerView = _entityViewUpdaterService.GetManualView(localPlayer.Entity);
				
				// We place audio listener roughly "in the player character's head"
				SetAudioListenerTransform(_playerView.transform, Vector3.up, Quaternion.identity);
				SetTargetTransform(_playerView.transform);
			}
		}

		private void ResetLeaderAndKiller()
		{
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			gameContainer.GetPlayersMatchData(game.Frames.Verified, out PlayerRef leader);
			var leaderPlayer = playersData[leader];
			
			_leader = leaderPlayer.Entity;
			_latestKiller = _leader;
		}
		
		private void OnLocalPlayerSpawned(EventOnLocalPlayerSpawned callback)
		{
			var follow = _entityViewUpdaterService.GetManualView(callback.Entity);
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;
			
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var playersData = gameContainer.PlayersData;
			gameContainer.GetPlayersMatchData(game.Frames.Verified, out PlayerRef leader);
			
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

			_latestKiller = callback.EntityKiller;
		}

		private void OnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			_leader = callback.EntityLeader;

			if (callback.EntityDead == _latestKiller)
			{
				_latestKiller = callback.EntityKiller;
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

				if (nextPlayerView != null)
				{
					SetTargetTransform(nextPlayerView.transform);
				}
			}
		}

		private EntityView GetNextPlayerView(Frame f)
		{
			EntityView nextPlayer = null;

			if (f.Exists(_latestKiller))
			{
				nextPlayer = _entityViewUpdaterService.GetManualView(_latestKiller);
			}
			else if (_leader.IsValid)
			{
				nextPlayer = _entityViewUpdaterService.GetManualView(_leader);
			}
			else
			{
				
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
		}

		private void SetAudioListenerTransform(Transform t, Vector3 pos, Quaternion rot)
		{
			var audioListenerTransform = _services.AudioFxService.AudioListener.transform;
			audioListenerTransform.SetParent(t);
			audioListenerTransform.localPosition = pos;
			audioListenerTransform.rotation = rot;
		}
	}
}