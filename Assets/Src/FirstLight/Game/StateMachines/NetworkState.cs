using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Realtime;
using PlayFab;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Network State and communication with Quantum servers in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class NetworkState : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, ILobbyCallbacks, IOnEventCallback
	{
		public static readonly IStatechartEvent PhotonMasterConnectedEvent = new StatechartEvent("NETWORK - Photon Master Connected Event");
		public static readonly IStatechartEvent PhotonDisconnectedEvent = new StatechartEvent("NETWORK - Photon Disconnected Event");
		public static readonly IStatechartEvent PhotonCriticalDisconnectedEvent = new StatechartEvent("NETWORK - Photon Critical Disconnected Event");
		
		public static readonly IStatechartEvent ConnectToRegionMasterEvent = new StatechartEvent("NETWORK - Connect To Region Master");
		public static readonly IStatechartEvent ConnectToNameServerFailEvent = new StatechartEvent("NETWORK - Connected To Name Fail Server Event");
		public static readonly IStatechartEvent RegionListReceivedEvent = new StatechartEvent("NETWORK - Regions List Received");
		
		public static readonly IStatechartEvent CreateRoomFailedEvent = new StatechartEvent("NETWORK - Create Room Failed Event");
		public static readonly IStatechartEvent JoinedMatchmakingEvent = new StatechartEvent("NETWORK - Joined Matchmaking Event");
		public static readonly IStatechartEvent JoinedRoomEvent = new StatechartEvent("NETWORK - Joined Room Event");
		public static readonly IStatechartEvent JoinRoomFailedEvent = new StatechartEvent("NETWORK - Join Room Fail Event");
		public static readonly IStatechartEvent LeftRoomEvent = new StatechartEvent("NETWORK - Left Room Event");
		public static readonly IStatechartEvent RoomClosedEvent = new StatechartEvent("NETWORK - Room Closed Event");
		
		public static readonly IStatechartEvent DcScreenBackEvent = new StatechartEvent("NETWORK - Disconnected Screen Back Event");
		public static readonly IStatechartEvent OpenServerSelectScreenEvent = new StatechartEvent("NETWORK - Open Server Select Screen Event");

		public static readonly IStatechartEvent IapProcessStartedEvent = new StatechartEvent("NETWORK - IAP Started Event");
		public static readonly IStatechartEvent IapProcessFinishedEvent = new StatechartEvent("NETWORK - IAP Processed Event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _criticalDisconnectCoroutine;
		private Coroutine _matchmakingCoroutine;
		private bool _requiresManualRoomReconnection;

		public NetworkState(IGameLogic gameLogic, IGameServices services,
		                    IInternalGameNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = gameLogic;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			_networkService.QuantumClient.AddCallbackTarget(this);
		}

		/// <summary>
		/// Setups the network state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("NETWORK - Initial");
			var final = stateFactory.Final("NETWORK - Final");
			var initialConnection = stateFactory.State("NETWORK - Initial Connection");
			var connected = stateFactory.State("NETWORK - Connected");
			var matchmaking = stateFactory.State("NETWORK - Matchmaking");
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
			disconnected.Event(JoinedMatchmakingEvent).Target(connected);
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
		
		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnSimulationStart);
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuitMessage);
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnMatchSimulationStartedMessage);
			_services.MessageBrokerService.Subscribe<MatchSimulationEndedMessage>(OnMatchSimulationEndedMessage);
			_services.MessageBrokerService.Subscribe<PlayMatchmakingReadyMessage>(OnPlayMatchmakingReadyMessage);
			_services.MessageBrokerService.Subscribe<PlayMapClickedMessage>(OnPlayMapClickedMessage);
			_services.MessageBrokerService.Subscribe<PlayJoinRoomClickedMessage>(OnPlayJoinRoomClickedMessage);
			_services.MessageBrokerService.Subscribe<PlayCreateRoomClickedMessage>(OnPlayCreateRoomClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomLeaveClickedMessage>(OnRoomLeaveClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomLockClickedMessage>(OnRoomLockClickedMessage);
			_services.MessageBrokerService.Subscribe<CoreMatchAssetsLoadedMessage>(OnCoreMatchAssetsLoadedMessage);
			_services.MessageBrokerService.Subscribe<AllMatchAssetsLoadedMessage>(OnAllMatchAssetsLoadedMessage);
			_services.MessageBrokerService.Subscribe<AssetReloadRequiredMessage>(OnAssetReloadRequiredMessage);
			_services.MessageBrokerService.Subscribe<SpectatorModeToggledMessage>(OnSpectatorToggleMessage);
			_services.MessageBrokerService.Subscribe<RequestKickPlayerMessage>(OnRequestKickPlayerMessage);
			_services.MessageBrokerService.Subscribe<NetworkActionWhileDisconnectedMessage>(OnNetworkActionWhileDisconnectedMessage);
			_services.MessageBrokerService.Subscribe<AttemptManualReconnectionMessage>(OnAttemptManualReconnectionMessage);

			_services.MatchmakingService.OnGameMatched += OnGameMatched;
			_services.MatchmakingService.OnMatchmakingJoined += OnMatchmakingJoined;
		}


		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.TickService?.UnsubscribeAll(this);
			_services.MatchmakingService.OnGameMatched -= OnGameMatched;
			_services.MatchmakingService.OnMatchmakingJoined -= OnMatchmakingJoined;
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
		
		private void TickReconnectAttempt(float deltaTime)
		{
			if (!_networkService.QuantumClient.IsConnectedAndReady && NetworkUtils.IsOnline())
			{
				ReconnectPhoton();
			}
		}
		
		// This method receives all photon events, but is only used for our custom in-game events
		public void OnEvent(EventData photonEvent)
		{
			if (photonEvent.Code == (byte) QuantumCustomEvents.KickPlayer)
			{
				OnKickPlayerEventReceived((int) photonEvent.CustomData, photonEvent.Sender);
			}
		}

		private void OnKickPlayerEventReceived(int userIdToLeave, int senderIndex)
		{
			if (_networkService.LocalPlayer.ActorNumber != userIdToLeave ||
				!_networkService.InRoom || _networkService.CurrentRoom.MasterClientId != senderIndex)
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

		private void HandleIapTransition()
		{
			ReconnectPhoton();
		}

		private void ConnectPhoton()
		{
			_networkService.ConnectPhotonToMaster();
		}
		
		private void ReconnectPhoton()
		{
			_networkService.ReconnectPhoton(CurrentSceneIsMatch(), out _requiresManualRoomReconnection);
		}

		private void DisconnectPhoton()
		{
			_networkService.DisconnectPhoton();
		}
		
		private void ConnectPhotonToRegionMaster()
		{
			_networkService.ConnectPhotonToRegionMaster(_gameDataProvider.AppDataProvider.ConnectionRegion.Value);
		}

		private void ConnectToNameServer()
		{
			var success = _networkService.ConnectPhotonToNameServer();

			if (!success)
			{
				_statechartTrigger(ConnectToNameServerFailEvent);
			}
		}
		
		private void OnGameMatched(GameMatched match)
		{
			_services.NetworkService.JoinOrCreateRoom(match.RoomSetup);
			_services.GenericDialogService.CloseDialog();
		}

		private void OnMatchmakingJoined(JoinedMatchmaking match)
		{
			_statechartTrigger(JoinedMatchmakingEvent);
			_services.GenericDialogService.OpenButtonDialog("Matchmaking", "[Dev UI] Matchmaking...", false, new GenericDialogButton());
		}

		private void StartRandomMatchmaking(MatchRoomSetup setup)
		{
			if (FeatureFlags.PLAYFAB_MATCHMAKING || setup.GameMode().Teams)
			{
				_services.MatchmakingService.JoinMatchmaking(setup);
			}
			else
			{
				_networkService.JoinOrCreateRandomRoom(setup);
			}
		}

		private void JoinRoom(string roomName, bool resetLastDcLocation = true)
		{
			if (!_networkService.QuantumClient.InRoom && resetLastDcLocation)
			{
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			}
			
			_networkService.JoinRoom(roomName);
		}

		private void LockRoom()
		{
			if (_networkService.CurrentRoom != null && _networkService.CurrentRoom.IsOpen)
			{
				_networkService.SetCurrentRoomOpen(false);
			}
		}
		
		private void LeaveRoom()
		{
			FLog.Error("LEAVING ROOM CALLED");
			_networkService.LeaveRoom(false, true);
		}

		private void StartMatchmakingLockRoomTimer()
		{
			if (!_networkService.LocalPlayer.IsMasterClient ||
				!_networkService.CurrentRoom.IsMatchmakingRoom() ||
				!_networkService.CurrentRoomMatchType.HasValue) 
			{
				return;
			}

			if (_networkService.CurrentRoomMatchType.Value == MatchType.Ranked)
			{
				_matchmakingCoroutine = _services.CoroutineService.StartCoroutine(RankedMatchmakingCoroutine());
			}
			else
			{
				_matchmakingCoroutine = _services.CoroutineService.StartCoroutine(CasualMatchmakingCoroutine());
			}
		}

		private void OnSimulationStart(MatchSimulationStartedMessage message)
		{
			if(FeatureFlags.QUANTUM_CUSTOM_SERVER)
			{
				_networkService.SendPlayerToken(PlayFabSettings.staticPlayer.EntityToken);
			}
		}
		
		public void OnConnected()
		{
			FLog.Info("OnConnected");
		}

		public void OnConnectedToMaster()
		{
			FLog.Info("OnConnectedToMaster");
			
			_statechartTrigger(PhotonMasterConnectedEvent);

			// Reconnections during matchmaking screen require manual reconnection to the room, due to TTL 0
			if (_requiresManualRoomReconnection &&
			    _networkService.LastDisconnectLocation.Value == LastDisconnectionLocation.Matchmaking)
			{
				_requiresManualRoomReconnection = false;
				JoinRoom(_networkService.LastConnectedRoomName.Value.TrimRoomCommitLock(), false);
			}
		}

		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("OnDisconnected " + cause);

			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Disconnection,
			                                                   _networkService.QuantumClient.DisconnectedCause
			                                                            .ToString());

			_statechartTrigger(PhotonDisconnectedEvent);
		}

		public void OnCreatedRoom()
		{
			FLog.Info("OnCreatedRoom");
		}

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

		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");
			
			_statechartTrigger(JoinedRoomEvent);

			_networkService.LastConnectedRoomName.Value = _networkService.QuantumClient.CurrentRoom.Name;

			if (_networkService.IsJoiningNewMatch.Value)
			{
				// Switch players from player to spectator, and vice versa, if the relevant room capacity is full
				var isSpectator = (bool) _networkService.LocalPlayer.CustomProperties
					[GameConstants.Network.PLAYER_PROPS_SPECTATOR];

				if (!isSpectator && _networkService.QuantumClient.CurrentRoom.GetRealPlayerAmount() >
				    _networkService.QuantumClient.CurrentRoom.GetRealPlayerCapacity())
				{
					_networkService.SetSpectatePlayerProperty(true);
				}
				else if (isSpectator && _networkService.QuantumClient.CurrentRoom.GetSpectatorAmount() >
				         _networkService.QuantumClient.CurrentRoom.GetSpectatorCapacity())
				{
					_networkService.SetSpectatePlayerProperty(false);
				}
			}

			if (_networkService.QuantumRunnerConfigs.IsOfflineMode || _services.TutorialService.IsTutorialRunning)
			{
				LockRoom();
			}
			else if (_networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom())
			{
				StartMatchmakingLockRoomTimer();
			}
		}
		
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
		
		public void OnJoinRandomFailed(short returnCode, string message)
		{
			OnJoinRoomFailed(returnCode, message);
		}
		
		public void OnLeftRoom()
		{
			FLog.Error("OnLeftRoom");

			if (_matchmakingCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_matchmakingCoroutine);
			}

			_statechartTrigger(LeftRoomEvent);
		}
		
		public void OnPlayerEnteredRoom(Player player)
		{
			FLog.Info($"OnPlayerEnteredRoom {player.NickName}");
		}
		
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

		public void OnRoomPropertiesUpdate(Hashtable changedProps)
		{
			FLog.Info("OnRoomPropertiesUpdate");

			if (changedProps.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				_statechartTrigger(RoomClosedEvent);
			}
		}

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

		public void OnMasterClientSwitched(Player newMasterClient)
		{
			FLog.Info("OnMasterClientSwitched " + newMasterClient.NickName);
		}

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
				_services.MessageBrokerService.Publish(new PingedRegionsMessage()
				{
					RegionHandler = regionHandler
				});
			});
		}
		
		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
			FLog.Info("OnCustomAuthenticationResponse " + data.Count);
		}

		public void OnCustomAuthenticationFailed(string debugMessage)
		{
			FLog.Info("OnCustomAuthenticationResponse " + debugMessage);
		}

		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
			FLog.Info("OnFriendListUpdate " + friendList.Count);
		}
		
		public void OnJoinedLobby()
		{
			FLog.Info("OnJoinedLobby");
		}

		public void OnLeftLobby()
		{
			FLog.Info("OnLeftLobby");
		}

		public void OnRoomListUpdate(List<RoomInfo> roomList)
		{
			FLog.Info("OnRoomListUpdate");
		}

		public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
		{
			FLog.Info("OnLobbyStatisticsUpdate");
		}

		private void OnRoomLockClickedMessage(RoomLockClickedMessage message)
		{
			_networkService.QuantumClient.CurrentRoom.SetCustomProperties(new Hashtable
			{
				{ GameConstants.Network.ROOM_PROPS_BOTS, message.AddBots }
			});

			LockRoom();
		}

		private void OnSpectatorToggleMessage(SpectatorModeToggledMessage message)
		{
			_networkService.SetSpectatePlayerProperty(message.IsSpectator);
		}

		private void OnRequestKickPlayerMessage(RequestKickPlayerMessage msg)
		{
			_networkService.KickPlayer(msg.Player);
		}
		
		private void OnAttemptManualReconnectionMessage(AttemptManualReconnectionMessage obj)
		{
			ReconnectPhoton();
		}
		
		private void OnNetworkActionWhileDisconnectedMessage(NetworkActionWhileDisconnectedMessage msg)
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
			if(_networkService.LocalPlayer.IsMasterClient) 
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
			var matchmakingSetup = new MatchRoomSetup()
			{
				MapId = (int) mapConfig.Map,
				GameModeHash = gameModeId.GetHashCode(),
				Mutators = mutators,
				MatchType = _services.GameModeService.SelectedGameMode.Value.Entry.MatchType
			};
			StartRandomMatchmaking(matchmakingSetup);
		}

		private void OnPlayMapClickedMessage(PlayMapClickedMessage msg)
		{
			var selectedGameMode = _services.GameModeService.SelectedGameMode.Value;
			var gameModeId = selectedGameMode.Entry.GameModeId;
			var setup = new MatchRoomSetup()
			{
				GameModeHash = gameModeId.GetHashCode(),
				MatchType = _services.GameModeService.SelectedGameMode.Value.Entry.MatchType,
				Mutators = selectedGameMode.Entry.Mutators,
				MapId = msg.MapId
			};
			StartRandomMatchmaking(setup);
		}

		private void OnPlayCreateRoomClickedMessage(PlayCreateRoomClickedMessage msg)
		{
			// TODO - REMOVE THE GETTING OF CONFIG - DOES IT JUST WORK?
			var gameModeId = msg.GameModeConfig.Id;
			_gameDataProvider.AppDataProvider.SetLastCustomGameOptions(msg.CustomGameOptions);
			_services.DataSaver.SaveData<AppData>();
			var setup = new MatchRoomSetup()
			{
				GameModeHash = gameModeId.GetHashCode(),
				MapId = (int) msg.MapConfig.Map,
				Mutators = msg.CustomGameOptions.Mutators,
				MatchType = MatchType.Custom,
				RoomIdentifier = msg.RoomName
			};
			var offlineMatch = msg.MapConfig.IsTestMap;
			if (msg.JoinIfExists)
			{
				_services.NetworkService.JoinOrCreateRoom(setup);
			}
			else
			{
				_services.NetworkService.CreateRoom(setup, offlineMatch);
			}
		}

		private void OnPlayJoinRoomClickedMessage(PlayJoinRoomClickedMessage msg)
		{
			JoinRoom(msg.RoomName);
		}

		private void OnCoreMatchAssetsLoadedMessage(CoreMatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{
					GameConstants.Network.PLAYER_PROPS_CORE_LOADED, true
				}
			};

			_networkService.SetPlayerCustomProperties(playerPropsUpdate);
		}

		private void OnAllMatchAssetsLoadedMessage(AllMatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_ALL_LOADED, true}
			};

			_networkService.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}

		private void OnAssetReloadRequiredMessage(AssetReloadRequiredMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.Network.PLAYER_PROPS_CORE_LOADED, false},
				{GameConstants.Network.PLAYER_PROPS_ALL_LOADED, false}
			};

			_networkService.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}

		private void OnApplicationQuitMessage(ApplicationQuitMessage data)
		{
			_networkService.QuantumClient.Disconnect();
		}
		
		private bool IsPhotonConnectedAndReady()
		{
			return _networkService.QuantumClient.IsConnectedAndReady;
		}
		
		private bool CurrentSceneIsMatch()
		{
			return SceneManager.GetActiveScene().name != GameConstants.Scenes.SCENE_MAIN_MENU;
		}

		private IEnumerator CriticalDisconnectCoroutine()
		{
			yield return new WaitForSeconds(GameConstants.Network.CRITICAL_DISCONNECT_THRESHOLD_SECONDS);
			_statechartTrigger(PhotonCriticalDisconnectedEvent);
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
	}
}