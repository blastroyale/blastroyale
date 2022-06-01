using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using Photon.Realtime;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// This utility class provides functionality for getting data used in photon network operations
	/// </summary>
	public static class NetworkUtils
	{
		/// <summary>
		/// Returns a room parameters used for creation of custom and matchmaking rooms
		/// </summary>
		public static EnterRoomParams GetRoomCreateParams(MapConfig mapConfig, string roomName, int playerTtl)
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
					CustomRoomProperties = GetCustomRoomProperties(mapConfig),
					CustomRoomPropertiesForLobby = new []
					{
						GameConstants.Network.ROOM_PROPS_COMMIT,
						GameConstants.Network.ROOM_PROPS_MAP
					},
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = false,
					DeleteNullProperties = true,
					EmptyRoomTtl = 0,
					IsOpen = true,
					IsVisible = string.IsNullOrEmpty(roomName),
					MaxPlayers = (byte)mapConfig.PlayersLimit,
					PlayerTtl = playerTtl
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
				RoomName = roomName,
				PlayerProperties = null,
				ExpectedUsers = null,
				Lobby = TypedLobby.Default,
				RoomOptions = null
			};
		}
		
		/// <summary>
		/// Returns random room entry parameters used for matchmaking room joining
		/// </summary>
		public static OpJoinRandomRoomParams GetJoinRandomRoomParams(MapConfig mapConfig)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetCustomRoomProperties(mapConfig),
				ExpectedMaxPlayers = (byte) mapConfig.PlayersLimit,
				ExpectedUsers = null,
				MatchingType = MatchmakingMode.FillRoom,
				SqlLobbyFilter = "",
				TypedLobby = TypedLobby.Default
			};
		}

		/// <summary>
		/// Returns the current map in rotation, used for creating rooms with maps in rotation
		/// </summary>
		public static MapConfig GetRotationMapConfig(GameMode gameMode, IGameServices services)
		{
			var configs = services.ConfigsProvider.GetConfigsDictionary<MapConfig>();
			var compatibleMaps = new List<MapConfig>();
			var span = DateTime.UtcNow - DateTime.UtcNow.Date;
			var timeSegmentIndex = Mathf.RoundToInt((float) span.TotalMinutes / GameConstants.Balance.MAP_ROTATION_TIME_MINUTES);

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
		
		private static Hashtable GetCustomRoomProperties(MapConfig mapConfig)
		{
			var properties = new Hashtable
			{
				// The commit should guarantee the same Quantum build version + App version etc.
				{ GameConstants.Network.ROOM_PROPS_COMMIT, VersionUtils.Commit },
				
				// Set the game map Id for the same matchmaking
				{ GameConstants.Network.ROOM_PROPS_MAP, mapConfig.Id },
				
				// Future proofing, good to know when a room was created
				{GameConstants.Network.ROOM_PROPS_START_TIME, DateTime.UtcNow.Ticks}
			};
			
			return properties;
		}
	}
}
