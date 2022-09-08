using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This utility class provides functionality for getting data used in photon network operations
	/// </summary>
	public static class NetworkUtils
	{
		public const char ROOM_SEPARATOR = '#';

		/// <summary>
		/// Returns a room parameters used for creation of custom and matchmaking rooms
		/// </summary>
		public static EnterRoomParams GetRoomCreateParams(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, MapGridConfigs gridConfigs, List<string> mutators,
		                                                  string roomName, bool isRankedMatch, bool gameHasBots)
		{
			var isRandomMatchmaking = string.IsNullOrWhiteSpace(roomName);

			var roomNameFinal = isRandomMatchmaking ? null : roomName;
			var emptyTtl = 0;
			var maxPlayers = GetMaxPlayers(gameModeConfig, mapConfig);
			
			if (FeatureFlags.COMMIT_VERSION_LOCK && !isRandomMatchmaking)
			{
				roomNameFinal += ROOM_SEPARATOR + VersionUtils.Commit;
			}

			if (!isRandomMatchmaking)
			{
				emptyTtl = roomNameFinal.Contains(GameConstants.Network.ROOM_NAME_PLAYTEST)
					           ? GameConstants.Network.EMPTY_ROOM_PLAYTEST_TTL_MS
					           : GameConstants.Network.EMPTY_ROOM_TTL_MS;
			}
			else
			{
				emptyTtl = GameConstants.Network.EMPTY_ROOM_TTL_MS;
			}

			var roomParams = new EnterRoomParams
			{
				RoomName = roomNameFinal,
				PlayerProperties = null,
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					BroadcastPropsChangeToAll = true,
					CleanupCacheOnLeave = true,
					CustomRoomProperties = GetCreateRoomProperties(gameModeConfig, mapConfig, gridConfigs, mutators, isRankedMatch, gameHasBots),
					CustomRoomPropertiesForLobby = GetCreateRoomPropertiesForLobby(),
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = false,
					DeleteNullProperties = true,
					EmptyRoomTtl = emptyTtl,
					IsOpen = true,
					IsVisible = isRandomMatchmaking,
					MaxPlayers = isRandomMatchmaking
						             ? (byte) maxPlayers
						             : (byte) (maxPlayers + GameConstants.Data.MATCH_SPECTATOR_SPOTS),
					PlayerTtl = GameConstants.Network.DEFAULT_PLAYER_TTL_MS
				}
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
				roomNameFinal += ROOM_SEPARATOR + VersionUtils.Commit;
			}

			return new EnterRoomParams
			{
				RoomName = roomNameFinal,
				PlayerProperties = null,
				ExpectedUsers = null,	
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					PlayerTtl = GameConstants.Network.DEFAULT_PLAYER_TTL_MS,
					EmptyRoomTtl = GameConstants.Network.EMPTY_ROOM_TTL_MS
				}
			};
		}

		/// <summary>
		/// Returns random room entry parameters used for matchmaking room joining
		/// </summary>
		public static OpJoinRandomRoomParams GetJoinRandomRoomParams(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, bool isRankedMatch)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetJoinRoomProperties(gameModeConfig, mapConfig, mutators, isRankedMatch),
				ExpectedMaxPlayers = (byte) GetMaxPlayers(gameModeConfig, mapConfig),
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
			var gameModeConfig = services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());
			var compatibleMaps = new List<QuantumMapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex =
				Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.Balance.MAP_ROTATION_TIME_MINUTES);

			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				var mapConfig = services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) mapId);
				if (!mapConfig.IsTestMap)
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
				GameConstants.Network.ROOM_PROPS_RANKED_MATCH,
				GameConstants.Network.ROOM_PROPS_GAME_MODE,
				GameConstants.Network.ROOM_PROPS_MUTATORS
			};
		}
		
		private static Hashtable GetCreateRoomProperties(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, MapGridConfigs gridConfigs, List<string> mutators, bool isRankedMatch, bool gameHasBots)
		{
			var properties = GetJoinRoomProperties(gameModeConfig, mapConfig, mutators, isRankedMatch);

			properties.Add(GameConstants.Network.ROOM_PROPS_START_TIME, DateTime.UtcNow.Ticks);
			
			properties.Add(GameConstants.Network.ROOM_PROPS_BOTS, gameHasBots);

			if (gameModeConfig.SpawnPattern)
			{
				properties.Add(GameConstants.Network.ROOM_PROPS_DROP_PATTERN, CalculateDropPattern(gridConfigs));
			}

			return properties;
		}

		private static Hashtable GetJoinRoomProperties(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig,
		                                               List<string> mutators, bool isRankedMatch)
		{
			return new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{GameConstants.Network.ROOM_PROPS_COMMIT, VersionUtils.Commit},

				// Set the game map Id for the same matchmaking
				{GameConstants.Network.ROOM_PROPS_MAP, mapConfig.Map},
				
				// For matchmaking, rooms are segregated by casual/ranked.
				{GameConstants.Network.ROOM_PROPS_RANKED_MATCH, isRankedMatch},

				// For matchmaking, rooms are segregated by casual/ranked.
				{GameConstants.Network.ROOM_PROPS_GAME_MODE, gameModeConfig.Id},
				
				// A list of mutators used in this room
				{GameConstants.Network.ROOM_PROPS_MUTATORS, string.Join(",", mutators)}
			};
		}

		private static bool[][] CalculateDropPattern(MapGridConfigs gridConfigs)
		{
			var size = gridConfigs.GetSize();
			var dropPattern = new bool[size.x][];

			for (int i = 0; i < size.y; i++)
			{
				dropPattern[i] = new bool[size.y];
			}

			int x = size.x - 1;
			int y = size.y - 1;

			// Starting square
			dropPattern[x][y] = true;
			dropPattern[x - 1][y] = true;
			dropPattern[x][y - 1] = true;

			// Path
			while (x > 0 || y > 0)
			{
				if (x == 0)
				{
					y--;
				}
				else if (y == 0)
				{
					x--;
				}
				else
				{
					if (Random.Range(0, 2) == 0)
					{
						y--;
					}
					else
					{
						x--;
					}
				}

				dropPattern[x][y] = true;

				// Expand path in N, W, and NW directions
				if (y > 0)
				{
					dropPattern[x][y - 1] = true;
				}

				if (x > 0)
				{
					dropPattern[x - 1][y] = true;
				}

				if (x > 0 && y > 0)
				{
					dropPattern[x - 1][y - 1] = true;
				}

				// Expand path if we're at an edge
				if (x == 0)
				{
					dropPattern[x + 1][y] = true;
				}

				if (x == size.x - 1)
				{
					dropPattern[x - 1][y] = true;
				}

				if (y == 0)
				{
					dropPattern[x][y + 1] = true;
				}

				if (y == size.y - 1)
				{
					dropPattern[x][y - 1] = true;
				}
			}

			// Flip vertically
			if (Random.Range(0, 2) == 0)
			{
				for (int ix = 0; ix < dropPattern.Length; ix++)
				{
					for (int iy = 0; iy < dropPattern[ix].Length / 2; iy++)
					{
						(dropPattern[ix][iy], dropPattern[ix][dropPattern[ix].Length - iy - 1]) =
							(dropPattern[ix][dropPattern[ix].Length - iy - 1], dropPattern[ix][iy]);
					}
				}
			}


			return dropPattern;
		}

		/// <summary>
		/// Calculates the maximum number of players based on game mode and map.
		/// </summary>
		public static int GetMaxPlayers(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig)
		{
			return Math.Min((int) gameModeConfig.MaxPlayers, mapConfig.MaxPlayers);
		}
	}
}
