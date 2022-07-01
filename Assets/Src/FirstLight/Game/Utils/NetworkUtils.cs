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
		public static EnterRoomParams GetRoomCreateParams(QuantumMapConfig mapConfig, MapGridConfigs gridConfigs,
		                                                  string roomName)
		{
			var isRandomMatchmaking = string.IsNullOrWhiteSpace(roomName);
			
			var roomParams = new EnterRoomParams
			{
				RoomName = isRandomMatchmaking ? null : roomName,// + ROOM_SEPARATOR + VersionUtils.Commit,
				PlayerProperties = null,
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = new RoomOptions
				{
					BroadcastPropsChangeToAll = true,
					CleanupCacheOnLeave = true,
					CustomRoomProperties = GetCreateRoomProperties(mapConfig, gridConfigs),
					CustomRoomPropertiesForLobby = new[]
					{
						GameConstants.Network.ROOM_PROPS_COMMIT,
						GameConstants.Network.ROOM_PROPS_MAP
					},
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = false,
					DeleteNullProperties = true,
					EmptyRoomTtl = GameConstants.Network.EMPTY_ROOM_TTL_MS,
					IsOpen = true,
					IsVisible = isRandomMatchmaking,
					MaxPlayers = (byte) mapConfig.PlayersLimit,
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
			return new EnterRoomParams
			{
				RoomName = roomName ,//+ ROOM_SEPARATOR + VersionUtils.Commit,
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
		public static OpJoinRandomRoomParams GetJoinRandomRoomParams(QuantumMapConfig mapConfig)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetJoinRoomProperties(mapConfig),
				ExpectedMaxPlayers = (byte) mapConfig.PlayersLimit,
				ExpectedUsers = null,
				MatchingType = MatchmakingMode.FillRoom,
				SqlLobbyFilter = "",
				TypedLobby = TypedLobby.Default,
			};
		}

		/// <summary>
		/// Returns the current map in rotation, used for creating rooms with maps in rotation
		/// </summary>
		public static QuantumMapConfig GetRotationMapConfig(GameMode gameMode, IGameServices services)
		{
			var configs = services.ConfigsProvider.GetConfigsDictionary<QuantumMapConfig>();
			var compatibleMaps = new List<QuantumMapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex =
				Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.Balance.MAP_ROTATION_TIME_MINUTES);

			foreach (var config in configs)
			{
				if (config.Value.GameMode == gameMode && !config.Value.IsTestMap)
				{
					compatibleMaps.Add(config.Value);
				}
			}

			if (timeSegmentIndex >= compatibleMaps.Count)
			{
				timeSegmentIndex %= compatibleMaps.Count;
			}

			return compatibleMaps[timeSegmentIndex];
		}

		private static Hashtable GetCreateRoomProperties(QuantumMapConfig mapConfig, MapGridConfigs gridConfigs)
		{
			var properties = GetJoinRoomProperties(mapConfig);

			if (mapConfig.GameMode == GameMode.BattleRoyale && !mapConfig.IsTestMap)
			{
				properties.Add(GameConstants.Network.ROOM_PROPS_DROP_PATTERN, CalculateDropPattern(gridConfigs));
			}

			return properties;
		}

		private static Hashtable GetJoinRoomProperties(QuantumMapConfig mapConfig)
		{
			return new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{GameConstants.Network.ROOM_PROPS_COMMIT, VersionUtils.Commit},

				// Set the game map Id for the same matchmaking
				{GameConstants.Network.ROOM_PROPS_MAP, mapConfig.Id},

				// Future proofing, good to know when a room was created
				{GameConstants.Network.ROOM_PROPS_START_TIME, DateTime.UtcNow.Ticks}
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
	}
}
