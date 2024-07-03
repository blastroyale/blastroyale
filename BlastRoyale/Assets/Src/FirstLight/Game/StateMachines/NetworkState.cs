using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Deterministic;
using Photon.Realtime;
using PlayFab;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using ErrorCode = Photon.Realtime.ErrorCode;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using Random = UnityEngine.Random;

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
		public static readonly IStatechartEvent RegionUpdatedEvent = new StatechartEvent("NETWORK - Connect To Region Master");
		public static readonly IStatechartEvent ConnectToNameServerFailEvent = new StatechartEvent("NETWORK - Connected To Name Fail Server Event");

		public static readonly IStatechartEvent CreateRoomFailedEvent = new StatechartEvent("NETWORK - Create Room Failed Event");
		public static readonly IStatechartEvent JoinedPlayfabMatchmaking = new StatechartEvent("NETWORK - Joined Matchmaking Event");
		public static readonly IStatechartEvent CanceledMatchmakingEvent = new StatechartEvent("NETWORK - Canceled Matchmaking Event");
		public static readonly IStatechartEvent JoinedRoomEvent = new StatechartEvent("NETWORK - Joined Room Event");
		public static readonly IStatechartEvent JoinRoomFailedEvent = new StatechartEvent("NETWORK - Join Room Fail Event");
		public static readonly IStatechartEvent AlreadyJoined = new StatechartEvent("NETWORK - Already joined");
		public static readonly IStatechartEvent GameDoesNotExists = new StatechartEvent("NETWORK - Game does not exists");
		public static readonly IStatechartEvent LeftRoomEvent = new StatechartEvent("NETWORK - Left Room Event");
		public static readonly IStatechartEvent OpenServerSelectScreenEvent = new StatechartEvent("NETWORK - Open Server Select Screen Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private Coroutine _criticalDisconnectCoroutine;
		private Coroutine _tickReconnectAttemptCoroutine;
		private bool _requiresManualRoomReconnection;

		public NetworkState(IGameDataProvider dataProvider, IGameServices services,
							IInternalGameNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = dataProvider;
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
			var connected = stateFactory.State("NETWORK - Connected");
			var waitSimulationFinished = stateFactory.TaskWait("NETWORK - Wait Simulation Finish");
			var disconnected = stateFactory.State("NETWORK - Disconnected");
			var connectionCheck = stateFactory.Choice("NETWORK - Connection Check");

			initial.Transition().Target(connectionCheck);
			initial.OnExit(SubscribeEvents);

			connectionCheck.Transition().Condition(HasValidConnection).Target(connected);
			connectionCheck.Transition().Target(disconnected);

			connected.Event(PhotonDisconnectedEvent).Target(waitSimulationFinished);

			waitSimulationFinished.WaitingFor(WaitSimulationFinish).Target(connectionCheck);

			disconnected.OnEnter(UpdateLastDisconnectLocation);
			disconnected.OnEnter(SubscribeDisconnectEvents);
			disconnected.Event(PhotonMasterConnectedEvent).Target(connected);
			disconnected.Event(JoinedRoomEvent).Target(connected);
			disconnected.Event(JoinedPlayfabMatchmaking).Target(connected);
			disconnected.OnExit(UnsubscribeDisconnectEvents);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<MatchSimulationStartedMessage>(OnSimulationStart);
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuitMessage);
			_services.MessageBrokerService.Subscribe<SimulationEndedMessage>(OnMatchSimulationEndedMessage);
			_services.MessageBrokerService.Subscribe<LocalPlayerClickedPlayMessage>(OnPlayerClickedPlay);
			_services.MessageBrokerService.Subscribe<MatchmakingCancelMessage>(OnMatchmakingCancelMessage);
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
			UnsubscribeDisconnectEvents();
			_tickReconnectAttemptCoroutine = _services.CoroutineService.StartCoroutine(TickReconnectAttempt());
			_criticalDisconnectCoroutine = _services.CoroutineService.StartCoroutine(CriticalDisconnectCoroutine());
		}

		private async UniTask WaitSimulationFinish()
		{
			FLog.Verbose("Waiting for simulation to finish");
			while (QuantumRunner.Default.IsDefinedAndRunning(false))
				await UniTask.Delay(5);
			FLog.Verbose("Simulation ended, advancing network action");
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

		private void UpdateLastDisconnectLocation()
		{
			// Only update DC location for main menu - match disconnections are more complex, and handled specifically
			// inside of MatchState.
			if (!CurrentSceneIsMatch())
			{
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Menu;
			}
		}

		private void ReconnectPhoton()
		{
			_networkService.ReconnectPhoton(out _requiresManualRoomReconnection);
		}

		private void OnGameMatched(GameMatched match)
		{
			if (match.RoomSetup.SimulationConfig.MapId == (int) GameId.Any)
			{
				var maps = _services.GameModeService.ValidMatchmakingMaps;
				var index = Random.Range(0, maps.Count);
				match.RoomSetup.SimulationConfig.MapId = (int) maps[index];
			}

			_services.RoomService.JoinOrCreateRoom(match.RoomSetup, match.PlayerProperties, match.ExpectedPlayers);
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

			FLog.Verbose("Using playfab matchmaking!");
			_networkService.LastUsedSetup.Value = setup;
			_services.MatchmakingService.JoinMatchmaking(setup);
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
			if (_services.GameBackendService.RunsSimulationOnServer())
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
				if (!string.IsNullOrEmpty(_services.LocalPrefsService.ServerRegion.Value))
				{
					_services.LocalPrefsService.ServerRegion.Value = null;
					FLog.Info("Invalid region, retrying");
					_statechartTrigger(PhotonInvalidServer);
					return;
				}
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

			FLog.Verbose($"Current Room Debug:{_networkService.CurrentRoom.Name}{_networkService.CurrentRoom.GetRoomDebugString()} ");

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
			_networkService.QuantumClient.RegionHandler.PingMinimumOfRegions(OnPingedRegions, "");
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
				if (_services.NetworkService.QuantumClient.Server == ServerConnection.NameServer)
				{
					FLog.Info($"Received region list while connected to name server, connecting to master");
					_services.NetworkService.ConnectPhotonServer();
				}
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
				FLog.Error($"Network action on connection state {_networkService.QuantumClient.State} on server {_networkService.QuantumClient.Server}");
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
				if (_services.RoomService.InRoom && _networkService.QuantumClient.IsConnectedAndReady)
				{
					_services.RoomService.LeaveRoom();
				}
			}
		}

		private void OnPlayerClickedPlay(LocalPlayerClickedPlayMessage msg)
		{
			FLog.Verbose("Received play ready matchmaking at network state");
			// If running the equipment/BP menu tutorial, the room is handled through the EquipmentBpTutorialState.cs
			// This is the same flow as the first match setup
			if (_services.TutorialService.IsTutorialRunning)
			{
				FLog.Verbose("Tutorial running!");
				return;
			}

			var selectedGameMode = _services.GameModeService.SelectedGameMode.Value;

			var map = _services.GameModeService.SelectedMap;
			var simulationConfig = selectedGameMode.Entry.MatchConfig.CloneSerializing();
			simulationConfig.MapId = (int) map;
			simulationConfig.MatchType = MatchType.Matchmaking;

			if (!FeatureFlags.ENABLE_NOOB)
			{
				var removeNoobOverwrites = Enum.GetValues(typeof(DropPlace)).Cast<DropPlace>()
					.Select(place => new MetaItemDropOverwrite()
					{
						Place = place,
						Id = GameId.NOOB,
						DropRate = FP._0
					});

				simulationConfig.MetaItemDropOverwrites = simulationConfig.MetaItemDropOverwrites.Where(drop => drop.Id != GameId.NOOB)
					.Concat(removeNoobOverwrites)
					.ToArray();
			}

			var matchmakingSetup = new MatchRoomSetup()
			{
				SimulationConfig = simulationConfig,
				PlayfabQueue = selectedGameMode.Entry.PlayfabQueue
			};

			StartRandomMatchmaking(matchmakingSetup);
		}

		private void OnMatchmakingCancelMessage(MatchmakingCancelMessage obj)
		{
			_services.MatchmakingService.LeaveMatchmaking();
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
				SimulationConfig = new SimulationMatchConfig()
				{
					MapId = (int) msg.MapConfig.Map,
					GameModeID = gameModeId,
					MatchType = MatchType.Custom,
					Mutators = mutatorsFullList.ToArray(),
					MaxPlayersOverwrite = msg.CustomGameOptions.PlayersNumber,
					BotOverwriteDifficulty = msg.CustomGameOptions.BotDifficulty,
					TeamSize = (uint) msg.CustomGameOptions.TeamSize,
				},
				RoomIdentifier = msg.RoomName,
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

		private bool HasValidConnection()
		{
			return _networkService.QuantumClient.IsConnectedAndReady || _networkService.QuantumClient.Server == ServerConnection.NameServer;
		}

		private bool CurrentSceneIsMatch()
		{
			return SceneManager.GetActiveScene().name != GameConstants.Scenes.SCENE_MAIN_MENU;
		}

		private IEnumerator CriticalDisconnectCoroutine()
		{
			yield return new WaitForSeconds(GameConstants.Network.CRITICAL_DISCONNECT_THRESHOLD_SECONDS);
			FLog.Error("Critical disconnection");
			_statechartTrigger(PhotonCriticalDisconnectedEvent);
		}

		public void OnErrorInfo(ErrorInfo errorInfo)
		{
			FLog.Verbose("On Error Info " + errorInfo.Info);
		}
	}
}