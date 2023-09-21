using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.Services.RoomService
{
	public enum PlayerChangeReason
	{
		Join,
		Leave,
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
		/// Triggered when custom game loading starts
		/// </summary>
		event Action OnCustomGameLoadStart;

		/// <summary>
		/// Do you need docs ?
		/// </summary>
		event Action OnMasterChanged;

		/// <summary>
		/// Yo
		/// </summary>
		event Action OnPlayerPropertiesUpdated;

		bool InRoom { get; }

		GameRoom CurrentRoom { get; }
		GameRoom LastRoom { get; }
		byte GetMaxSpectators(MatchType matchType);

		byte GetMaxPlayers(QuantumGameModeConfig gameModeConfig,
						   QuantumMapConfig mapConfig,
						   MatchType type,
						   bool spectators = true);

		/// <summary>
		///  Used for custom games owner clicks to start, it will move players to load screen
		/// </summary>
		/// <returns></returns>
		void StartCustomGameLoading();

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
		bool JoinRoom(string roomName);

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
		bool CreateRoom(MatchRoomSetup setup, bool offlineMode);

		/// <summary>
		/// Joins a specific room with matching params if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <param name="roomName">Name of the room to join</param>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>Note, in order to join a room, the "entry params" that are generated, need to match a created room exactly
		/// for the client to be able to enter. If there is even one param mismatching, join operation will fail.
		/// Used for Playfab matchmaking, because with playfab matchmaking we know the name of the room, so a random player will create it and the others will join
		/// </remarks>
		bool JoinOrCreateRoom(MatchRoomSetup setup, string teamID = null, string[] expectedPlayers = null);

		/// <summary>
		/// Joins a random room of matching parameters if it exists, or creates a new one if it doesn't
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		/// <remarks>This is used for the "OLD" matchmaking, we don't have the room name so it will try to match a match with same properties</remarks>
		bool JoinOrCreateRandomRoom(MatchRoomSetup setup);

		/// <summary>
		/// Leaves the current room that local player is in
		/// </summary>
		/// <returns>True if the operation was sent successfully</returns>
		bool LeaveRoom(bool becomeInactive);
	}

	public class RoomService : IInRoomCallbacks, IMatchmakingCallbacks, IRoomService
	{
		internal IGameNetworkService _networkService;
		private IGameBackendService _backendService;
		internal IConfigsProvider _configsProvider;
		private ICoroutineService _coroutineService;

		public GameRoom CurrentRoom { get; private set; }
		public GameRoom LastRoom { get; private set; }

		private RoomServiceParameters _parameters;


		public event Action<Player, PlayerChangeReason> OnPlayersChange;
		public event Action OnRoomPropertiesChanged;
		public event Action OnMatchStarted;
		public event Action OnCustomGameLoadStart;
		public event Action OnMasterChanged;
		public event Action OnPlayerPropertiesUpdated;

		public bool InRoom => CurrentRoom != null;

		internal MatchmakingAndRoomConfig Configs => _configsProvider.GetConfig<MatchmakingAndRoomConfig>();

		public RoomService(IGameNetworkService networkService, IGameBackendService backendService, IConfigsProvider configsProvider,
						   ICoroutineService coroutineService)
		{
			_networkService = networkService;
			_backendService = backendService;
			_configsProvider = configsProvider;
			_coroutineService = coroutineService;
			_parameters = new RoomServiceParameters(this);
			RegisterListeners();

			// TODO: Optimize to only start this coroutine on game join
			_coroutineService.StartCoroutine(CheckGameStartCoroutine());
		}

		public QuantumGameModeConfig GetGameModeConfig(string gameModeId) => _configsProvider.GetConfig<QuantumGameModeConfig>(gameModeId);
		public QuantumMapConfig GetMapConfig(int mapId) => _configsProvider.GetConfig<QuantumMapConfig>(mapId);


		/// <summary>
		/// Used for joining custom games
		/// </summary>
		/// <param name="roomName"></param>
		/// <returns></returns>
		public bool JoinRoom(string roomName)
		{
			FLog.Info($"JoinRoom: {InRoom}");

			if (InRoom) return false;

			var enterParams = _parameters.GetRoomEnterParams(roomName);
			_networkService.QuantumRunnerConfigs.IsOfflineMode = false;

			_networkService.ResetQuantumProperties();
			_networkService.SetSpectatePlayerProperty(false);
			_networkService.LastUsedSetup.Value = null;

			return _networkService.QuantumClient.OpJoinRoom(enterParams);
		}

		/// <summary>
		/// This used for custom games or forced matches
		/// </summary>
		public bool CreateRoom(MatchRoomSetup setup, bool offlineMode)
		{
			if (InRoom) return false;

			FLog.Info($"CreateRoom: {setup}");

			var createParams = _parameters.GetRoomCreateParams(setup, offlineMode);

			_networkService.QuantumRunnerConfigs.IsOfflineMode = offlineMode;

			_networkService.ResetQuantumProperties();
			_networkService.SetSpectatePlayerProperty(false);
			_networkService.LastDisconnectLocation = LastDisconnectionLocation.None;
			_networkService.LastUsedSetup.Value = setup;
			return _networkService.QuantumClient.OpCreateRoom(createParams);
		}


		/// <summary>
		/// Used for Playfab matchmaking, because with playfab matchmaking we know the name of the room, so a random player will create it and the others will join
		/// </summary>
		/// <returns></returns>
		public bool JoinOrCreateRoom(MatchRoomSetup setup, string teamID = null, string[] expectedPlayers = null)
		{
			if (InRoom) return false;

			FLog.Info($"JoinOrCreateRoom: {setup}");

			var createParams = _parameters.GetRoomCreateParams(setup);
			_networkService.QuantumRunnerConfigs.IsOfflineMode = false;

			_networkService.ResetQuantumProperties(teamID);
			_networkService.SetSpectatePlayerProperty(false);
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


		/// <summary>
		/// This is used for the "OLD" matchmaking, we don't have the room name so it will try to match a match with same properties
		/// </summary>
		/// <returns></returns>
		public bool JoinOrCreateRandomRoom(MatchRoomSetup setup)
		{
			if (InRoom) return false;

			FLog.Info($"JoinOrCreateRandomRoom: {setup}");

			var createParams = _parameters.GetRoomCreateParams(setup);
			var joinRandomParams = _parameters.GetJoinRandomRoomParams(setup);

			FLog.Verbose(ModelSerializer.PrettySerialize(createParams));
			FLog.Verbose(ModelSerializer.PrettySerialize(joinRandomParams));
			_networkService.QuantumRunnerConfigs.IsOfflineMode = false;

			_networkService.ResetQuantumProperties();

			_networkService.SetSpectatePlayerProperty(false);
			_networkService.LastDisconnectLocation = LastDisconnectionLocation.None;
			_networkService.LastUsedSetup.Value = setup;

			return _networkService.QuantumClient.OpJoinRandomOrCreateRoom(joinRandomParams, createParams);
		}


		public bool LeaveRoom(bool becomeInactive)
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

		public byte GetMaxPlayers(QuantumGameModeConfig gameModeConfig,
								  QuantumMapConfig mapConfig,
								  MatchType type,
								  bool spectators = true)
		{
			var maxPlayers = Math.Min(gameModeConfig.MaxPlayers, mapConfig.MaxPlayers);
			if (spectators)
			{
				maxPlayers += GetMaxSpectators(type);
			}

			// Quantum development servers only allow 20 players :(
			if (_backendService.IsDev())
			{
				maxPlayers = Math.Min(20, maxPlayers);
			}

			return (byte) maxPlayers;
		}


		public void RegisterListeners()
		{
			_networkService.QuantumClient.AddCallbackTarget(this);
		}


		public void OnPlayerEnteredRoom(Player newPlayer)
		{
			OnPlayersChange?.Invoke(newPlayer, PlayerChangeReason.Join);
		}

		public void OnPlayerLeftRoom(Player otherPlayer)
		{
			OnPlayersChange?.Invoke(otherPlayer, PlayerChangeReason.Leave);
		}

		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			if (!InRoom) return;
			FLog.Info("Received properties update");
			foreach (var entry in propertiesThatChanged)
			{
				CurrentRoom.Properties.OnReceivedPropertyChange(entry.Key.ToString(), entry.Value);
			}

			OnRoomPropertiesChanged?.Invoke();
		}


		public void OnJoinedRoom()
		{
			CurrentRoom = new GameRoom(this, _networkService.QuantumClient.CurrentRoom);
			LastRoom = CurrentRoom;
			var properties = _networkService.QuantumClient.CurrentRoom.CustomProperties;
			foreach (var entry in properties)
			{
				CurrentRoom.Properties.OnReceivedPropertyChange(entry.Key.ToString(), entry.Value);
			}

			CurrentRoom.Properties.OnLocalPlayerSetProperty += OnLocalPlayerSetProperty;
			OnRoomPropertiesChanged?.Invoke();
			SubscribeToPropertyChangeEvents(CurrentRoom);

			// When master joins matchmaking it should set the timer values
			CheckMatchmakingLoadingStart();
		}

		private void CheckMatchmakingLoadingStart()
		{
			// When master joins matchmaking it should set the timer values
			if (!CurrentRoom.LocalPlayer.IsMasterClient || CurrentRoom.Properties.MatchType.Value == MatchType.Custom ||
				CurrentRoom.Properties.LoadingStartServerTime.HasValue || CurrentRoom.GameStarted) return;
			var time = _networkService.ServerTimeInMilliseconds;
			// We don't have server time yet lets wait
			if (time == 0) return;
			CurrentRoom.Properties.LoadingStartServerTime.Value = _networkService.ServerTimeInMilliseconds;
			CurrentRoom.Properties.SecondsToStart.Value = Configs.SecondsToStartOldMatchmakingRoom;
		}

		private void SubscribeToPropertyChangeEvents(GameRoom room)
		{
			room.Properties.StartCustomGame.OnValueChanged += property =>
			{
				if (property.Value)
				{
					// Game started
					OnCustomGameLoadStart?.Invoke();
				}
			};
			room.Properties.GameStarted.OnValueChanged += property =>
			{
				if (property.Value)
				{
					OnMatchStarted?.Invoke();
				}
			};
		}

		private void OnLocalPlayerSetProperty(string key, object value)
		{
			_networkService.QuantumClient.CurrentRoom.SetCustomProperties(new Hashtable
			{
				{key, value}
			});
			FLog.Verbose($"Setting property {key} to {value}");
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
			if (InRoom && !CurrentRoom.GameStarted)
			{
				FLog.Verbose("game starts in " + CurrentRoom.TimeLeftToGameStart().TotalSeconds);
			}

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

			if (CurrentRoom.Properties.MatchType.Value == MatchType.Custom && !CurrentRoom.Properties.StartCustomGame.Value)
			{
				return;
			}

			// Check if players loaded assets, give them 5 more seconds then start game
			if (!CurrentRoom.AreAllPlayersReady() && CurrentRoom.GameStartsAt() + (Configs.SecondsLoadingTimeout * 1000) >
				_networkService.ServerTimeInMilliseconds)
			{
				return;
			}

			// Start game anyway
			StartGame();
		}

		/// <summary>
		///  Used for custom games owner clicks to start, it will move players to load screen
		/// </summary>
		/// <returns></returns>
		public void StartCustomGameLoading()
		{
			if (!_networkService.LocalPlayer.IsMasterClient)
			{
				return;
			}

			_networkService.CurrentRoom.IsOpen = false;
			CurrentRoom.Properties.LoadingStartServerTime.Value =
				_networkService.ServerTimeInMilliseconds;
			CurrentRoom.Properties.SecondsToStart.Value = Configs.SecondsToLoadCustomGames;
			CurrentRoom.Properties.StartCustomGame.Value = true;
		}

		private void StartGame()
		{
			_networkService.CurrentRoom.IsOpen = false;
			CurrentRoom.Properties.GameStarted.Value = true;
			_networkService.QuantumClient.CurrentRoom.EmptyRoomTtl = GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS;
		}


		public void OnJoinRoomFailed(short returnCode, string message)
		{
			FLog.Info(message);
		}

		public void OnJoinRandomFailed(short returnCode, string message)
		{
			FLog.Info(message);
		}

		public void OnLeftRoom()
		{
			CurrentRoom = null;
		}

		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			OnPlayerPropertiesUpdated?.Invoke();
		}

		public void OnMasterClientSwitched(Player newMasterClient)
		{
			if (newMasterClient.IsLocal && CurrentRoom.Properties.MatchType.Value != MatchType.Custom)
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
		}

		public void OnCreateRoomFailed(short returnCode, string message)
		{
			FLog.Info(message);
		}
	}
}