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
			_runtimeConfig.MapId = config.Id;
			_runtimeConfig.Map = op.WaitForCompletion().Settings;
			_runtimeConfig.PlayersLimit = config.PlayersLimit;
			_runtimeConfig.GameMode = config.GameMode;
			_runtimeConfig.GameEndTarget = config.GameEndTarget;
			_runtimeConfig.IsTestMap = config.IsTestMap;
		}

		/// <inheritdoc cref="EnterRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public EnterRoomParams GetEnterRoomParams(MapConfig mapConfig, string roomName)
		{
			var roomParams = new EnterRoomParams
			{
				RoomName = roomName,
				PlayerProperties = null,
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					BroadcastPropsChangeToAll = true,
					CleanupCacheOnLeave = true,
					CustomRoomProperties = GetCreationRoomProperties(mapConfig),
					CustomRoomPropertiesForLobby = GetRoomPropertiesToExposeInLobby(),
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = false,
					DeleteNullProperties = true,
					EmptyRoomTtl = 0,
					IsOpen = true,
					IsVisible = string.IsNullOrEmpty(roomName),
					MaxPlayers = (byte)mapConfig.PlayersLimit,
					PlayerTtl = _serverSettings.PlayerTtlInSeconds * 1000
				}
			};
			
			return roomParams;
		}

		/// <inheritdoc cref="OpJoinRandomRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public OpJoinRandomRoomParams GetJoinRandomRoomParams(MapConfig mapConfig)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetMatchMakingRoomProperties(mapConfig),
				ExpectedMaxPlayers = (byte) mapConfig.PlayersLimit,
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
		public QuantumRunner.StartParameters GetDefaultStartParameters(int playerLimit)
		{
			var gameMode = playerLimit == 1 ? DeterministicGameMode.Local : DeterministicGameMode.Multiplayer;
			
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
				PlayerCount =playerLimit
			};
		}

		private Hashtable GetCreationRoomProperties(MapConfig mapConfig)
		{
			var properties = GetMatchMakingRoomProperties(mapConfig);
			
			properties.Add(GameConstants.ROOM_PROPS_START_TIME, DateTime.UtcNow.Ticks);

			return properties;
		}
		
		private Hashtable GetMatchMakingRoomProperties(MapConfig mapConfig)
		{
			var properties = new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{ GameConstants.ROOM_PROPS_COMMIT, VersionUtils.Commit },
				
				// Set the game map Id for the same matchmaking
				{ GameConstants.ROOM_PROPS_MAP, mapConfig.Id },
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
				GameConstants.ROOM_PROPS_COMMIT,
				GameConstants.ROOM_PROPS_MAP
			};
		}
	}
}