using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.Core;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Services.RoomService
{
	public class GameRoom
	{
		public RoomProperties Properties { get; private set; }
		private Dictionary<int, PlayerProperties> _playerProperties = new ();

		private Room _room;
		private RoomService _roomService;
		private bool _pause;

		public bool GameStarted => Properties.GameStarted.Value;
		public bool IsTeamGame => Properties.SimulationMatchConfig.Value.TeamSize > 1;
		public QuantumMapConfig MapConfig => _roomService.GetMapConfig(Properties.SimulationMatchConfig.Value.MapId);
		public QuantumGameModeConfig GameModeConfig => _roomService.GetGameModeConfig(Properties.SimulationMatchConfig.Value.GameModeID);
		public Dictionary<int, Player> Players => _room.Players;
		public string Name => _room.Name;

		public int PlayerCount => _room.PlayerCount;

		public int MasterClientId => _room.MasterClientId;

		public bool IsOffline => _room.IsOffline;

		public GameRoom(RoomService roomService, Room room)
		{
			_roomService = roomService;
			_room = room;
			Properties = new RoomProperties();
		}

		public Player LocalPlayer => _roomService._networkService.LocalPlayer;
		public PlayerProperties LocalPlayerProperties => GetPlayerProperties(LocalPlayer);

		/// <summary>
		/// Return when the game starts based on the server time
		/// </summary>
		/// <returns></returns>
		public long GameStartsAt()
		{
			return Properties.LoadingStartServerTime.Value + Properties.SecondsToStart.Value * 1000;
		}

		public bool ShouldTimerRun()
		{
			return Properties.LoadingStartServerTime.HasValue && !_pause;
		}

		public TimeSpan TimeLeftToGameStart()
		{
			return TimeSpan.FromMilliseconds(GameStartsAt() - _roomService._networkService.ServerTimeInMilliseconds);
		}

		public bool ShouldGameStart()
		{
			if (GameModeConfig.InstantLoad) return true;
			if ((RoomService.AutoStartWhenLoaded || FeatureFlags.GetLocalConfiguration().SkipMatchLoadTimer) && AreAllPlayersReady()) return true;
			if (_pause) return false;
			if (!Properties.LoadingStartServerTime.HasValue)
			{
				return false;
			}

			return GameStartsAt() < _roomService._networkService.ServerTimeInMilliseconds;
		}

		public int GetMaxPlayers()
		{
			return _room.MaxPlayers - GetMaxSpectators();
		}

		public int GetMaxSpectators()
		{
			return _roomService.GetMaxSpectators(Properties.SimulationMatchConfig.Value.MatchType);
		}

		/// <summary>
		/// Obtains room capacity for non-spectator players
		/// </summary>
		public int GetRealPlayerCapacity()
		{
			return _room.MaxPlayers - GetMaxSpectators();
		}

		/// <summary>
		/// Obtains amount of spectators players currently in room
		/// </summary>
		public int GetSpectatorAmount()
		{
			return _room.Players.Values.Count(player => GetPlayerProperties(player).Spectator.Value);
		}

		/// <summary>
		/// Obtains amount of non-spectator players currently in room
		/// </summary>
		public int GetRealPlayerAmount()
		{
			return _room.Players.Values.Count(player => !GetPlayerProperties(player).Spectator.Value);
		}

		/// <inheritdoc cref="QuantumRunner.StartParameters"/>
		/// <remarks>
		/// Default values to start the Quantum simulation based on the current selected adventure
		/// </remarks>
		public QuantumRunner.StartParameters GetDefaultStartParameters()
		{
			var runnerConfigs = _roomService._configsProvider.GetConfig<QuantumRunnerConfigs>();

			var playersInRoom = GetRealPlayerAmount();
			var roomSize = GetRealPlayerCapacity();
			var gameMode = DeterministicGameMode.Multiplayer;

			if (Properties.SimulationMatchConfig.Value.DisableBots)
			{
				roomSize = playersInRoom;
			}

			var quitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.LeaveRoomAndBecomeInactive;

			FLog.Info($"Starting simulation for {roomSize} players");

			if (_room.IsOffline)
			{
				gameMode = DeterministicGameMode.Local;
				quitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.None;
			}
			
			var recordInput = FeatureFlags.GetLocalConfiguration().RecordQuantumInput;
			return new QuantumRunner.StartParameters
			{
				RuntimeConfig = runnerConfigs.RuntimeConfig,
				DeterministicConfig = runnerConfigs.DeterministicSessionConfigAsset.Config,
				ReplayProvider = null,
				GameMode = gameMode,
				RunnerId = "DEFAULT",
				QuitBehaviour = quitBehaviour,
				LocalPlayerCount = 1,
				StartGameTimeoutInSeconds = 15,
				RecordingFlags = recordInput ? RecordingFlags.Input : RecordingFlags.None,
				ResourceManagerOverride = null,
				InstantReplayConfig = default,
				HeapExtraCount = 0,
				PlayerCount = roomSize
			};
		}

		/// <summary>
		/// Defines the <see cref="RuntimeConfig"/> to set on the Quantum's simulation when starting
		/// </summary>
		public void SetRuntimeConfig(MapAsset mapAsset)
		{
			var simulationConfig = Properties.SimulationMatchConfig.Value;
			var runtimeConfig = _roomService._configsProvider.GetConfig<QuantumRunnerConfigs>().RuntimeConfig;
			if (FeatureFlags.GetLocalConfiguration().FixedQuantumSeed)
			{
				runtimeConfig.Seed = 42;
			}
			else
			{
				runtimeConfig.Seed = Random.Range(0, int.MaxValue);
			}

			runtimeConfig.MatchConfigs = simulationConfig;
			runtimeConfig.Map = mapAsset.Settings;
		}

		/// <summary>
		/// Requests the current state of the given <paramref name="room"/> if it is ready to start the game or not
		/// based on loading state of all players assets
		/// </summary>
		public bool AreAllPlayersReady()
		{
			foreach (var playerKvp in _room.Players)
			{
				// We check userid null because that means player is joining first time
				// if userid is not null means he entered the room then left, in this case room should start without him
				// with the player being inactive so he can join later
				var loaded = GetPlayerProperties(playerKvp.Value).CoreLoaded.Value;
				if (playerKvp.Value.IsInactive && playerKvp.Value.UserId == null)
				{
					FLog.Verbose("Inactive player" + loaded);
					continue;
				}

				if (!loaded)
				{
					return false;
				}
			}

			return true;
		}

		public string GetRoomName()
		{
			return _room.GetRoomName();
		}

		public MatchRoomSetup ToMatchSetup()
		{
			return new MatchRoomSetup()
			{
				RoomIdentifier = _room.Name,
				SimulationConfig = Properties.SimulationMatchConfig.Value
			};
		}

		public PlayerProperties GetPlayerProperties(Player player)
		{
			if (_playerProperties.TryGetValue(player.ActorNumber, out var properties))
			{
				return properties;
			}

			var newPlayerProps = new PlayerProperties();
			_playerProperties.Add(player.ActorNumber, newPlayerProps);
			return newPlayerProps;
		}

		private bool IsTimerPaused() => _pause;

		/// <summary>
		/// Pauses the room timer.
		/// This property is not synced, only work for rooms with one player
		/// </summary>
		public void PauseTimer()
		{
			_pause = true;
		}

		public void ResumeTimer(int secondsToStart = -1)
		{
			_pause = false;
			Properties.LoadingStartServerTime.Value = _roomService._networkService.ServerTimeInMilliseconds;
			if (secondsToStart == -1)
			{
				return;
			}

			Properties.SecondsToStart.Value = secondsToStart;
		}
	}
}