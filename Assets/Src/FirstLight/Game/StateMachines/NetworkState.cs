using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Network State and communication with Quantum servers in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class NetworkState : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
	{
		public static readonly IStatechartEvent PhotonBaseConnectedEvent = new StatechartEvent("Photon Base Connected Event");
		public static readonly IStatechartEvent PhotonMasterConnectedEvent = new StatechartEvent("Photon Master Connected Event");
		public static readonly IStatechartEvent PhotonDisconnectedEvent = new StatechartEvent("Photon Disconnected Event");
		public static readonly IStatechartEvent CreatedRoomEvent = new StatechartEvent("Created Room Event Event");
		public static readonly IStatechartEvent CreateRoomFailedEvent = new StatechartEvent("Create Room Failed Event");
		public static readonly IStatechartEvent JoinedRoomEvent = new StatechartEvent("Joined Room Event");
		public static readonly IStatechartEvent JoinRoomFailedEvent = new StatechartEvent("Join Room Fail Event");
		public static readonly IStatechartEvent LeftRoomEvent = new StatechartEvent("Left Room Event");
		public static readonly IStatechartEvent PlayerJoinedRoomEvent = new StatechartEvent("PLayer Joined Room Event");
		public static readonly IStatechartEvent PlayerLeftRoomEvent = new StatechartEvent("Player Left Room Event");
		public static readonly IStatechartEvent MasterClientSwitchedEvent = new StatechartEvent("Master Client Switched Event");
		public static readonly IStatechartEvent RoomPropertiesUpdatedEvent = new StatechartEvent("Room Properties Updated Event");
		public static readonly IStatechartEvent RoomClosedEvent = new StatechartEvent("Room Closed Event");
		public static readonly IStatechartEvent PlayerPropertiesUpdatedEvent = new StatechartEvent("Player Properties Updated Event Event");
		public static readonly IStatechartEvent RegionListReceivedEvent = new StatechartEvent("Region List Received Event");
		public static readonly IStatechartEvent CustomAuthResponseEvent = new StatechartEvent("Custom Auth Response Event");
		public static readonly IStatechartEvent CustomAuthFailedEvent = new StatechartEvent("Custom Auth Failed Event");
		public static readonly IStatechartEvent FriendListUpdateEvent = new StatechartEvent("Friend List Updated Event");
		public static readonly IStatechartEvent PhotonTryReconnectEvent = new StatechartEvent("Photon Try Reconnect Event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameUiService _uiService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public NetworkState(IGameDataProvider dataProvider, IGameServices services, IGameUiService uiService,
		                    IGameBackendNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
		{
			_dataProvider = dataProvider;
			_services = services;
			_uiService = uiService;
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

			initial.Transition().Target(initialConnection);
			initial.OnExit(SubscribeEvents);

			initialConnection.OnEnter(ConnectPhoton);
			initialConnection.Event(PhotonMasterConnectedEvent).Target(connected);

			connected.Event(PhotonDisconnectedEvent).Target(disconnected);

			disconnected.OnEnter(ConnectPhoton);
			disconnected.Event(PhotonMasterConnectedEvent).Target(connected);

			final.OnEnter(UnsubscribeEvents);
		}
		
		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
			_services.TickService.SubscribeOnUpdate(TickQuantumServer, 0.1f, true, true);
			_services.MessageBrokerService.Subscribe<MatchSimulationEndedMessage>(OnMatchSimulationEndedMessage);
			_services.MessageBrokerService.Subscribe<RoomRandomClickedMessage>(OnRoomRandomClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomJoinClickedMessage>(OnRoomJoinClickedMessage);
			_services.MessageBrokerService.Subscribe<RoomCreateClickedMessage>(OnRoomCreateClicked);
			_services.MessageBrokerService.Subscribe<RoomLeaveClickedMessage>(OnRoomLeaveClickedMessage);
			_services.MessageBrokerService.Subscribe<EnteredMatchmakingStateMessage>(OnEnteredMatchmakingStateMessage);
			_services.MessageBrokerService.Subscribe<MatchAssetsLoadedMessage>(OnMatchAssetsLoaded);
			_services.MessageBrokerService.Subscribe<EquipmentAssetsLoadedMessage>(OnPlayerAssetsLoadedMessage);
			_services.MessageBrokerService.Subscribe<RoomDevClickedMessage>(OnRoomDevClicked);
		}
		
		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.TickService?.UnsubscribeAll(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private void ConnectPhoton()
		{
			var settings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings.AppSettings;
			UpdateQuantumClientProperties();
			_networkService.QuantumClient.ConnectUsingSettings(settings, _dataProvider.AppDataProvider.Nickname);
		}
		
		private void DisconnectPhoton()
		{
			_networkService.QuantumClient.Disconnect();
		}

		private void LeaveRoom()
		{
			_networkService.QuantumClient.OpLeaveRoom(false, true);
		}

		private void UpdateQuantumClientProperties()
		{
			_networkService.QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			_networkService.QuantumClient.EnableProtocolFallback = true;
			_networkService.QuantumClient.NickName = _dataProvider.AppDataProvider.Nickname;
			
			var preloadIds = new List<int>();
			
			foreach (var (key, value) in _dataProvider.EquipmentDataProvider.EquippedItems)
			{
				var equipmentDataInfo = _dataProvider.EquipmentDataProvider.GetEquipmentDataInfo(value);
				preloadIds.Add((int) equipmentDataInfo.GameId);
			}

			preloadIds.Add((int) _dataProvider.PlayerDataProvider.CurrentSkin.Value);
			
			var playerProps = new Hashtable
			{
				{GameConstants.PLAYER_PROPS_PRELOAD_IDS, preloadIds.ToArray()},
				{GameConstants.PLAYER_PROPS_LOADED_EQUIP, false}
			};
			
			_networkService.QuantumClient.LocalPlayer.SetCustomProperties(playerProps);
		}
		
		private void OnRoomLeaveClickedMessage(RoomLeaveClickedMessage msg)
		{
			LeaveRoom();
		}
		
		private void OnMatchSimulationEndedMessage(MatchSimulationEndedMessage msg)
		{
			LeaveRoom();
		}

		private void OnRoomRandomClickedMessage(RoomRandomClickedMessage msg)
		{
			StartRandomMatchmaking();	
		}
		
		private void OnRoomDevClicked(RoomDevClickedMessage msg)
		{
			CreateRoom(msg.RoomName);
		}
		
		private void OnRoomCreateClicked(RoomCreateClickedMessage msg)
		{
			CreateRoom(msg.RoomName);
		}
		
		private void OnRoomJoinClickedMessage(RoomJoinClickedMessage msg)
		{
			JoinRoom(msg.RoomName);
		}
		
		private void OnEnteredMatchmakingStateMessage(EnteredMatchmakingStateMessage msg)
		{
			StartMatchmakingLockRoomTimer();
		}
		
		private void OnMatchAssetsLoaded(MatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{ GameConstants.PLAYER_PROPS_LOADED_MATCH, true }
			};
			
			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		private void OnPlayerAssetsLoadedMessage(EquipmentAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{GameConstants.PLAYER_PROPS_LOADED_EQUIP, true}
			};
			
			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}

		/// <inheritdoc />
		public void OnConnected()
		{
			FLog.Info("OnConnected");
			_statechartTrigger(PhotonBaseConnectedEvent);
			_services.MessageBrokerService.Publish(new PhotonBaseConnectedMessage());
		}

		/// <inheritdoc />
		public void OnConnectedToMaster()
		{
			FLog.Info("OnConnectedToMaster");

			_statechartTrigger(PhotonMasterConnectedEvent);
			_services.MessageBrokerService.Publish(new PhotonMasterConnectedMessage());
		}

		private void StartRandomMatchmaking()
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var mapConfig = _dataProvider.AppDataProvider.CurrentMapConfig;
			var enterParams = config.GetEnterRoomParams(_dataProvider, mapConfig);
			var joinParams = config.GetJoinRandomRoomParams(mapConfig);
			UpdateQuantumClientProperties();
			
			_networkService.QuantumClient.OpJoinRandomOrCreateRoom(joinParams, enterParams);
		}

		private void JoinRoom(string roomName)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var mapConfig = _dataProvider.AppDataProvider.CurrentMapConfig;
			var enterParams = config.GetEnterRoomParams(_dataProvider, mapConfig, roomName);
			UpdateQuantumClientProperties();
			
			_networkService.QuantumClient.OpJoinRoom(enterParams);
		}

		private void CreateRoom(string roomName)
		{
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var mapConfig = _dataProvider.AppDataProvider.CurrentMapConfig;
			var enterParams = config.GetEnterRoomParams(_dataProvider, mapConfig, roomName);
			UpdateQuantumClientProperties();
			
			_networkService.QuantumClient.OpCreateRoom(enterParams);
		}

		/// <inheritdoc />
		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("OnDisconnected " + cause);

			_statechartTrigger(PhotonDisconnectedEvent);
			_services.MessageBrokerService.Publish(new PhotonDisconnectedMessage() {Cause = cause});
		}

		/// <inheritdoc />
		public void OnCreatedRoom()
		{
			FLog.Info("OnCreatedRoom");
			_statechartTrigger(CreatedRoomEvent);
			_services.MessageBrokerService.Publish(new CreatedRoomMessage());
		}

		/// <inheritdoc />
		public void OnCreateRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnCreateRoomFailed: {returnCode.ToString()} - {message}");

			var title = string.Format(ScriptLocalization.MainMenu.RoomErrorCreate, message);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenDialog(title, false, confirmButton);
			
			_statechartTrigger(CreateRoomFailedEvent);
			_services.MessageBrokerService.Publish(new CreateRoomFailedMessage());
		}

		/// <inheritdoc />
		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");

			_statechartTrigger(JoinedRoomEvent);
			_services.MessageBrokerService.Publish(new JoinedRoomMessage());
		}

		/// <inheritdoc />
		public void OnJoinRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnJoinRoomFailed: {returnCode.ToString()} - {message}");
			
			var title = string.Format(ScriptLocalization.MainMenu.RoomErrorJoin, message);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK, 
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenDialog(title, false, confirmButton);
			
			_statechartTrigger(JoinRoomFailedEvent);
			_services.MessageBrokerService.Publish(new JoinRoomFailedMessage() {ReturnCode = returnCode, Message = message});
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
			_statechartTrigger(LeftRoomEvent);
			_services.MessageBrokerService.Publish(new LeftRoomMessage());
		}

		/// <inheritdoc />
		public void OnPlayerEnteredRoom(Player player)
		{
			FLog.Info($"OnPlayerEnteredRoom {player.NickName}");
			_statechartTrigger(PlayerJoinedRoomEvent);
			
			_services.MessageBrokerService.Publish(new PlayerJoinedRoomMessage() {Player = player});
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player player)
		{
			FLog.Info($"OnPlayerLeftRoom {player.NickName}");
			_statechartTrigger(PlayerLeftRoomEvent);
			_services.MessageBrokerService.Publish(new PlayerLeftRoomMessage() {Player = player});
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable changedProps)
		{
			FLog.Info("OnRoomPropertiesUpdate");
			_statechartTrigger(RoomPropertiesUpdatedEvent);
			_services.MessageBrokerService.Publish(new RoomPropertiesUpdatedMessage() {ChangedProps = changedProps});
			
			if (changedProps.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				_statechartTrigger(RoomClosedEvent);
				_services.MessageBrokerService.Publish(new RoomClosedMessage());
			}
		}
		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			FLog.Info("OnPlayerPropertiesUpdate " + targetPlayer.NickName);
			_statechartTrigger(PlayerPropertiesUpdatedEvent);
			_services.MessageBrokerService.Publish(new PlayerPropertiesUpdatedMessage() {Player = targetPlayer, ChangedProps = changedProps});
			
			if (changedProps.TryGetValue(GameConstants.PLAYER_PROPS_LOADED_MATCH, out var loadedMatch) && (bool) loadedMatch)
			{
				_services.MessageBrokerService.Publish(new PlayerLoadedMatchMessage(){ Player = targetPlayer });
				CheckAllLoadedMatch();
			}
			
			if (changedProps.TryGetValue(GameConstants.PLAYER_PROPS_LOADED_EQUIP, out var loadedEquip) && (bool) loadedEquip)
			{
				_services.MessageBrokerService.Publish(new PlayerLoadedEquipmentMessage(){ Player = targetPlayer });
				CheckAllLoadedEquipment();
			}
		}
		
		private void CheckAllLoadedMatch()
		{
			// Check if all players loaded match
			foreach (var playerKvp in _services.NetworkService.QuantumClient.CurrentRoom.Players)
			{
				if (playerKvp.Value.CustomProperties.TryGetValue(GameConstants.PLAYER_PROPS_LOADED_MATCH, out var isLoaded) && (bool) isLoaded == false)
				{
					return;
				}
			}
			
			_services.MessageBrokerService.Publish(new AllPlayersLoadedMatchMessage());

		}
		private void CheckAllLoadedEquipment()
		{
			// Check if all players loaded equipment
			foreach (var playerKvp in _services.NetworkService.QuantumClient.CurrentRoom.Players)
			{
				if (playerKvp.Value.CustomProperties.TryGetValue(GameConstants.PLAYER_PROPS_LOADED_EQUIP, out var isLoaded) && (bool) isLoaded == false)
				{
					return;
				}
			}
			
			_services.MessageBrokerService.Publish(new AllPlayersLoadedEquipmentMessage());
		}
		
		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			FLog.Info("OnMasterClientSwitched " + newMasterClient.NickName);
			_statechartTrigger(MasterClientSwitchedEvent);
			_services.MessageBrokerService.Publish(new MasterClientSwichedMessage() {NewMaster = newMasterClient});
		}
		
		/// <inheritdoc />
		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			FLog.Info("OnRegionListReceived " + regionHandler.GetResults());
			_statechartTrigger(RegionListReceivedEvent);
			_services.MessageBrokerService.Publish(new RegionListReceivedMessage() {RegionHandler = regionHandler});
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
			FLog.Info("OnCustomAuthenticationResponse " + data.Count);
			_statechartTrigger(CustomAuthResponseEvent);
			_services.MessageBrokerService.Publish(new CustomAuthResponseMessage() {Data = data});
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationFailed(string debugMessage)
		{
			FLog.Info("OnCustomAuthenticationResponse " + debugMessage);
			_statechartTrigger(CustomAuthFailedEvent);
			_services.MessageBrokerService.Publish(new CustomAuthFailedMessage() {Message = debugMessage});
		}

		/// <inheritdoc />
		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
			FLog.Info("OnFriendListUpdate " + friendList.Count);
			_statechartTrigger(FriendListUpdateEvent);
			_services.MessageBrokerService.Publish(new FriendListUpdateMessage() {FriendList = friendList});
		}
		
		private void TickQuantumServer(float deltaTime)
		{
			_networkService.QuantumClient.Service();

			// TODO: Make the lag check in the game
			//_networkService.CheckLag();
		}
		
		private void StartMatchmakingLockRoomTimer()
		{
			// Return if custom match (not visible), or local player not master
			if (!_services.NetworkService.QuantumClient.CurrentRoom.IsVisible ||
			    !_services.NetworkService.QuantumClient.LocalPlayer.IsMasterClient)
			{
				return;
			}

			_services.CoroutineService.StartCoroutine(LockRoomCoroutine());

			IEnumerator LockRoomCoroutine()
			{
				yield return new WaitForSeconds(_services.ConfigsProvider.GetConfig<QuantumGameConfig>().MatchmakingTime.AsFloat);

				LockRoom();
			}
		}

		private void LockRoom()
		{
			if (_networkService.QuantumClient.CurrentRoom.IsOpen)
			{
				_networkService.QuantumClient.CurrentRoom.IsOpen = false;
			}
		}

		private void OnApplicationQuit(ApplicationQuitMessage data)
		{
			DisconnectPhoton();
		}
	}
}