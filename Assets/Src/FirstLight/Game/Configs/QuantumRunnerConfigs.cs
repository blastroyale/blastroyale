using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using Quantum.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Configs
{
	/// <summary>
	/// Scriptable object with the necessary config data to start the <see cref="QuantumRunner"/>
	/// </summary>
	[CreateAssetMenu(fileName = "QuantumRunner Configs", menuName = "ScriptableObjects/QuantumRunner Configs")]
	public class QuantumRunnerConfigs : ScriptableObject
	{
		private const string _roomPropertyKeyStartTime = "t";
		private const string _roomPropertyKeyGitCommit = "g";
		private const string _roomPropertyKeyMap = "m";
		private const string _roomPropertyKeyDevMode = "dev";
		
		[SerializeField] private RuntimeConfig _runtimeConfig;
		[SerializeField] private DeterministicSessionConfigAsset _deterministicConfigAsset;
		[SerializeField] private PhotonServerSettings _serverSettings;

		/// <inheritdoc cref="DeterministicSessionConfigAsset"/>
		public DeterministicSessionConfigAsset DeterministicSessionConfigAsset => _deterministicConfigAsset;
		/// <inheritdoc cref="PhotonServerSettings"/>
		public PhotonServerSettings PhotonServerSettings => _serverSettings;
		/// <summary>
		/// Marks the Quantum simulation to run in offline or online mode
		/// </summary>
		public bool IsOfflineMode { get; set; } = false;
		/// <summary>
		/// Marks the Quantum simulation to run in dev mode
		/// </summary>
		public bool IsDevMode { get; set; } = false;
		/// <summary>
		/// Returns the <see cref="RuntimeConfig"/> used to build the simulation from the client side
		/// </summary>
		public RuntimeConfig RuntimeConfig => _runtimeConfig;

		/// <summary>
		/// Defines the <see cref="RuntimeConfig"/> to set on the Quantum's simulation when starting
		/// </summary>
		public void SetRuntimeConfig(MapConfig config)
		{
			var op = Addressables.LoadAssetAsync<MapAsset>($"Maps/{config.Map.ToString()}.asset");
			
			_runtimeConfig.Seed = Random.Range(0, int.MaxValue);
			_runtimeConfig.BotDifficultyLevel = 1;
			_runtimeConfig.MapId = config.Map;
			_runtimeConfig.Map = op.WaitForCompletion().Settings;
			_runtimeConfig.PlayersLimit = config.PlayersLimit;
			_runtimeConfig.GameMode = config.GameMode;
			_runtimeConfig.GameEndTarget = config.GameEndTarget;
		}

		/// <inheritdoc cref="EnterRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public EnterRoomParams GetEnterRoomParams(IGameDataProvider dataProvider, MapConfig config, string roomName = null)
		{
			var preloadIds = new List<int>();
			
			foreach (var (key, value) in dataProvider.EquipmentDataProvider.EquippedItems)
			{
				var equipmentDataInfo = dataProvider.EquipmentDataProvider.GetEquipmentDataInfo(value);
				preloadIds.Add((int) equipmentDataInfo.GameId);
			}

			preloadIds.Add((int) dataProvider.PlayerDataProvider.CurrentSkin.Value);
			
			var roomParams = new EnterRoomParams
			{
				RoomName = roomName,
				PlayerProperties = new Hashtable {{"PreloadIds", preloadIds.ToArray()}},
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					BroadcastPropsChangeToAll = true,
					CleanupCacheOnLeave = true,
					CustomRoomProperties = GetCreationRoomProperties(config),
					CustomRoomPropertiesForLobby = GetRoomPropertiesToExposeInLobby(),
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = false,
					DeleteNullProperties = true,
					EmptyRoomTtl = 0,
					IsOpen = true,
					IsVisible = string.IsNullOrEmpty(roomName),
					MaxPlayers = (byte)config.PlayersLimit,
					PlayerTtl = _serverSettings.PlayerTtlInSeconds * 1000,

				}
			};
			
			return roomParams;
		}

		/// <inheritdoc cref="OpJoinRandomRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public OpJoinRandomRoomParams GetJoinRandomRoomParams(MapConfig config)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetMatchMakingRoomProperties(config),
				ExpectedMaxPlayers = (byte) config.PlayersLimit,
				ExpectedUsers = null,
				MatchingType = MatchmakingMode.FillRoom,
				SqlLobbyFilter = "",
				TypedLobby = TypedLobby.Default
			};
		}

		/// <inheritdoc cref="QuantumRunner.StartParameters"/>
		/// <remarks>
		/// Default values to start the Quantum simulation based on the current selected adventure
		/// </remarks>
		public QuantumRunner.StartParameters GetDefaultStartParameters(MapConfig config)
		{
			var gameMode = config.PlayersLimit == 1 ? DeterministicGameMode.Local : DeterministicGameMode.Multiplayer;
			
			return new QuantumRunner.StartParameters
			{
				RuntimeConfig = _runtimeConfig,
				DeterministicConfig = _deterministicConfigAsset.Config,
				ReplayProvider  = null,
				GameMode = IsOfflineMode ? DeterministicGameMode.Local : gameMode,
				InitialFrame = 0,
				RunnerId = "DEFAULT",
				QuitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.LeaveRoom,
				LocalPlayerCount = 1,
				RecordingFlags = RecordingFlags.All,
				ResourceManagerOverride = null,
				InstantReplayConfig = InstantReplaySettings.Default,
				HeapExtraCount = 0,
				PlayerCount = config.PlayersLimit
			};
		}

		private Hashtable GetCreationRoomProperties(MapConfig config)
		{
			var properties = GetMatchMakingRoomProperties(config);
			
			properties.Add(_roomPropertyKeyStartTime, DateTime.UtcNow.Ticks);

			return properties;
		}
		
		private Hashtable GetMatchMakingRoomProperties(MapConfig config)
		{
			var properties = new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{ _roomPropertyKeyGitCommit, VersionUtils.Commit },
				
				// Set the game map Id for the same matchmaking
				{ _roomPropertyKeyMap, config.Id },
				
				// Set if only dev mode players match together
				{ _roomPropertyKeyDevMode, IsDevMode },
			};
			
			return properties;
		}

		/// <summary>
		/// These are the room prop keys that will be publicly accessible to other clients for
		/// matchmaking.
		/// </summary>
		private string[] GetRoomPropertiesToExposeInLobby()
		{
			return new []
			{
				_roomPropertyKeyGitCommit,
				_roomPropertyKeyMap,
				_roomPropertyKeyDevMode
			};
		}
	}
}