using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Photon.Realtime;
using Quantum;
using Unity.Services.Authentication;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using UniTaskCompletionSource = Cysharp.Threading.Tasks.UniTaskCompletionSource;

namespace FirstLight.Game.Services.RoomService
{
	public enum PlayerChangeReason
	{
		Join,
		Leave,
	}

	public class PlayerJoinRoomProperties
	{
		public string Team;
		public byte TeamColor;
		public bool Spectator;
	}

	public interface IRoomService
	{
		/// <summary>
		/// Triggered when players join/leave the current room
		/// </summary>
		event Action<Player, PlayerChangeReason> OnPlayersChange;

		/// <summary>
		/// Triggered when room properties change
		/// </summary>
		event Action OnRoomPropertiesChanged;

		/// <summary>
		/// Triggered when the match starts
		/// </summary>
		event Action OnMatchStarted;

		/// <summary>
		/// Do you need docs ?
		/// </summary>
		event Action OnMasterChanged;

		event Action OnJoinedRoom;
		event Action OnLeaveRoom;

		/// <summary>
		/// Yo
		/// </summary>
		event Action OnPlayerPropertiesUpdated;

		/// <summary>
		/// When the local player got kicked
		/// </summary>
		public event Action OnLocalPlayerKicked;

		bool InRoom { get; }
		bool IsJoiningRoom { get; }

		GameRoom CurrentRoom { get; }
		GameRoom LastRoom { get; }
		int LastMatchPlayerAmount { get; }
		public bool IsLocalPlayerSpectator { get; }
		byte GetMaxSpectators(MatchType matchType);

		byte GetMaxPlayers(MatchRoomSetup setup,
						   bool spectators = true);

		QuantumGameModeConfig GetGameModeConfig(string gameModeId);

		/// <summary>
		/// 
		/// Join a specified room by name
		/// </summary>
		/// <param name="roomName">Name of the room to join</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>Note, in order to join a room, the "entry params" that are generated, need to match a created exactly
		/// for the client to be able to enter. If there is even one param mismatching, join operation will fail.
		/// Used to join custom games
		/// </remarks>
		bool JoinRoom(string roomName, PlayerJoinRoomProperties playerProperties = null);

		UniTask JoinRoomAsync(string roomName, PlayerJoinRoomProperties playerProperties = null);

		/// <summary>
		/// Rejoins a room, only if the player is still active in the room. Will never enter a room creating a new player.
		/// The room creation params must match the room params when created.
		/// </summary>
		bool RejoinRoom(string name);

		/// <summary>
		/// Creates room with a specified name
		/// </summary>
		/// <param name="roomName">Name that the created room will have</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>
		///  This used for custom games or forced matches
		/// </remarks>
		bool CreateRoom(MatchRoomSetup setup, bool offlineMode, PlayerJoinRoomProperties playerJoinRoomProperties = null);

		UniTask CreateRoomAsync(MatchRoomSetup setup, PlayerJoinRoomProperties playerJoinRoomProperties = null);

		/// <summary>
		/// Joins a specific room with matching params if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <param name="roomName">Name of the room to join</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>Note, in order to join a room, the "entry params" that are generated, need to match a created room exactly
		/// for the client to be able to enter. If there is even one param mismatching, join operation will fail.
		/// Used for Playfab matchmaking, because with playfab matchmaking we know the name of the room, so a random player will create it and the others will join
		/// </remarks>
		bool JoinOrCreateRoom(MatchRoomSetup setup, PlayerJoinRoomProperties playerProperties = null, string[] expectedPlayers = null);

		/// <summary>
		/// Leaves the current room that local player is in
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool LeaveRoom(bool becomeInactive = false);

		/// <summary>
		/// Kicks another player, only master can call this method
		/// </summary>
		bool KickPlayer(Player playerToKick);
	}

	public class RoomService : IInRoomCallbacks, IMatchmakingCallbacks, IRoomService
	{
		internal readonly IGameNetworkService _networkService;
		private readonly IGameBackendService _backendService;
		internal readonly IConfigsProvider _configsProvider;
		private readonly ICoroutineService _coroutineService;
		internal readonly IGameDataProvider _dataProvider;
		private readonly ILeaderboardService _leaderboardService;
		private readonly HashSet<ClientState> _joiningStates = new (new[] {ClientState.Joining, ClientState.Joined, ClientState.JoinedLobby, ClientState.JoiningLobby});
		public bool IsJoiningRoom => _joiningStates.Contains(_networkService.QuantumClient.State);
		public GameRoom CurrentRoom { get; private set; }
		public GameRoom LastRoom { get; private set; }

		public int LastMatchPlayerAmount { get; private set; }

		private RoomServiceParameters _parameters;
		private RoomServiceCommands _commands;

		public static bool AutoStartWhenLoaded = false;
		public event Action<Player, PlayerChangeReason> OnPlayersChange;
		public event Action OnRoomPropertiesChanged;
		public event Action OnMatchStarted;
		public event Action OnMasterChanged;
		public event Action OnJoinedRoom;
		public event Action OnLeaveRoom;
		public event Action OnPlayerPropertiesUpdated;
		public event Action OnLocalPlayerKicked;

		public bool InRoom => CurrentRoom != null && _networkService.InRoom;

		public bool IsLocalPlayerSpectator
		{
			get
			{
				if (CurrentRoom == null)
				{
					FLog.Error("Trying to get spectator properties when room no longer exists.");
					return false;
				}

				return CurrentRoom?.LocalPlayerProperties?.Spectator?.Value ?? false;
			}
		}

		internal MatchmakingAndRoomConfig Configs => _configsProvider.GetConfig<MatchmakingAndRoomConfig>();

		private UniTaskCompletionSource _roomCreationTcs;
		private UniTaskCompletionSource _roomJoinTcs;

		public RoomService(IGameNetworkService networkService, IGameBackendService backendService, IConfigsProvider configsProvider,
						   ICoroutineService coroutineService, IGameDataProvider dataProvider, ILeaderboardService leaderboardService)
		{
			_networkService = networkService;
			_backendService = backendService;
			_configsProvider = configsProvider;
			_coroutineService = coroutineService;
			_dataProvider = dataProvider;
			_leaderboardService = leaderboardService;
			_parameters = new RoomServiceParameters(this);
			_commands = new RoomServiceCommands(this);
			RegisterListeners();

			// TODO: Optimize to only start this coroutine on game join
			_coroutineService.StartCoroutine(CheckGameStartCoroutine());
		}

		public QuantumGameModeConfig GetGameModeConfig(string gameModeId) => _configsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);
		public QuantumMapConfig GetMapConfig(string mapId) => _configsProvider.GetConfig<QuantumMapConfig>(mapId);

		/// <summary>
		/// Used for joining custom games
		/// </summary>
		/// <param name="roomName"></param>
		/// <returns></returns>
		public bool JoinRoom(string roomName, PlayerJoinRoomProperties playerProperties = null)
		{
			FLog.Info($"JoinRoom: Room:{roomName} InRoom?{InRoom}");

			if (InRoom) return false;

			var enterParams = _parameters.GetRoomEnterParams(roomName);
			_networkService.QuantumRunnerConfigs.IsOfflineMode = false;

			ResetLocalPlayerProperties(playerProperties);
			_networkService.LastUsedSetup.Value = null;

			return _networkService.QuantumClient.OpJoinRoom(enterParams);
		}

		public UniTask JoinRoomAsync(string roomName, PlayerJoinRoomProperties playerProperties = null)
		{
			Assert.IsNull(_roomJoinTcs, "JoinRoomAsync called while another join operation is in progress");

			_roomJoinTcs = new UniTaskCompletionSource();

			JoinRoom(roomName, playerProperties);

			return _roomJoinTcs.Task;
		}

		/// <summary>
		/// This used for custom games or forced matches
		/// </summary>
		public bool CreateRoom(MatchRoomSetup setup, bool offlineMode, PlayerJoinRoomProperties playerJoinRoomProperties)
		{
			if (InRoom) return false;

			FLog.Info($"CreateRoom: {setup}");

			var createParams = _parameters.GetRoomCreateParams(setup, offlineMode);

			_networkService.QuantumRunnerConfigs.IsOfflineMode = offlineMode;

			ResetLocalPlayerProperties(playerJoinRoomProperties);
			_networkService.LastDisconnectLocation = LastDisconnectionLocation.None;
			_networkService.LastUsedSetup.Value = setup;
			return _networkService.QuantumClient.OpCreateRoom(createParams);
		}

		public UniTask CreateRoomAsync(MatchRoomSetup setup, PlayerJoinRoomProperties playerJoinRoomProperties)
		{
			Assert.IsNull(_roomCreationTcs, "CreateRoomAsync called while another create operation is in progress");

			_roomCreationTcs = new UniTaskCompletionSource();

			CreateRoom(setup, false, playerJoinRoomProperties);

			return _roomCreationTcs.Task;
		}

		/// <summary>
		/// Used for Playfab matchmaking, because with playfab matchmaking we know the name of the room, so a random player will create it and the others will join
		/// </summary>
		/// <returns></returns>
		public bool JoinOrCreateRoom(MatchRoomSetup setup, PlayerJoinRoomProperties playerProperties, string[] expectedPlayers = null)
		{
			if (InRoom) return false;

			FLog.Info($"JoinOrCreateRoom: {setup}");

			var createParams = _parameters.GetRoomCreateParams(setup);
			_networkService.QuantumRunnerConfigs.IsOfflineMode = false;

			ResetLocalPlayerProperties(playerProperties);
			_networkService.LastDisconnectLocation = LastDisconnectionLocation.None;
			_networkService.LastUsedSetup.Value = setup;
			return _networkService.QuantumClient.OpJoinOrCreateRoom(createParams);
		}

		public bool RejoinRoom(string room)
		{
			if (InRoom) return false;

			FLog.Info($"RejoinRoom: {room}");

			_networkService.QuantumRunnerConfigs.IsOfflineMode = false;
			_networkService.LastDisconnectLocation = LastDisconnectionLocation.None;
			return _networkService.QuantumClient.OpRejoinRoom(room);
		}

		public bool LeaveRoom(bool becomeInactive = false)
		{
			if (!InRoom) return false;

			FLog.Info("LeaveRoom");

			return _networkService.QuantumClient.OpLeaveRoom(becomeInactive, true);
		}

		public byte GetMaxSpectators(MatchType matchType)
		{
			if (matchType != MatchType.Custom)
			{
				return 0;
			}

			return _backendService.IsDev()
				? (byte)
				// Limit spectators in development environment, so we have slots to real players
				5
				: (byte) GameConstants.Data.MATCH_SPECTATOR_SPOTS;
		}

		public byte GetMaxPlayers(MatchRoomSetup setup,
								  bool spectators = true)
		{
			var maxPlayers = setup.SimulationConfig.MaxPlayersOverwrite > 0 ? setup.SimulationConfig.MaxPlayersOverwrite : GetMapConfig(setup.SimulationConfig.MapId).MaxPlayers;
			if (spectators)
			{
				maxPlayers += GetMaxSpectators(setup.SimulationConfig.MatchType);
			}

			return (byte) maxPlayers;
		}

		public void RegisterListeners()
		{
			_networkService.QuantumClient.AddCallbackTarget(this);
			_networkService.OnConnectedToMaster += OnConnectedToMaster;
		}

		private void OnConnectedToMaster()
		{
			ResetLocalPlayerProperties();
		}

		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			InitPlayerProperties(newPlayer);
			OnPlayersChange?.Invoke(newPlayer, PlayerChangeReason.Join);
			LastMatchPlayerAmount = CurrentRoom.Players.Count;
		}

		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			OnPlayersChange?.Invoke(otherPlayer, PlayerChangeReason.Leave);
			LastMatchPlayerAmount = CurrentRoom.Players.Count;
		}

		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			if (!InRoom) return;
			FLog.Info("Received properties update " + propertiesThatChanged);
			foreach (var entry in propertiesThatChanged)
			{
				CurrentRoom.Properties.OnReceivedPropertyChange(entry.Key.ToString(), entry.Value);
			}

			OnRoomPropertiesChanged?.Invoke();
		}

		void CheckRoomInit()
		{
			if (CurrentRoom != null && CurrentRoom.Name == _networkService.CurrentRoom.Name)
			{
				return;
			}

			CurrentRoom = new GameRoom(this, _networkService.QuantumClient.CurrentRoom);
			LastRoom = CurrentRoom;
			// Fill room properties
			var properties = _networkService.QuantumClient.CurrentRoom.CustomProperties;
			foreach (var entry in properties)
			{
				CurrentRoom.Properties.OnReceivedPropertyChange(entry.Key.ToString(), entry.Value);
			}

			CurrentRoom.Properties.OnLocalPlayerSetProperty += OnLocalPlayerSetRoomProperty;

			// Fill player properties
			foreach (var player in CurrentRoom.Players.Values)
			{
				InitPlayerProperties(player);
			}

			OnRoomPropertiesChanged?.Invoke();
			OnPlayerPropertiesUpdated?.Invoke();
			SubscribeToPropertyChangeEvents(CurrentRoom);

			// When master joins matchmaking it should set the timer values
			CheckMatchmakingLoadingStart();
		}

		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			if (!InRoom) return;
			FLog.Info($"Received player property {targetPlayer.NickName} update {changedProps}");

			foreach (var entry in changedProps)
			{
				var props = CurrentRoom.GetPlayerProperties(targetPlayer);
				props.OnReceivedPropertyChange(entry.Key.ToString(), entry.Value);
			}

			OnPlayerPropertiesUpdated?.Invoke();
		}

		void IMatchmakingCallbacks.OnJoinedRoom()
		{
			_roomJoinTcs?.TrySetResult();
			_roomJoinTcs = null;

			FLog.Verbose("Joined room!");
			CheckRoomInit();
			OnJoinedRoom?.Invoke();
		}

		private void InitPlayerProperties(Player player)
		{
			var props = CurrentRoom.GetPlayerProperties(player);
			foreach (var entry in player.CustomProperties)
			{
				props.OnReceivedPropertyChange(entry.Key.ToString(), entry.Value);
			}

			if (player.IsLocal)
			{
				props.OnLocalPlayerSetProperty += OnLocalPlayerSetPlayerProperty;
			}
		}

		private void CheckMatchmakingLoadingStart()
		{
			// When master joins matchmaking it should set the timer values
			if (!CurrentRoom.LocalPlayer.IsMasterClient ||
				CurrentRoom.Properties.LoadingStartServerTime.HasValue || CurrentRoom.GameStarted) return;
			var time = _networkService.ServerTimeInMilliseconds;
			// We don't have server time yet lets wait
			if (time == 0) return;
			CurrentRoom.Properties.LoadingStartServerTime.Value = _networkService.ServerTimeInMilliseconds;
			CurrentRoom.Properties.SecondsToStart.Value = CurrentRoom.Properties.SimulationMatchConfig.Value.MatchType == MatchType.Custom
				? Configs.SecondsToLoadCustomGames
				: Configs.MatchmakingLoadingTimeout;
		}

		private void SubscribeToPropertyChangeEvents(GameRoom room)
		{
			room.Properties.GameStarted.OnValueChanged += property =>
			{
				if (property.Value)
				{
					OnMatchStarted?.Invoke();
				}
			};
		}

		private void OnLocalPlayerSetRoomProperty(string key, object value)
		{
			_networkService.QuantumClient.CurrentRoom.SetCustomProperties(new Hashtable
			{
				{key, value}
			});
			FLog.Verbose($"Setting room property {key} to {value}");
		}

		private void OnLocalPlayerSetPlayerProperty(string key, object value)
		{
			CurrentRoom.LocalPlayer.SetCustomProperties(new Hashtable()
			{
				{key, value}
			});
			FLog.Verbose($"Setting local player property {key} to {value}");
		}

		public IEnumerator CheckGameStartCoroutine()
		{
			var oneSec = new WaitForSeconds(1);
			while (true)
			{
				CheckSimulationStart();
				yield return oneSec;
			}
		}

		public void CheckSimulationStart()
		{
			// Sometimes when player joined the room we don't have the server time yet so we don't start the loading timer,
			// so we keep checking if we have the time to start matchmaking
			if (InRoom && CurrentRoom.LocalPlayer.IsMasterClient)
			{
				CheckMatchmakingLoadingStart();
			}

			// Check timer to start the game
			if (!InRoom || !CurrentRoom.LocalPlayer.IsMasterClient || !CurrentRoom.ShouldGameStart() || CurrentRoom.GameStarted)
			{
				return;
			}

			// Check if players loaded assets, give them 5 more seconds then start game
			if (!CurrentRoom.AreAllPlayersReady() && CurrentRoom.GameStartsAt() >
				_networkService.ServerTimeInMilliseconds)
			{
				return;
			}

			// Start game anyway
			StartGame();
		}

		private void StartGame()
		{
			_networkService.CurrentRoom.IsOpen = false;
			CurrentRoom.Properties.GameStarted.Value = true;
			_networkService.QuantumClient.CurrentRoom.EmptyRoomTtl = GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS;
		}

		public void OnJoinRoomFailed(short returnCode, string message)
		{
			_roomJoinTcs?.TrySetException(new Exception("Failed to join room: " + message));
			_roomJoinTcs = null;
			FLog.Info(message);
		}

		public void OnJoinRandomFailed(short returnCode, string message)
		{
			FLog.Info(message);
		}

		public void OnLeftRoom()
		{
			FLog.Verbose("Left room");
			CurrentRoom = null;
			OnLeaveRoom?.Invoke();
		}

		public void OnMasterClientSwitched(Player newMasterClient)
		{
			if (newMasterClient.IsLocal)
			{
				CheckMatchmakingLoadingStart();
			}

			OnMasterChanged?.Invoke();
		}

		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
		}

		public void OnCreatedRoom()
		{
			_roomCreationTcs?.TrySetResult();
			_roomCreationTcs = null;
		}

		public void OnCreateRoomFailed(short returnCode, string message)
		{
			_roomCreationTcs?.TrySetException(new Exception("Failed to create room: " + message));
			_roomCreationTcs = null;
			FLog.Info(message);
		}

		internal void InvokeLocalPlayerKicked()
		{
			OnLocalPlayerKicked?.Invoke();
		}

		public bool KickPlayer(Player playerToKick)
		{
			if (CurrentRoom == null || !CurrentRoom.LocalPlayer.IsMasterClient)
			{
				return false;
			}

			FLog.Info($"KickPlayer: {playerToKick}");
			return _commands.SendKickCommand(playerToKick);
		}

		private void ResetLocalPlayerProperties(PlayerJoinRoomProperties properties = null)
		{
			FLog.Verbose("Setting local player properties");
			// reconnection edge case where 
			if (_networkService.QuantumClient.State == ClientState.Joining)
			{
				FLog.Warn("Skipped property reset, client was still joining");
				return;
			}

			_networkService.QuantumClient.NickName = AuthenticationService.Instance.GetPlayerNameWithSpaces();
			var preloadIds = new List<GameId>();

			preloadIds.Add(_dataProvider.CollectionDataProvider.GetEquipped(new (GameIdGroup.PlayerSkin)).Id);
			preloadIds.Add(_dataProvider.CollectionDataProvider.GetEquipped(new (GameIdGroup.MeleeSkin)).Id);

			var props = new PlayerProperties
			{
				Loadout = {Value = preloadIds},
				Spectator = {Value = properties?.Spectator ?? false},
				CoreLoaded = {Value = false},
				TeamId = {Value = properties?.Team},
				Rank = {Value = _leaderboardService.CurrentRankedEntry.Position},
				ColorIndex = {Value = properties?.TeamColor ?? 0}
			};

			_networkService.LocalPlayer.SetCustomProperties(props.ToHashTable());
		}
	}
}