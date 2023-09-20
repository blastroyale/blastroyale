using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
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

		private Room _room;
		private RoomService _roomService;

		public bool GameStarted => Properties.GameStarted.Value;
		public QuantumMapConfig MapConfig => _roomService.GetMapConfig(Properties.MapId.Value.GetHashCode());
		public QuantumGameModeConfig GameModeConfig => _roomService.GetGameModeConfig(Properties.GameModeId.Value);
		public Dictionary<int, Player> Players => _room.Players;
		public string Name => _room.Name;

		public int PlayerCount => _room.PlayerCount;

		public bool IsOffline => _room.IsOffline;

		public GameRoom(RoomService roomService, Room room)
		{
			_roomService = roomService;
			_room = room;
			Properties = new RoomProperties();
		}
		public Player LocalPlayer => _roomService._networkService.LocalPlayer;
        

		/// <summary>
		/// Return when the game starts based on the server time
		/// </summary>
		/// <returns></returns>
		public long GameStartsAt()
		{
			var mp = Properties.SecondsToStart.Value * 1000;
			return Properties.LoadingStartServerTime.Value + Properties.SecondsToStart.Value * 1000;
		}

		public bool ShouldTimerRun()
		{
			return Properties.LoadingStartServerTime.HasValue;
		}

		public TimeSpan TimeLeftToGameStart()
		{
			return TimeSpan.FromMilliseconds(GameStartsAt() - _roomService._networkService.ServerTimeInMilliseconds);
		}

		public bool ShouldGameStart()
		{
			if (GameModeConfig.InstantLoad) return true;
			if (!Properties.LoadingStartServerTime.HasValue)
			{
				return false;
			}

			return GameStartsAt() < _roomService._networkService.ServerTimeInMilliseconds;
		}

		public byte GetMaxPlayers(bool includeSpectators = true)
		{
			return _roomService.GetMaxPlayers(GameModeConfig, MapConfig, Properties.MatchType.Value);
		}

		public int GetMaxSpectators()
		{
			return _roomService.GetMaxSpectators(Properties.MatchType.Value);
		}


		/// <summary>
		/// Obtains room capacity for non-spectator players
		/// </summary>
		public int GetRealPlayerCapacity()
		{
			return _room.MaxPlayers - GetMaxSpectators();
		}

		/// <summary>
		/// Obtains info on whether room has all its player slots full
		/// </summary>
		public bool IsAtFullPlayerCapacity()
		{
			// This is playfab mm
			/*if (room.ShouldUsePlayFabMatchmaking(cfgProvider) && room.ExpectedUsers != null && room.ExpectedUsers.Length > 0)
			{
				bool everyBodyJoined = room.ExpectedUsers
					.All(id => room.Players.Any(p => p.Value.UserId == id));

				bool everybodyLoadedCoreAssets = room.Players.Values.All(p => p.LoadedCoreMatchAssets());
				return everyBodyJoined && everybodyLoadedCoreAssets;
			}*/
			return GetRealPlayerAmount() >= GetRealPlayerCapacity();
		}

		/// <summary>
		/// Obtains amount of spectators players currently in room
		/// </summary>
		public int GetSpectatorAmount()
		{
			int playerAmount = 0;

			foreach (var kvp in _room.Players)
			{
				var isSpectator = (bool) kvp.Value.CustomProperties[GameConstants.Network.PLAYER_PROPS_SPECTATOR];

				if (isSpectator)
				{
					playerAmount++;
				}
			}

			return playerAmount;
		}


		/// <summary>
		/// Obtains amount of non-spectator players currently in room
		/// </summary>
		public int GetRealPlayerAmount()
		{
			var playerAmount = 0;

			foreach (var kvp in _room.Players)
			{
				kvp.Value.CustomProperties.TryGetValue(GameConstants.Network.PLAYER_PROPS_SPECTATOR, out var isSpectator);
				if (isSpectator is null or false)
				{
					playerAmount++;
				}
			}

			return playerAmount;
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


			if (!Properties.HasBots.Value)
			{
				roomSize = playersInRoom;
			}

			var quitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.LeaveRoomAndBecomeInactive;

			FLog.Verbose($"Starting simulation for {roomSize} players");

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
		public void SetRuntimeConfig()
		{
			var op = Addressables.LoadAssetAsync<MapAsset>($"Maps/{Properties.MapId.Value.ToString()}.asset");
			var runtimeConfig = _roomService._configsProvider.GetConfig<QuantumRunnerConfigs>().RuntimeConfig;
			runtimeConfig.Seed = Random.Range(0, int.MaxValue);

			runtimeConfig.MapId = Properties.MapId.Value.GetHashCode();
			runtimeConfig.Map = op.WaitForCompletion().Settings;
			runtimeConfig.GameModeId = Properties.GameModeId.Value;
			runtimeConfig.Mutators = Properties.Mutators.Value.ToArray();
			runtimeConfig.BotOverwriteDifficulty = Properties.BotDifficultyOverwrite.HasValue ? Properties.BotDifficultyOverwrite.Value : -1;
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
				if (playerKvp.Value.IsInactive && playerKvp.Value.UserId == null)
				{
					FLog.Verbose("Inactive player" + playerKvp.Value.LoadedCoreMatchAssets());
					continue;
				}
				
				if (!playerKvp.Value.LoadedCoreMatchAssets())
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
				Mutators = Properties.Mutators.Value.ToList(),
				MapId = Properties.MapId.Value.GetHashCode(),
				MatchType = Properties.MatchType.Value,
				BotDifficultyOverwrite = Properties.BotDifficultyOverwrite.Value,
				GameModeId = Properties.GameModeId.Value,
				RoomIdentifier = _room.Name
			};
		}
	}
}