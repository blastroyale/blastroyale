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
		private const string _roomPropertyKeyAdventure = "a";
		
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
		public void SetRuntimeConfig(AdventureInfo info)
		{
			var op = Addressables.LoadAssetAsync<MapAsset>($"Maps/{info.Config.Map.ToString()}.asset");
			
			_runtimeConfig.Seed = Random.Range(0, int.MaxValue);
			_runtimeConfig.BotDifficultyLevel = info.Config.EnemiesDifficulty;
			_runtimeConfig.MapId = info.Config.Map;
			_runtimeConfig.Map = op.WaitForCompletion().Settings;
			_runtimeConfig.TotalFightersLimit = info.Config.TotalFightersLimit;
			_runtimeConfig.GameMode = GameMode.Deathmatch;
			_runtimeConfig.GameEndTarget = info.Config.DeathmatchKillCount;
		}

		/// <inheritdoc cref="EnterRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public EnterRoomParams GetDefaultEnterRoomParams(AdventureInfo info)
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
					CustomRoomProperties = GetCreationRoomProperties(info),
					CustomRoomPropertiesForLobby = GetRoomPropertiesToExposeInLobby(),
					DeleteNullProperties = true,
					EmptyRoomTtl = 0,
					IsOpen = true,
					IsVisible = true,
					MaxPlayers = (byte) info.Config.PlayersLimit,
					PlayerTtl = _serverSettings.PlayerTtlInSeconds * 1000
				}
			};

#if !RELEASE_BUILD
			if (SROptions.Current.IsPrivateRoomSet)
			{
				roomParams.RoomName = SROptions.Current.PrivateRoomName;
				roomParams.RoomOptions.IsVisible = false;
			}
#endif
			
			return roomParams;
		}

		/// <inheritdoc cref="OpJoinRandomRoomParams"/>
		/// <remarks>
		/// Default values that can be used or adapted to the custom situation
		/// </remarks>
		public OpJoinRandomRoomParams GetDefaultJoinRoomParams(AdventureInfo info)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetMatchMakingRoomProperties(info),
				ExpectedMaxPlayers = (byte) info.Config.PlayersLimit,
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
		public QuantumRunner.StartParameters GetDefaultStartParameters(AdventureInfo info)
		{
			var gameMode = info.Config.PlayersLimit == 1 ? DeterministicGameMode.Local : DeterministicGameMode.Multiplayer;
			
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
				PlayerCount = info.Config.PlayersLimit
			};
		}

		private Hashtable GetCreationRoomProperties(AdventureInfo info)
		{
			var properties = GetMatchMakingRoomProperties(info);
			
			properties.Add(RoomPropertyKeyStartTime, DateTime.UtcNow.Ticks);

			return properties;
		}
		
		private Hashtable GetMatchMakingRoomProperties(AdventureInfo info)
		{
			return new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{ _roomPropertyKeyGitCommit, VersionUtils.Commit },
				
				// We send the map GameId as an int.
				{ _roomPropertyKeyMap, info.Config.Map.ToString("D") },
				
				// We send the game adventure Id
				{ _roomPropertyKeyAdventure, info.AdventureData.Id },
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
				_roomPropertyKeyMap,
				_roomPropertyKeyAdventure
			};
		}
	}
}