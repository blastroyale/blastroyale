using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Network State and communication with Quantum servers in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class NetworkState : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
	{
		public static readonly IStatechartEvent PhotonMasterConnectedEvent = new StatechartEvent("NETWORK - Photon Master Connected Event");
		public static readonly IStatechartEvent PhotonDisconnectedEvent = new StatechartEvent("NETWORK - Photon Disconnected Event");
		public static readonly IStatechartEvent PhotonCriticalDisconnectedEvent = new StatechartEvent("NETWORK - Photon Critical Disconnected Event");
		
		public static readonly IStatechartEvent ConnectToRegionMasterEvent = new StatechartEvent("NETWORK - Connect To Region Master");
		public static readonly IStatechartEvent ConnectToNameServerFailEvent = new StatechartEvent("NETWORK - Connected To Name Fail Server Event");
		public static readonly IStatechartEvent RegionListReceivedEvent = new StatechartEvent("NETWORK - Regions List Received");
		
		public static readonly IStatechartEvent CreateRoomFailedEvent = new StatechartEvent("NETWORK - Create Room Failed Event");
		public static readonly IStatechartEvent JoinedRoomEvent = new StatechartEvent("NETWORK - Joined Room Event");
		public static readonly IStatechartEvent JoinRoomFailedEvent = new StatechartEvent("NETWORK - Join Room Fail Event");
		public static readonly IStatechartEvent LeftRoomEvent = new StatechartEvent("NETWORK - Left Room Event");
		public static readonly IStatechartEvent RoomClosedEvent = new StatechartEvent("NETWORK - Room Closed Event");
		
		public static readonly IStatechartEvent DcScreenBackEvent = new StatechartEvent("NETWORK - Disconnected Screen Back Event");
		public static readonly IStatechartEvent OpenServerSelectScreenEvent = new StatechartEvent("NETWORK - Open Server Select Screen Event");

		public static readonly IStatechartEvent IapProcessStartedEvent = new StatechartEvent("NETWORK - IAP Started Event");
		public static readonly IStatechartEvent IapProcessFinishedEvent = new StatechartEvent("NETWORK - IAP Processed Event");
		public static readonly IStatechartEvent ForceConnectionCheckEvent = new StatechartEvent("NETWORK - Force Connection Check Event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _criticalDisconnectCoroutine;
		private Coroutine _matchmakingCoroutine;
		private bool _requiresManualRoomReconnection;

		private QuantumRunnerConfigs QuantumRunnerConfigs => _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

		public NetworkState(IGameLogic gameLogic, IGameServices services,
		                    IGameBackendNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = gameLogic;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_networkService.QuantumClient.AddCallbackTarget(this);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("NETWORK - Initial");
			var final = stateFactory.Final("NETWORK - Final");
			var initialConnection = stateFactory.State("NETWORK - Initial Connection");
			var connected = stateFactory.State("NETWORK - Connected");
			var disconnected = stateFactory.State("NETWORK - Disconnected");
			var disconnectForServerSelect = stateFactory.State("NETWORK - Disconnect Photon For Name Server");
			var getAvailableRegions = stateFactory.State("NETWORK - Server Select Screen");
			var connectedToNameServer = stateFactory.State("NETWORK - Connected To Name Server");
			var connectToRegionMaster = stateFactory.State("NETWORK - Connect To Region Master");
			var connectionCheck = stateFactory.Choice("NETWORK - Connection Check");
			var iapProcessing = stateFactory.State("NETWORK - IAP Processing");
			
			initial.Transition().Target(initialConnection);
			initial.OnExit(SubscribeEvents);

			initialConnection.OnEnter(ConnectPhoton);
			initialConnection.Event(PhotonMasterConnectedEvent).Target(connected);
			
			iapProcessing.Event(IapProcessFinishedEvent).OnTransition(HandleIapTransition).Target(connected);
			
			connectionCheck.Transition().Condition(IsPhotonConnectedAndReady).Target(connected);
			connectionCheck.Transition().Target(disconnected);
			
			connected.Event(PhotonDisconnectedEvent).Target(disconnected);
			connected.Event(OpenServerSelectScreenEvent).Target(disconnectForServerSelect);
			
			disconnected.OnEnter(UpdateLastDisconnectLocation);
			disconnected.OnEnter(SubscribeDisconnectEvents);
			disconnected.Event(PhotonMasterConnectedEvent).Target(connected);
			disconnected.Event(JoinedRoomEvent).Target(connected);
			disconnected.OnExit(UnsubscribeDisconnectEvents);

			disconnectForServerSelect.OnEnter(DisconnectPhoton);
			disconnectForServerSelect.Event(PhotonDisconnectedEvent).Target(getAvailableRegions);

			getAvailableRegions.OnEnter(ConnectToNameServer);
			getAvailableRegions.Event(RegionListReceivedEvent).Target(connectedToNameServer);
			getAvailableRegions.Event(ConnectToNameServerFailEvent).Target(disconnected);

			connectedToNameServer.Event(ConnectToRegionMasterEvent).Target(connectToRegionMaster);
			
			connectToRegionMaster.OnEnter(ConnectPhotonToRegionMaster);
			connectToRegionMaster.Event(PhotonMasterConnectedEvent).Target(connected);
			connectToRegionMaster.Event(PhotonDisconnectedEvent).Target(disconnected);
			
			final.OnEnter(UnsubscribeEvents);
		}

		/// <summary>
		/// This method receives all photon events, but is only used for our custom in-game events
		/// </summary>
		public void OnEvent(EventData photonEvent)
		{
			if (photonEvent.Code == (byte) QuantumCustomEvents.KickPlayer)
			{
				OnKickPlayerEventReceived((int) photonEvent.CustomData, photonEvent.Sender);
			}
		}

		private void OnKickPlayerEventReceived(int userIdToLeave, int senderIndex)
		{
			if (_networkService.QuantumClient.LocalPlayer.ActorNumber != userIdToLeave ||
				!_networkService.QuantumClient.InRoom ||
				_networkService.QuantumClient.CurrentRoom.MasterClientId != senderIndex)
			{
				return;
			}

			LeaveRoom();

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK.ToUpper(),
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.MainMenu.MatchmakingKickedNotification.ToUpper(), false, confirmButton);
		}

		private void UpdateLastDisconnectLocation()
		{
			// Only update DC location for main menu - match disconnections are more complex, and handled specifically
			// inside of MatchState.
			if (!CurrentSceneIsMatch())
			{
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Menu;
			}
		}

		private bool CurrentSceneIsMatch()
		{
			return SceneManager.GetActiveScene().name != GameConstants.Scenes.SCENE_MAIN_MENU;
		}

		private void HandleIapTransition()
		{
			_services.CoroutineService.StartCoroutine(ConnectionCheckCoroutine());
			ReconnectPhoton();
		}

		private IEnumerator ConnectionCheckCoroutine()
		{
			yield return new WaitForSeconds(7.5f);

			_statechartTrigger(ForceConnectionCheckEvent);
		}

		private void SubscribeEvents()
		{
			_services.TickService.SubscribeOnUpdate(TickQuantumServer, GameConstants.Network.NETWORK_QUANTUM_TICK_SECONDS, true, true);
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			_services.MessageBrokerService.Subscribe<MatchSimulationEndedMessage>(OnMatchSimulationEndedMessage);
			_services.MessageBrokerService.Subscribe<PlayMatchmakingReadyMessage>(OnPlayMatchmakingReadyMessage);
			_services.MessageBrokerService.Subscribe<PlayMapClickedMessage>(OnPlayMapClickedMessage);
			_services.MessageBrokerService.Subscribe<PlayJoinRoomClickedMessage>(OnPlayJoinRoomClickedMessage);
			_services.MessageBrokerService.Subscribe<PlayCreateRoomClickedMessage>(OnPlayCreateRoomClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomLeaveClickedMessage>(OnRoomLeaveClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomLockClickedMessage>(OnRoomLockClicked);
			_services.MessageBrokerService.Subscribe<CoreMatchAssetsLoadedMessage>(OnCoreMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<AllMatchAssetsLoadedMessage>(OnAllMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<AssetReloadRequiredMessage>(OnAssetReloadRequiredMessage);
			_services.MessageBrokerService.Subscribe<SpectatorModeToggledMessage>(OnSpectatorToggleMessage);
			_services.MessageBrokerService.Subscribe<RequestKickPlayerMessage>(OnRequestKickPlayerMessage);
			_services.MessageBrokerService.Subscribe<NetworkActionWhileDisconnectedMessage>(OnNetworkActionWhileDisconnected);
			_services.MessageBrokerService.Subscribe<AttemptManualReconnectionMessage>(OnAttemptManualReconnectionMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.TickService?.UnsubscribeAll(this);
		}

		private void SubscribeDisconnectEvents()
		{
			_services.TickService.SubscribeOnUpdate(TickReconnectAttempt, GameConstants.Network.NETWORK_ATTEMPT_RECONNECT_SECONDS);
			_criticalDisconnectCoroutine = _services.CoroutineService.StartCoroutine(CriticalDisconnectCoroutine());
		}

		private void UnsubscribeDisconnectEvents()
		{
			_services.TickService.Unsubscribe(TickReconnectAttempt);

			if (_criticalDisconnectCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_criticalDisconnectCoroutine);
			}
		}

		private IEnumerator CriticalDisconnectCoroutine()
		{
			yield return new WaitForSeconds(GameConstants.Network.CRITICAL_DISCONNECT_THRESHOLD_SECONDS);
			_statechartTrigger(PhotonCriticalDisconnectedEvent);
		}

		/// <inheritdoc />
		public void OnConnected()
		{
			FLog.Info("OnConnected");
		}

		/// <inheritdoc />
		public void OnConnectedToMaster()
		{
			FLog.Info("OnConnectedToMaster");
			
			_statechartTrigger(PhotonMasterConnectedEvent);

			// Reconnections during matchmaking screen require manual reconnection to the room, due to TTL 0
			if (_requiresManualRoomReconnection &&
			    _networkService.LastDisconnectLocation.Value == LastDisconnectionLocation.Matchmaking)
			{
				_requiresManualRoomReconnection = false;
				JoinRoom(_networkService.LastConnectedRoomName.Value.StripRoomCommitLock(), false);
			}
		}

		/// <inheritdoc />
		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("OnDisconnected " + cause);

			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Disconnection,
			                                                   _services.NetworkService.QuantumClient.DisconnectedCause
			                                                            .ToString());

			_statechartTrigger(PhotonDisconnectedEvent);
		}

		/// <inheritdoc />
		public void OnCreatedRoom()
		{
			FLog.Info("OnCreatedRoom");
		}

		/// <inheritdoc />
		public void OnCreateRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnCreateRoomFailed: {returnCode.ToString()} - {message}");

			var desc = string.Format(ScriptLocalization.MainMenu.RoomError, message);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);

			_statechartTrigger(CreateRoomFailedEvent);
		}

		/// <inheritdoc />
		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");

			_statechartTrigger(JoinedRoomEvent);

			_networkService.LastConnectedRoomName.Value = _networkService.QuantumClient.CurrentRoom.Name;

			if (_networkService.IsJoiningNewMatch.Value)
			{
				// Switch players from player to spectator, and vice versa, if the relevant room capacity is full
				var isSpectator = (bool) _networkService.QuantumClient.LocalPlayer.CustomProperties
					[GameConstants.Network.PLAYER_PROPS_SPECTATOR];

				if (!isSpectator && _networkService.QuantumClient.CurrentRoom.GetRealPlayerAmount() >
				    _networkService.QuantumClient.CurrentRoom.GetRealPlayerCapacity())
				{
					SetSpectatePlayerProperty(true);
				}
				else if (isSpectator && _networkService.QuantumClient.CurrentRoom.GetSpectatorAmount() >
				         _networkService.QuantumClient.CurrentRoom.GetSpectatorCapacity())
				{
					SetSpectatePlayerProperty(false);
				}
			}

			if (QuantumRunnerConfigs.IsOfflineMode)
			{
				LockRoom();
			}
			else if (_networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom())
			{
				StartMatchmakingLockRoomTimer();
			}
		}

		/// <inheritdoc />
		public void OnJoinRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnJoinRoomFailed: {returnCode.ToString()} - {message}");

			var desc = string.Format(ScriptLocalization.MainMenu.RoomError, message);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);

			_statechartTrigger(JoinRoomFailedEvent);
		}

		/// <inheritdoc />
		public void OnJoinRandomFailed(short returnCode, string message)
		{
			OnJoinRoomFailed(returnCode, message);
		}

		/// <inheritdoc />
		public void OnLeftRoom()
		{
			FLog.Info("OnLeftRoom");

			if (_matchmakingCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_matchmakingCoroutine);
			}

			_statechartTrigger(LeftRoomEvent);
		}

		/// <inheritdoc />
		public void OnPlayerEnteredRoom(Player player)
		{
			FLog.Info($"OnPlayerEnteredRoom {player.NickName}");
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player player)
		{
			FLog.Info($"OnPlayerLeftRoom {player.NickName}");

			var allPlayersReady = _networkService.QuantumClient.CurrentRoom.AreAllPlayersReady();
				
			if (_networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom() && !allPlayersReady)
			{
				StartMatchmakingLockRoomTimer();
			}
			else if (allPlayersReady)
			{
				_statechartTrigger(MatchState.AllPlayersReadyEvent);
			}
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable changedProps)
		{
			FLog.Info("OnRoomPropertiesUpdate");

			if (changedProps.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				_statechartTrigger(RoomClosedEvent);
			}
		}

		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			FLog.Info("OnPlayerPropertiesUpdate " + targetPlayer.NickName);

			if (changedProps.TryGetValue(GameConstants.Network.PLAYER_PROPS_ALL_LOADED, out var loadedMatch) &&
			    (bool) loadedMatch)
			{
				_services.MessageBrokerService.Publish(new PlayerLoadedMatchMessage());

				if (_networkService.QuantumClient.CurrentRoom.AreAllPlayersReady())
				{
					_statechartTrigger(MatchState.AllPlayersReadyEvent);
				}
			}
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			FLog.Info("OnMasterClientSwitched " + newMasterClient.NickName);
		}

		/// <inheritdoc />
		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			FLog.Info("OnRegionListReceived " + regionHandler.GetResults());

			_services.MessageBrokerService.Publish(new RegionListReceivedMessage(){RegionHandler = regionHandler});
			
			_networkService.QuantumClient.RegionHandler.PingMinimumOfRegions(OnPingedRegions, "");
			
			_statechartTrigger(RegionListReceivedEvent);
		}
		
		// NOTE: THIS DOES NOT EXECUTE ON MAIN THREAD BECAUSE PHOTON IS PHOTON
		private void OnPingedRegions(RegionHandler regionHandler)
		{
			FLog.Info("OnPingedRegions" + regionHandler.GetResults());

			_services.ThreadService.MainThreadDispatcher.Enqueue(() =>
			{
				_services.MessageBrokerService.Publish(new PingedRegionsMessage(){RegionHandler = regionHandler});
			});
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
			FLog.Info("OnCustomAuthenticationResponse " + data.Count);
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationFailed(string debugMessage)
		{
			FLog.Info("OnCustomAuthenticationResponse " + debugMessage);
		}

		/// <inheritdoc />
		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
			FLog.Info("OnFriendListUpdate " + friendList.Count);
		}

		private void OnRoomLockClicked(RoomLockClickedMessage message)
		{
			_networkService.QuantumClient.CurrentRoom.SetCustomProperties(new Hashtable
			{
				{GameConstants.Network.ROOM_PROPS_BOTS, message.AddBots}
			});

			LockRoom();
		}

		private void OnSpectatorToggleMessage(SpectatorModeToggledMessage message)
		{
			SetSpectatePlayerProperty(message.IsSpectator);
		}

		private void SetSpectatePlayerProperty(bool isSpectator)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_SPECTATOR, isSpectator}
			};

			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		private void OnRequestKickPlayerMessage(RequestKickPlayerMessage msg)
		{
			if (_networkService.QuantumClient.CurrentRoom == null ||
			    !_networkService.QuantumClient.LocalPlayer.IsMasterClient)
			{
				return;
			}

			var eventOptions = new RaiseEventOptions() { Receivers = ReceiverGroup.All };
			_networkService.QuantumClient.OpRaiseEvent((byte) QuantumCustomEvents.KickPlayer, msg.Player.ActorNumber, eventOptions,
			                                           SendOptions.SendReliable);
		}
		
		private void OnAttemptManualReconnectionMessage(AttemptManualReconnectionMessage obj)
		{
			ReconnectPhoton();
		}
		
		private void OnNetworkActionWhileDisconnected(NetworkActionWhileDisconnectedMessage msg)
		{
			if (!NetworkUtils.IsOnline() || !_networkService.QuantumClient.IsConnectedAndReady)
			{
				_statechartTrigger(PhotonCriticalDisconnectedEvent);
			}
		}

		private void OnRoomLeaveClickedMessage(RoomLeaveClickedMessage msg)
		{
			LeaveRoom();
		}

		private void OnMatchSimulationEndedMessage(MatchSimulationEndedMessage msg)
		{
			LeaveRoom();
		}

		private void OnMatchSimulationStartedMessage(MatchSimulationStartedMessage msg)
		{
			_networkService.LastMatchPlayers.Clear();

			foreach (var player in _networkService.QuantumClient.CurrentRoom.Players.Values)
			{
				_networkService.LastMatchPlayers.Add(player);
			}
			
			// Once match starts, TTL needs to be set to max so player can DC+RC easily
			if(_networkService.QuantumClient.LocalPlayer.IsMasterClient) 
			{
				_networkService.QuantumClient.CurrentRoom.PlayerTtl = GameConstants.Network.PLAYER_GAME_TTL_MS;
				
				if(!_networkService.QuantumClient.CurrentRoom.IsPlayTestRoom())
				{
					_networkService.QuantumClient.CurrentRoom.EmptyRoomTtl = GameConstants.Network.EMPTY_ROOM_GAME_TTL_MS;
				}
			}
		}

		private void OnPlayMatchmakingReadyMessage(PlayMatchmakingReadyMessage msg)
		{
			var selectedGameMode = _services.GameModeService.SelectedGameMode.Value;
			var gameModeId = selectedGameMode.Entry.GameModeId;
			var mutators = selectedGameMode.Entry.Mutators;
			var mapConfig = NetworkUtils.GetRotationMapConfig(gameModeId, _services);
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());

			StartRandomMatchmaking(gameModeConfig, mapConfig, mutators);
		}

		private void OnPlayMapClickedMessage(PlayMapClickedMessage msg)
		{
			var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(msg.MapId);
			var selectedGameMode = _services.GameModeService.SelectedGameMode.Value;
			var gameModeId = selectedGameMode.Entry.GameModeId;
			var mutators = selectedGameMode.Entry.Mutators;
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());

			StartRandomMatchmaking(gameModeConfig, mapConfig, mutators);
		}

		private void OnPlayCreateRoomClickedMessage(PlayCreateRoomClickedMessage msg)
		{
			var gameModeId = msg.GameModeConfig.Id;
			var gameModeConfig = _services.ConfigsProvider.GetConfig<QuantumGameModeConfig>(gameModeId.GetHashCode());
			_gameDataProvider.AppDataProvider.SetLastCustomGameOptions(msg.CustomGameOptions);
			_services.DataSaver.SaveData<AppData>();
			if (msg.JoinIfExists)
			{
				JoinOrCreateRoom(gameModeConfig, msg.MapConfig, msg.CustomGameOptions.Mutators, msg.RoomName);
			}
			else
			{
				CreateRoom(gameModeConfig, msg.MapConfig, msg.CustomGameOptions.Mutators, msg.RoomName);
			}
		}

		private void OnPlayJoinRoomClickedMessage(PlayJoinRoomClickedMessage msg)
		{
			JoinRoom(msg.RoomName);
		}

		private void OnCoreMatchAssetsLoaded(CoreMatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_CORE_LOADED, true}
			};

			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}

		private void OnAllMatchAssetsLoaded(AllMatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_ALL_LOADED, true}
			};

			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}

		private void OnAssetReloadRequiredMessage(AssetReloadRequiredMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_CORE_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_ALL_LOADED, false}
			};

			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		private void OnApplicationQuit(ApplicationQuitMessage data)
		{
			_networkService.QuantumClient.Disconnect();
		}

		private void StartRandomMatchmaking(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators)
		{
			var matchType = _services.GameModeService.SelectedGameMode.Value.Entry.MatchType;
			var gameHasBots = gameModeConfig.AllowBots;
			var gridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var createParams =
				NetworkUtils.GetRoomCreateParams(gameModeConfig, mapConfig, gridConfigs, null, matchType, mutators, gameHasBots);
			var joinRandomParams = NetworkUtils.GetJoinRandomRoomParams(gameModeConfig, mapConfig, matchType, mutators);

			QuantumRunnerConfigs.IsOfflineMode = NetworkUtils.GetMaxPlayers(gameModeConfig, mapConfig) == 1;

			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				_networkService.QuantumClient.OpJoinRandomOrCreateRoom(joinRandomParams, createParams);
			}
		}

		private void JoinRoom(string roomName, bool resetLastDcLocation = true)
		{
			var enterParams = NetworkUtils.GetRoomEnterParams(roomName);

			QuantumRunnerConfigs.IsOfflineMode = false;

			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;

				if (resetLastDcLocation)
				{
					_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				}
				
				_networkService.QuantumClient.OpJoinRoom(enterParams);
			}
		}

		private void CreateRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, string roomName)
		{
			var gridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var createParams = NetworkUtils.GetRoomCreateParams(gameModeConfig, mapConfig, gridConfigs, roomName, MatchType.Custom, mutators, false);

			QuantumRunnerConfigs.IsOfflineMode = false;

			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				_networkService.QuantumClient.OpCreateRoom(createParams);
			}
		}
		
		private void JoinOrCreateRoom(QuantumGameModeConfig gameModeConfig, QuantumMapConfig mapConfig, List<string> mutators, string roomName)
		{
			var gridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var createParams = NetworkUtils.GetRoomCreateParams(gameModeConfig, mapConfig, gridConfigs, roomName, MatchType.Custom, mutators, false);

			QuantumRunnerConfigs.IsOfflineMode = false;

			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				_networkService.QuantumClient.OpJoinOrCreateRoom(createParams);
			}
		}

		private void TickQuantumServer(float deltaTime)
		{
			_networkService.QuantumClient.Service();
			_networkService.CheckLag();
		}

		private void TickReconnectAttempt(float deltaTime)
		{
			if (!_networkService.QuantumClient.IsConnectedAndReady && NetworkUtils.IsOnline())
			{
				ReconnectPhoton();
			}
		}

		private void StartMatchmakingLockRoomTimer()
		{
			if (!_networkService.QuantumClient.LocalPlayer.IsMasterClient ||
			    !_networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom()) 
			{
				return;
			}

			if (_networkService.QuantumClient.CurrentRoom.GetMatchType() == MatchType.Ranked)
			{
				_matchmakingCoroutine = _services.CoroutineService.StartCoroutine(RankedMatchmakingCoroutine());
			}
			else
			{
				_matchmakingCoroutine = _services.CoroutineService.StartCoroutine(CasualMatchmakingCoroutine());
			}
		}

		private void LockRoom()
		{
			var room = _networkService?.QuantumClient?.CurrentRoom;

			if (room != null && room.IsOpen)
			{
				room.IsOpen = false;
			}
		}

		private IEnumerator CasualMatchmakingCoroutine()
		{
			var oneSecond = new WaitForSeconds(1f);
			var roomCreationTime = _networkService.QuantumClient.CurrentRoom.GetRoomCreationDateTime();
			var matchmakingEndTime = roomCreationTime.AddSeconds(_services.ConfigsProvider.GetConfig<QuantumGameConfig>().CasualMatchmakingTime.AsFloat);
			var room = _networkService.QuantumClient.CurrentRoom;

			while ((DateTime.UtcNow < matchmakingEndTime && !room.IsAtFullPlayerCapacity()))
			{
				yield return oneSecond;
			}

			LockRoom();
		}

		private IEnumerator RankedMatchmakingCoroutine()
		{
			var oneSecond = new WaitForSeconds(1f);
			var roomCreationTime = _networkService.QuantumClient.CurrentRoom.GetRoomCreationDateTime();
			var matchmakingEndTime = roomCreationTime.AddSeconds(_services.ConfigsProvider.GetConfig<QuantumGameConfig>().RankedMatchmakingTime.AsFloat);
			var minPlayers = _services.ConfigsProvider.GetConfig<QuantumGameConfig>().RankedMatchmakingMinPlayers;
			var room = _networkService.QuantumClient.CurrentRoom;

			while ((DateTime.UtcNow < matchmakingEndTime && !room.IsAtFullPlayerCapacity()) ||
			       (DateTime.UtcNow >= matchmakingEndTime && room.GetRealPlayerAmount() < minPlayers))
			{
				yield return oneSecond;
			}

			LockRoom();
		}

		private void ConnectPhoton()
		{
			if (string.IsNullOrEmpty(_gameDataProvider.AppDataProvider.ConnectionRegion.Value))
			{
				_gameDataProvider.AppDataProvider.ConnectionRegion.Value = GameConstants.Network.DEFAULT_REGION;
			}

			var settings = QuantumRunnerConfigs.PhotonServerSettings.AppSettings;
			settings.FixedRegion = _gameDataProvider.AppDataProvider.ConnectionRegion.Value;
			
			UpdateQuantumClientProperties();
			_networkService.QuantumClient.ConnectUsingSettings(settings, _gameDataProvider.AppDataProvider.DisplayNameTrimmed);
		}

		private void ConnectPhotonToRegionMaster()
		{
			_networkService.QuantumClient.ConnectToRegionMaster(_gameDataProvider.AppDataProvider.ConnectionRegion.Value);
		}
		
		private void ReconnectPhoton()
		{
			if (_networkService.QuantumClient.LoadBalancingPeer.PeerState == PeerStateValue.Connecting) return;
			
			if (CurrentSceneIsMatch())
			{
				_networkService.IsJoiningNewMatch.Value = false;

				if (_networkService.LastDisconnectLocation.Value == LastDisconnectionLocation.Matchmaking)
				{
					_networkService.IsJoiningNewMatch.Value = true;
					SetSpectatePlayerProperty(false);
					
					// TTL during matchmaking is 0 - we must connect to room manually again by name
					// Rejoining room is handled OnMasterConnected
					_requiresManualRoomReconnection = true;
					_networkService.QuantumClient.ReconnectToMaster();
				}
				else if(_services.NetworkService.LastMatchPlayers.Count == 1)
				{
					// We don't want to reconnect back to solo rooms - they don't work currently for resyncs
					_networkService.QuantumClient.ReconnectToMaster();
				}
				else
				{
					_networkService.QuantumClient.ReconnectAndRejoin();
				}
			}
			else
			{
				_networkService.QuantumClient.ReconnectToMaster();
			}
		}

		private void DisconnectPhoton()
		{
			_networkService.QuantumClient.Disconnect();
		}

		private void ConnectToNameServer()
		{
			var success = _networkService.QuantumClient.ConnectToNameServer();

			if (!success)
			{
				_statechartTrigger(ConnectToNameServerFailEvent);
			}
		}

		private bool IsPhotonConnectedAndReady()
		{
			return _networkService.QuantumClient.IsConnectedAndReady;
		}

		private void LeaveRoom()
		{
			if (_networkService.QuantumClient.InRoom)
			{
				_networkService.QuantumClient.OpLeaveRoom(false, true);
			}
		}

		private void UpdateQuantumClientProperties()
		{
			_networkService.QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			_networkService.QuantumClient.EnableProtocolFallback = true;
			_networkService.QuantumClient.NickName = _gameDataProvider.AppDataProvider.DisplayNameTrimmed;

			var preloadIds = new List<int>();

			foreach (var item in _gameDataProvider.EquipmentDataProvider.Loadout)
			{
				var equipmentDataInfo = _gameDataProvider.EquipmentDataProvider.Inventory[item.Value];
				preloadIds.Add((int) equipmentDataInfo.GameId);
			}

			preloadIds.Add((int) _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin);

			var playerProps = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_PRELOAD_IDS, preloadIds.ToArray()},
				{GameConstants.Network.PLAYER_PROPS_CORE_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_ALL_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_SPECTATOR, false}
			};

			_networkService.QuantumClient.LocalPlayer.SetCustomProperties(playerProps);
		}
	}
}
