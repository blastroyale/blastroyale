using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ExitGames.Client.Photon;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Deterministic;
using Photon.Realtime;
using PlayFab;
using UnityEngine;
using UnityEngine.SceneManagement;
using ErrorCode = Photon.Realtime.ErrorCode;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Network State and communication with Quantum servers in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class NetworkState : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, ILobbyCallbacks, IErrorInfoCallback
	{
		public static readonly IStatechartEvent PhotonMasterConnectedEvent = new StatechartEvent("NETWORK - Photon Master Connected Event");
		public static readonly IStatechartEvent PhotonInvalidServer = new StatechartEvent("NETWORK - Photon Invalid Server Event");
		public static readonly IStatechartEvent PhotonDisconnectedEvent = new StatechartEvent("NETWORK - Photon Disconnected Event");
		
		public static readonly IStatechartEvent PhotonCriticalDisconnectedEvent = new StatechartEvent("NETWORK - Photon Critical Disconnected Event");
		public static readonly IStatechartEvent ConnectToRegionMasterEvent = new StatechartEvent("NETWORK - Connect To Region Master");
		public static readonly IStatechartEvent ConnectToNameServerFailEvent = new StatechartEvent("NETWORK - Connected To Name Fail Server Event");
		public static readonly IStatechartEvent RegionListReceivedEvent = new StatechartEvent("NETWORK - Regions List Received");
		public static readonly IStatechartEvent RegionListPinged = new StatechartEvent("NETWORK - Regions List Pinged");

		public static readonly IStatechartEvent CreateRoomFailedEvent = new StatechartEvent("NETWORK - Create Room Failed Event");
		public static readonly IStatechartEvent JoinedPlayfabMatchmaking = new StatechartEvent("NETWORK - Joined Matchmaking Event");
		public static readonly IStatechartEvent CanceledMatchmakingEvent = new StatechartEvent("NETWORK - Canceled Matchmaking Event");
		public static readonly IStatechartEvent JoinedRoomEvent = new StatechartEvent("NETWORK - Joined Room Event");
		public static readonly IStatechartEvent JoinRoomFailedEvent = new StatechartEvent("NETWORK - Join Room Fail Event");
		public static readonly IStatechartEvent AlreadyJoined = new StatechartEvent("NETWORK - Already joined");
		public static readonly IStatechartEvent GameDoesNotExists = new StatechartEvent("NETWORK - Game does not exists");
		public static readonly IStatechartEvent LeftRoomEvent = new StatechartEvent("NETWORK - Left Room Event");
		public static readonly IStatechartEvent DcScreenBackEvent = new StatechartEvent("NETWORK - Disconnected Screen Back Event");
		public static readonly IStatechartEvent OpenServerSelectScreenEvent = new StatechartEvent("NETWORK - Open Server Select Screen Event");

		public static readonly IStatechartEvent IapProcessStartedEvent = new StatechartEvent("NETWORK - IAP Started Event");
		public static readonly IStatechartEvent IapProcessFinishedEvent = new StatechartEvent("NETWORK - IAP Processed Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _criticalDisconnectCoroutine;
		private Coroutine _tickReconnectAttemptCoroutine;
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
			var waitSimulationFinished = stateFactory.TaskWait("NETWORK - Wait Simulation Finish");
			var disconnected = stateFactory.State("NETWORK - Disconnected");
			var disconnectForServerSelect = stateFactory.State("NETWORK - Disconnect Photon For Name Server");
			var getAvailableRegions = stateFactory.State("NETWORK - Server Select Screen");
			var connectedToNameServer = stateFactory.State("NETWORK - Connected To Name Server");
			var connectToRegionMaster = stateFactory.State("NETWORK - Connect To Region Master");
			var connectionCheck = stateFactory.Choice("NETWORK - Connection Check");
			var iapProcessing = stateFactory.State("NETWORK - IAP Processing");
			var invalidServer = stateFactory.Transition("NETWORK - InvalidServer");

			initial.Transition().Target(initialConnection);
			initial.OnExit(SubscribeEvents);

			initialConnection.OnEnter(ConnectPhoton);
			initialConnection.Event(RegionListPinged).Target(connectToRegionMaster);
			initialConnection.Event(PhotonMasterConnectedEvent).Target(connected);
			initialConnection.Event(PhotonInvalidServer).Target(invalidServer);;
			
			invalidServer.OnEnter(ClearServerData);
			invalidServer.Transition().Target(initialConnection);
			
			iapProcessing.Event(IapProcessFinishedEvent).OnTransition(HandleIapTransition).Target(connected);

			connectionCheck.Transition().Condition(IsPhotonConnectedAndReady).Target(connected);
			connectionCheck.Transition().Target(disconnected); // TODO: Send to reconnection state instead

			connected.Event(PhotonDisconnectedEvent).Target(waitSimulationFinished);
			connected.Event(OpenServerSelectScreenEvent).Target(disconnectForServerSelect);

			waitSimulationFinished.WaitingFor(WaitSimulationFinish).Target(connectionCheck);
			
			disconnected.OnEnter(UpdateLastDisconnectLocation);
			disconnected.OnEnter(SubscribeDisconnectEvents);
			disconnected.Event(PhotonMasterConnectedEvent).Target(connected);
			disconnected.Event(JoinedRoomEvent).Target(connected);
			disconnected.Event(JoinedPlayfabMatchmaking).Target(connected);
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

		private void ClearServerData()
		{
			_gameDataProvider.AppDataProvider.ConnectionRegion.Value = null;
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnSimulationStart);
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuitMessage);
			_services.MessageBrokerService.Subscribe<SimulationEndedMessage>(OnMatchSimulationEndedMessage);
			_services.MessageBrokerService.Subscribe<PlayMatchmakingReadyMessage>(OnPlayMatchmakingReadyMessage);
			_services.MessageBrokerService.Subscribe<MatchmakingCancelMessage>(OnMatchmakingCancelMessage);
			_services.MessageBrokerService.Subscribe<PlayMapClickedMessage>(OnPlayMapClickedMessage);
			_services.MessageBrokerService.Subscribe<PlayJoinRoomClickedMessage>(OnPlayJoinRoomClickedMessage);
			_services.MessageBrokerService.Subscribe<PlayCreateRoomClickedMessage>(OnPlayCreateRoomClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomLeaveClickedMessage>(OnRoomLeaveClickedMessage);
			_services.MessageBrokerService.Subscribe<NetworkActionWhileDisconnectedMessage>(OnNetworkActionWhileDisconnectedMessage);
			_services.MessageBrokerService.Subscribe<AttemptManualReconnectionMessage>(OnAttemptManualReconnectionMessage);
			_services.MatchmakingService.OnGameMatched += OnGameMatched;
			_services.MatchmakingService.OnMatchmakingJoined += OnMatchmakingJoined;
			_services.MatchmakingService.OnMatchmakingCancelled += OnMatchmakingCancelled;
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services.MatchmakingService.OnGameMatched -= OnGameMatched;
			_services.MatchmakingService.OnMatchmakingJoined -= OnMatchmakingJoined;
			_services.MatchmakingService.OnMatchmakingCancelled -= OnMatchmakingCancelled;
		}

		private void SubscribeDisconnectEvents()
		{
			_tickReconnectAttemptCoroutine = _services.CoroutineService.StartCoroutine(TickReconnectAttempt());
			_criticalDisconnectCoroutine = _services.CoroutineService.StartCoroutine(CriticalDisconnectCoroutine());
		}
		
		private async Task WaitSimulationFinish()
		{
			while(QuantumRunner.Default != null && QuantumRunner.Default.IsRunning)
				await Task.Delay(1);
		}
		
		private void UnsubscribeDisconnectEvents()
		{
			if (_tickReconnectAttemptCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_tickReconnectAttemptCoroutine);
				_tickReconnectAttemptCoroutine = null;
			}

			if (_criticalDisconnectCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_criticalDisconnectCoroutine);
				_criticalDisconnectCoroutine = null;
			}
		}

		private IEnumerator TickReconnectAttempt()
		{
			var waitForSeconds = new WaitForSeconds(GameConstants.Network.NETWORK_ATTEMPT_RECONNECT_SECONDS);

			while (true)
			{
				if (!_networkService.QuantumClient.IsConnectedAndReady && NetworkUtils.IsOnline())
				{
					ReconnectPhoton();
				}

				yield return waitForSeconds;
			}
		}

		[Conditional("LOG_LEVEL_VERBOSE")]
		private void DebugEvent(EventData photonEvent)
		{
			FLog.Verbose("Photon Event Received:"+photonEvent.Code);
			foreach (var k in photonEvent.Parameters)
			{
				FLog.Verbose("Parameter: "+k.Key+" = "+k.Value);
			}
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
			_networkService.ConnectPhotonServer();
		}

		private void ReconnectPhoton()
		{
			_networkService.ReconnectPhoton(out _requiresManualRoomReconnection);
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
			_services.RoomService.JoinOrCreateRoom(match.RoomSetup, match.TeamId, match.ExpectedPlayers);
			_services.GenericDialogService.CloseDialog();
		}

		private void OnMatchmakingJoined(JoinedMatchmaking match)
		{
			_networkService.JoinSource.Value = JoinRoomSource.FirstJoin;
			_networkService.LastUsedSetup.Value = match.RoomSetup;
			_statechartTrigger(JoinedPlayfabMatchmaking);
		}

		private void OnMatchmakingCancelled()
		{
			_statechartTrigger(CanceledMatchmakingEvent);
		}
		
		private void StartRandomMatchmaking(MatchRoomSetup setup)
		{			
			_gameDataProvider.AppDataProvider.LastFrameSnapshot.Value = default;
			_networkService.JoinSource.Value = JoinRoomSource.FirstJoin;

			var config = _services.RoomService.GetGameModeConfig(setup.GameModeId);
			if (config.ShouldUsePlayfabMatchmaking())
			{
				_networkService.LastUsedSetup.Value = setup;
				_services.MatchmakingService.JoinMatchmaking(setup);
			}
			else
			{
				_services.RoomService.JoinOrCreateRandomRoom(setup);
			}
		}

		private void JoinRoom(string roomName, bool resetLastDcLocation = true)
		{
			if (!_networkService.QuantumClient.InRoom && resetLastDcLocation)
			{
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
			}
			_services.RoomService.JoinRoom(roomName);
		}

		private void LockRoom()
		{
			if (_networkService.CurrentRoom != null && _networkService.CurrentRoom.IsOpen)
			{
				FLog.Info($"RoomDebugString: {_networkService.CurrentRoom.GetRoomDebugString()}");
				_networkService.SetCurrentRoomOpen(false);
			}
		}
        

		private void OnSimulationStart(MatchSimulationStartedMessage message)
		{
			if (FeatureFlags.QUANTUM_CUSTOM_SERVER)
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
				FLog.Verbose("Manual reconnection - re-joining room");
				_services.RoomService.RejoinRoom(_networkService.LastConnectedRoom.Value.Name);
			}
		}

		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("OnDisconnected " + cause);

			if (cause == DisconnectCause.InvalidRegion || cause == DisconnectCause.InvalidAuthentication)
			{
				if (!string.IsNullOrEmpty(_gameDataProvider.AppDataProvider.ConnectionRegion.Value))
				{
					FLog.Info("Invalid region, retrying");
					_statechartTrigger(PhotonInvalidServer);
					return;
				}
			}
			
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Disconnection,
				_networkService.QuantumClient.DisconnectedCause
					.ToString());

			if (QuantumRunner.Default != null && QuantumRunner.Default.Session.GameMode != DeterministicGameMode.Local)
			{
				FLog.Verbose("Disabling Simulation Updates");
				QuantumRunner.Default.OverrideUpdateSession = true;
			}
			_statechartTrigger(PhotonDisconnectedEvent);
		}

		public void OnCreatedRoom()
		{
			FLog.Info("OnCreatedRoom");
		}

		public void OnCreateRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnCreateRoomFailed: {returnCode.ToString()} - {message}");

			if (!_networkService.JoinSource.Value.IsSnapshotAutoConnect())
			{
				var desc = string.Format(ScriptLocalization.MainMenu.RoomError, message);
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};
				_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);
			}
			
			_statechartTrigger(CreateRoomFailedEvent);
		}

		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");
			
			FLog.Info($"Current Room Debug:{_networkService.CurrentRoom.Name}{_networkService.CurrentRoom.GetRoomDebugString()} ");
			
			_services.PartyService.ForceRefresh(); // TODO: This should be in a "OnReconnected" callback

			_networkService.SetLastRoom();

			var room = _services.RoomService.CurrentRoom;
			if (_networkService.JoinSource.Value == JoinRoomSource.FirstJoin)
			{
				var isSpectator = _services.RoomService.IsLocalPlayerSpectator;

				if (!isSpectator && _services.RoomService.CurrentRoom.GetRealPlayerAmount() >
					_services.RoomService.CurrentRoom.GetRealPlayerCapacity())
				{
					room.LocalPlayerProperties.Spectator.Value = true;
				}
				else if (isSpectator && room.GetSpectatorAmount() >
						room.GetMaxSpectators())
				{
					room.LocalPlayerProperties.Spectator.Value = false;
				}
				
				if (_networkService.QuantumRunnerConfigs.IsOfflineMode ||
					_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH)
				{
					LockRoom();
				}
			}
			
			_statechartTrigger(JoinedRoomEvent);
		}

		public void OnJoinRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnJoinRoomFailed: {returnCode.ToString()} - {message}");

			if (returnCode == ErrorCode.GameDoesNotExist)
			{
				_statechartTrigger(GameDoesNotExists);
			}

			if (_networkService.JoinSource.Value.IsSnapshotAutoConnect())
			{
				if (returnCode == ErrorCode.JoinFailedPeerAlreadyJoined)
				{
					FLog.Verbose("Player already inactive in the room, rejoining");
					_statechartTrigger(AlreadyJoined);
					var lastSnapshot = _gameDataProvider.AppDataProvider.LastFrameSnapshot.Value;
					_services.RoomService.RejoinRoom(lastSnapshot.RoomName);
					return;
				} 
			}
			else
			{
				var desc = string.Format(ScriptLocalization.MainMenu.RoomError, message);
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};
				_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);
			}

			_statechartTrigger(JoinRoomFailedEvent);
		}

		public void OnJoinRandomFailed(short returnCode, string message)
		{
			OnJoinRoomFailed(returnCode, message);
		}

		public void OnLeftRoom()
		{
			FLog.Info("OnLeftRoom");
			_statechartTrigger(LeftRoomEvent);
		}

		public void OnPlayerEnteredRoom(Player player)
		{
			
		}

		public void OnPlayerLeftRoom(Player player)
		{
			
		}

		public void OnRoomPropertiesUpdate(Hashtable changedProps)
		{
		
		}

		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			
		}

		public void OnMasterClientSwitched(Player newMasterClient)
		{
			
			
		}

		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			FLog.Verbose("OnRegionListReceived " + regionHandler.GetResults());
			_services.MessageBrokerService.Publish(new RegionListReceivedMessage() {RegionHandler = regionHandler});
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
				_statechartTrigger(RegionListPinged);
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
        
        
		private void OnAttemptManualReconnectionMessage(AttemptManualReconnectionMessage obj)
		{
			ReconnectPhoton();
		}

		private void OnNetworkActionWhileDisconnectedMessage(NetworkActionWhileDisconnectedMessage msg)
		{
			if (!NetworkUtils.IsOnline() || !_networkService.QuantumClient.IsConnected)
			{
				FLog.Warn($"Network action on connection state {_networkService.QuantumClient.State} on server {_networkService.QuantumClient.Server}");
				_statechartTrigger(PhotonCriticalDisconnectedEvent);
			}
		}

		private void OnRoomLeaveClickedMessage(RoomLeaveClickedMessage msg)
		{
			if (MainInstaller.TryResolve<IMatchServices>(out var s))
			{
				s.FrameSnapshotService.ClearFrameSnapshot();
			}
			_services.RoomService.LeaveRoom();
		}

		private void OnMatchSimulationEndedMessage(SimulationEndedMessage msg)
		{
			if (msg.Reason != SimulationEndReason.Disconnected)
			{
				FLog.Verbose("Simulation endeed abruptly, leaving room");
				_services.RoomService.LeaveRoom();
			}
		}
        
		private void OnPlayMatchmakingReadyMessage(PlayMatchmakingReadyMessage msg)
		{
			// If running the equipment/BP menu tutorial, the room is handled through the EquipmentBpTutorialState.cs
			// This is the same flow as the first match setup
			if (_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.META_GUIDE_AND_MATCH)
			{
				return;
			}

			var selectedGameMode = _services.GameModeService.SelectedGameMode.Value;
			var gameModeId = selectedGameMode.Entry.GameModeId;
			var mutators = selectedGameMode.Entry.Mutators;
			var mapConfig = _services.GameModeService.GetRotationMapConfig(gameModeId);

			var matchmakingSetup = new MatchRoomSetup()
			{
				MapId = (int) mapConfig.Map,
				GameModeId = gameModeId,
				Mutators = mutators,
				MatchType = _services.GameModeService.SelectedGameMode.Value.Entry.MatchType,
				AllowedRewards = selectedGameMode.Entry.AllowedRewards
			};

			StartRandomMatchmaking(matchmakingSetup);
		}

		private void OnMatchmakingCancelMessage(MatchmakingCancelMessage obj)
		{
			_services.MatchmakingService.LeaveMatchmaking();
		}

		private void OnPlayMapClickedMessage(PlayMapClickedMessage msg)
		{
			var selectedGameMode = _services.GameModeService.SelectedGameMode.Value;
			var gameModeId = selectedGameMode.Entry.GameModeId;
			var setup = new MatchRoomSetup()
			{
				GameModeId = gameModeId,
				MatchType = _services.GameModeService.SelectedGameMode.Value.Entry.MatchType,
				Mutators = selectedGameMode.Entry.Mutators,
				MapId = msg.MapId,
			};
			StartRandomMatchmaking(setup);
		}
		
		

		private void OnPlayCreateRoomClickedMessage(PlayCreateRoomClickedMessage msg)
		{
			var gameModeId = msg.GameModeConfig.Id;
			_networkService.JoinSource.Value = JoinRoomSource.FirstJoin;
			_gameDataProvider.AppDataProvider.SetLastCustomGameOptions(msg.CustomGameOptions);
			_services.DataSaver.SaveData<AppData>();
			var mutatorsFullList = msg.CustomGameOptions.Mutators;
			if (msg.CustomGameOptions.WeaponLimiter != ScriptLocalization.MainMenu.None)
			{
				mutatorsFullList.Add(msg.CustomGameOptions.WeaponLimiter);
			}
			var setup = new MatchRoomSetup()
			{
				GameModeId = gameModeId,
				MapId = (int) msg.MapConfig.Map,
				Mutators = mutatorsFullList,
				MatchType = MatchType.Custom,
				RoomIdentifier = msg.RoomName,
				BotDifficultyOverwrite = msg.CustomGameOptions.BotDifficulty,
			};
			var offlineMatch = msg.MapConfig.IsTestMap;
			if (msg.JoinIfExists)
			{
				_services.RoomService.JoinOrCreateRoom(setup);
			}
			else
			{
				_services.RoomService.CreateRoom(setup, offlineMatch);
			}
		}

		private void OnPlayJoinRoomClickedMessage(PlayJoinRoomClickedMessage msg)
		{
			_networkService.JoinSource.Value = JoinRoomSource.FirstJoin;
			JoinRoom(msg.RoomName);
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
			FLog.Verbose("Critical disconnection");
			_statechartTrigger(PhotonCriticalDisconnectedEvent);
		}
        
		public void OnErrorInfo(ErrorInfo errorInfo)
		{
			FLog.Verbose("On Error Info "+errorInfo.Info);
		}
	}
}