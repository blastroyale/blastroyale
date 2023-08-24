using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Server.SDK.Modules;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Environment = FirstLight.Game.Services.Environment;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This utility class provides functionality for getting data used in photon network operations
	/// </summary>
	public static class NetworkUtils
	{
		public static string RoomCommitLockData => GameConstants.Network.ROOM_META_SEPARATOR + VersionUtils.Commit;


		public static byte GetMaxPlayers(MatchRoomSetup setup, bool spectators = true)
		{
			var maxPlayers =  Math.Min((int) setup.GameMode().MaxPlayers, setup.Map().MaxPlayers);
			if (spectators)
			{
				maxPlayers += GetMaxSpectators(setup);
			}
			// Quantum development servers only allow 20 players :(
			if (MainInstaller.Resolve<IGameServices>().GameBackendService.IsDev())
			{
				maxPlayers = Math.Min(20, maxPlayers);
			}
			return (byte)maxPlayers;
		}

		public static byte GetMaxSpectators(MatchRoomSetup setup)
		{
			if (setup.JoinType != JoinType.Custom )
			{
				return 0;
			}
			
			if (MainInstaller.Resolve<IGameServices>().GameBackendService.IsDev())
			{
				// Limit spectators in development environment, so we have slots to real players
				return 5;
			}
			
			return GameConstants.Data.MATCH_SPECTATOR_SPOTS;

		}
		
		/// <summary>
		/// Returns a room parameters used for creation of custom and matchmaking rooms
		/// </summary>
		public static EnterRoomParams GetRoomCreateParams(MatchRoomSetup setup, Vector3 dropzonePosRot, string[] expectedPlayers = null)
		{
			if (FeatureFlags.FORCE_RANKED)
			{
				setup.MatchType = MatchType.Ranked;
			}

			var isRandomMatchmaking = setup.MatchType != MatchType.Custom;
			var isTutorialMode = setup.GameModeId == GameConstants.Tutorial.FIRST_TUTORIAL_GAME_MODE_ID ||
				setup.GameModeId == GameConstants.Tutorial.SECOND_BOT_MODE_ID;
			var roomNameFinal = setup.RoomIdentifier;
			
			// In offline games we need to create the room with the correct TTL as we cannot update TTL
			// mid games. If we don't we won't be able to reconnect to the room unless we use a frame snapshot which is tricky.
			var emptyTtl = isTutorialMode ? GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS : 0;

			if (FeatureFlags.COMMIT_VERSION_LOCK && !isRandomMatchmaking)
			{
				roomNameFinal += RoomCommitLockData;
			}

			var roomParams = new EnterRoomParams
			{
				RoomName = roomNameFinal,
				PlayerProperties = null,
				
				// Expected users commented out. This is used for squads specifically when 
				// joining only a single squad to the game.
				// This makes the game auto-start when all players join the game not giving
				// time to select the drop zone. This is because our matchmaking is the same as our
				// lobby which is also the same as our drop zone selector. :L
				
				// ExpectedUsers = expectedPlayers,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					BroadcastPropsChangeToAll = true,
					CleanupCacheOnLeave = true,
					CustomRoomProperties = GetCreateRoomProperties(setup, dropzonePosRot),
					CustomRoomPropertiesForLobby = GetCreateRoomPropertiesForLobby(),
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = setup.GameMode().ShouldUsePlayfabMatchmaking(),
					DeleteNullProperties = true,
					EmptyRoomTtl = emptyTtl,
					IsOpen = true,
					IsVisible = isRandomMatchmaking && !isTutorialMode,
					MaxPlayers =  GetMaxPlayers(setup),
					PlayerTtl = GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS
				},
			};

			return roomParams;
		}

		/// <summary>
		/// Returns room entry parameters used solely for entering rooms directly (custom games)
		/// </summary>
		public static EnterRoomParams GetRoomEnterParams(string roomName)
		{
			var roomNameFinal = roomName;

			if (FeatureFlags.COMMIT_VERSION_LOCK)
			{
				roomNameFinal += RoomCommitLockData;
			}

			Log.Warn("Enter Custom Games");
			return new EnterRoomParams
			{
				RoomName = roomNameFinal,
				PlayerProperties = null,
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					PlayerTtl = GameConstants.Network.PLAYER_GAME_TTL_MS,
					EmptyRoomTtl = 0
				}
			};
		}

		/// <summary>
		/// Returns random room entry parameters used for matchmaking room joining
		/// </summary>
		public static OpJoinRandomRoomParams GetJoinRandomRoomParams(MatchRoomSetup setup)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetJoinRoomProperties(setup),
				ExpectedMaxPlayers = GetMaxPlayers(setup),
				ExpectedUsers = null,
				MatchingType = MatchmakingMode.FillRoom,
				SqlLobbyFilter = "",
				TypedLobby = TypedLobby.Default,
			};
		}

		/// <summary>
		/// Returns the current map in rotation, used for creating rooms with maps in rotation
		/// </summary>
		public static QuantumMapConfig GetRotationMapConfig(string gameModeId, IGameServices services)
		{
			var gameModeConfig = services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);
			var compatibleMaps = new List<QuantumMapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex =
				Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.Balance.MAP_ROTATION_TIME_MINUTES);

			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				var mapConfig = services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) mapId);
				if (!mapConfig.IsTestMap && !mapConfig.IsCustomOnly)
				{
					compatibleMaps.Add(mapConfig);
				}
			}

			if (timeSegmentIndex >= compatibleMaps.Count)
			{
				timeSegmentIndex %= compatibleMaps.Count;
			}

			return compatibleMaps[timeSegmentIndex];
		}

		private static string[] GetCreateRoomPropertiesForLobby()
		{
			return new[]
			{
				GameConstants.Network.ROOM_PROPS_COMMIT,
				GameConstants.Network.ROOM_PROPS_MAP,
				GameConstants.Network.ROOM_PROPS_MATCH_TYPE,
				GameConstants.Network.ROOM_PROPS_GAME_MODE,
				GameConstants.Network.ROOM_PROPS_MUTATORS
			};
		}
		
		private static Hashtable GetCreateRoomProperties(MatchRoomSetup setup, Vector3 dropzonePosRot)
		{
			var properties = GetJoinRoomProperties(setup);

			// TODO: Setting the time here prevents from 2 players joining the same room if they click concurrently
			// this is because their initial room properties will not match
			properties.Add(GameConstants.Network.ROOM_PROPS_CREATION_TICKS, DateTime.UtcNow.Ticks);
			properties.Add(GameConstants.Network.ROOM_PROPS_BOTS, setup.GameMode().AllowBots);
			properties.Add(GameConstants.Network.ROOM_PROPS_SETUP, ModelSerializer.Serialize(setup).Value);

			// TODO - RENAME "SpawnPattern"
			if (setup.GameMode().SpawnPattern)
			{
				properties.Add(GameConstants.Network.DROP_ZONE_POS_ROT, dropzonePosRot);
			}

			return properties;
		}

		private static Hashtable GetJoinRoomProperties(MatchRoomSetup setup)
		{
			// !!!NOTE!!!
			// If you add anything here you must also add the key in GetCreateRoomPropertiesForLobby!

			return new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{GameConstants.Network.ROOM_PROPS_COMMIT, VersionUtils.Commit},

				// Set the game map Id for the same matchmaking
				{GameConstants.Network.ROOM_PROPS_MAP, setup.Map().Map},

				// For matchmaking, rooms are segregated by casual/ranked.
				{GameConstants.Network.ROOM_PROPS_MATCH_TYPE, setup.MatchType.ToString()},

				// For matchmaking, rooms are segregated by casual/ranked.
				{GameConstants.Network.ROOM_PROPS_GAME_MODE, setup.GameMode().Id},

				// A list of mutators used in this room
				{GameConstants.Network.ROOM_PROPS_MUTATORS, string.Join(",", setup.Mutators)}
			};
		}
		

		/// <summary>
		/// Requests to check if the device is online
		/// </summary>
		public static bool IsOnline()
		{
			return Application.internetReachability != NetworkReachability.NotReachable;
		}

		/// <summary>
		/// Requests to check if the device is offline
		/// </summary>
		public static bool IsOffline()
		{
			return Application.internetReachability == NetworkReachability.NotReachable;
		}

		/// <summary>
		/// Requests to check if the device is connected to internet, and Photon is connected
		/// </summary>
		public static bool IsOnlineAndConnected()
		{
			return IsOnline() && MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient.IsConnected;
		}

		/// <summary>
		/// Requests to check if the device is disconnted from internet, or Photon is disconnected
		/// </summary>
		public static bool IsOfflineOrDisconnected()
		{
			return IsOffline() || !MainInstaller.Resolve<IGameServices>().NetworkService.QuantumClient.IsConnected;
		}

		/// <summary>
		/// Checks to see if a network action triggered by player input can be sent.
		/// Sends a NetworkActionWhileDisconnectedMessage if not.
		/// </summary>
		public static bool CheckAttemptNetworkAction()
		{
			if (IsOfflineOrDisconnected())
			{
				MainInstaller.Resolve<IGameServices>().MessageBrokerService.Publish(new NetworkActionWhileDisconnectedMessage());
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns a random dropzone vector to be added to room creation params
		/// </summary>
		public static Vector3 GetRandomDropzonePosRot()
		{
			var radiusPosPercent = GameConstants.Balance.MAP_DROPZONE_POS_RADIUS_PERCENT;
			return new Vector3(Random.Range(-radiusPosPercent, radiusPosPercent),
				Random.Range(-radiusPosPercent, radiusPosPercent), Random.Range(0, 360));
		}


		public static float GetMatchmakingTime(MatchType type, QuantumGameModeConfig gameModeConfig, QuantumGameConfig quantumGameConfig)
		{
			if (type == MatchType.Ranked)
			{
				return quantumGameConfig.RankedMatchmakingTime.AsFloat;
			}

			return gameModeConfig.ShouldUsePlayfabMatchmaking()
				? quantumGameConfig.PlayfabMatchmakingTime.AsFloat
				: quantumGameConfig.CasualMatchmakingTime.AsFloat;
		}
	}
}