using System;
using ExitGames.Client.Photon;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
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
		public const string RoomPropertyKeyStartTime = "t";
		
		private const string _roomPropertyKeyGitCommit = "g";
		private const string _roomPropertyKeyMap = "m";
		
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
		public EnterRoomParams GetDefaultEnterRoomParams(MapConfig config)
		{
			var roomParams = new EnterRoomParams
			{
				RoomName = null,
				PlayerProperties = new Hashtable(),
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					BroadcastPropsChangeToAll = true,
					CleanupCacheOnLeave = true,
					CustomRoomProperties = GetCreationRoomProperties(config),
					CustomRoomPropertiesForLobby = GetRoomPropertiesToExposeInLobby(),
					DeleteNullProperties = true,
					EmptyRoomTtl = 0,
					IsOpen = true,
					IsVisible = true,
					MaxPlayers = (byte) config.PlayersLimit,
					PlayerTtl = _serverSettings.PlayerTtlInSeconds * 1000
				}
			};

			if (IsDevMode)
			{
				roomParams.RoomName = "Development";
				roomParams.RoomOptions.IsVisible = false;
			}
			
			return roomParams;
		}

		/// <inheritdoc cref="OpJoinRandomRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public OpJoinRandomRoomParams GetDefaultJoinRoomParams(MapConfig config)
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
				QuitBehaviour = QuantumNetworkCommunicator.QuitBehaviour.Disconnect,
				LocalPlayerCount = 1,
				RecordingFlags = RecordingFlags.All,
				ResourceManagerOverride = null,
				InstantReplayConfig = InstantReplaySettings.Default,
				HeapExtraCount = 0,
				PlayerCount =config.PlayersLimit
			};
		}

		private Hashtable GetCreationRoomProperties(MapConfig config)
		{
			var properties = GetMatchMakingRoomProperties(config);
			
			properties.Add(RoomPropertyKeyStartTime, DateTime.UtcNow.Ticks);

			return properties;
		}
		
		private Hashtable GetMatchMakingRoomProperties(MapConfig config)
		{
			return new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{ _roomPropertyKeyGitCommit, VersionUtils.Commit },
				
				// We send the game map Id
				{ _roomPropertyKeyMap, config.Id },
			};
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
				_roomPropertyKeyMap
			};
		}
	}
}