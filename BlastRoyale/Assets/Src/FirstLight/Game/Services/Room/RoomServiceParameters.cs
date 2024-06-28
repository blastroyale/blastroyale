using FirstLight.FLogger;
using FirstLight.Game.Utils;
using Photon.Realtime;
using Quantum;

namespace FirstLight.Game.Services.RoomService
{
	public class RoomServiceParameters
	{
		public static string RoomCommitLockData => GameConstants.Network.ROOM_META_SEPARATOR + VersionUtils.Commit;
		private static string[] _expectedCustomRoomProperties = new RoomProperties().GetExposedPropertiesIds();

		private RoomService _service;

		public RoomServiceParameters(RoomService service)
		{
			_service = service;
		}

		/// <summary>
		/// Returns random room entry parameters used for matchmaking room joining
		/// </summary>
		public OpJoinRandomRoomParams GetJoinRandomRoomParams(MatchRoomSetup setup)
		{
			return new OpJoinRandomRoomParams
			{
				ExpectedCustomRoomProperties = GetJoinRoomProperties(setup).ToHashTable(),
				ExpectedMaxPlayers = _service.GetMaxPlayers(setup),
				ExpectedUsers = null,
				MatchingType = MatchmakingMode.FillRoom,
				SqlLobbyFilter = "",
				TypedLobby = TypedLobby.Default,
			};
		}

		private RoomProperties GetCreateRoomProperties(MatchRoomSetup setup)
		{
			var properties = GetJoinRoomProperties(setup);
			// TODO: Setting the time here prevents from 2 players joining the same room if they click concurrently
			// If you can select the spawn point give players time to do it
			FLog.Info("Loading starts at " + properties.LoadingStartServerTime.Value);
			FLog.Info("Now " + _service._networkService.ServerTimeInMilliseconds);
			FLog.Info("Diff " + (properties.LoadingStartServerTime.Value - _service._networkService.ServerTimeInMilliseconds));

			return properties;
		}

		private RoomProperties GetJoinRoomProperties(MatchRoomSetup setup)
		{
			// !!!NOTE!!!
			// If you add anything here you must also add the key in GetCreateRoomPropertiesForLobby!
			return new RoomProperties
			{
				Commit = {Value = VersionUtils.Commit},
				GameStarted = {Value = false},
				SimulationMatchConfig = {Value = setup.SimulationConfig}
			};
		}

		/// <summary>
		/// Returns a room parameters used for creation of custom and matchmaking rooms
		/// </summary>
		public EnterRoomParams GetRoomCreateParams(MatchRoomSetup setup, bool offline = false)
		{
			var roomNameFinal = string.IsNullOrEmpty(setup.RoomIdentifier) ? null : setup.RoomIdentifier;
			// In offline games we need to create the room with the correct TTL as we cannot update TTL
			// mid games. If we don't we won't be able to reconnect to the room unless we use a frame snapshot which is tricky.
			var emptyTtl = offline ? GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS : 0;
			if (RemoteConfigs.Instance.EnableCommitVersionLock)
			{
				roomNameFinal += RoomCommitLockData;
			}

			var createProperties = GetCreateRoomProperties(setup);
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
					CustomRoomProperties = createProperties.ToHashTable(),
					CustomRoomPropertiesForLobby = _expectedCustomRoomProperties,
					Plugins = null,
					SuppressRoomEvents = false,
					SuppressPlayerInfo = false,
					PublishUserId = setup.SimulationConfig.MatchType == MatchType.Matchmaking,
					DeleteNullProperties = true,
					EmptyRoomTtl = emptyTtl,
					IsOpen = true,
					IsVisible = setup.SimulationConfig.MatchType == MatchType.Custom,
					MaxPlayers = _service.GetMaxPlayers(setup),
					PlayerTtl = GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS
				},
			};

			return roomParams;
		}

		/// <summary>
		/// Returns room entry parameters used solely for entering rooms directly (custom games)
		/// </summary>
		public EnterRoomParams GetRoomEnterParams(string roomName)
		{
			var roomNameFinal = roomName;

			if (RemoteConfigs.Instance.EnableCommitVersionLock)
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
	}
}