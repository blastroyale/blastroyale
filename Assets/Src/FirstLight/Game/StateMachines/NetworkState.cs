using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
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
		public static readonly IStatechartEvent ConnectedEvent = new StatechartEvent("Connected to Quantum Event");
		public static readonly IStatechartEvent DisconnectedEvent = new StatechartEvent("Disconnected Quantum Event");
		public static readonly IStatechartEvent ReconnectEvent = new StatechartEvent("Reconnecting to Quantum Event");
		
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public NetworkState(IGameDataProvider dataProvider, IGameServices services, IGameBackendNetworkService networkService, 
		                    Action<IStatechartEvent> statechartTrigger)
		{
			_dataProvider = dataProvider;
			_services = services;
			_networkService = networkService;
			_statechartTrigger = statechartTrigger;
			
			_networkService.QuantumClient.AddCallbackTarget(this);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var roomEntry = stateFactory.State("Room Entry");
			var reconnecting = stateFactory.State("Reconnecting");
			var connected = stateFactory.State("Connected");
			var disconnected = stateFactory.State("Disconnected");
			var disconnecting = stateFactory.State("Disconnecting");

			initial.Transition().Target(roomEntry);
			initial.OnExit(SubscribeEvents);
			
			roomEntry.OnEnter(ConnectToPhoton);
			roomEntry.Event(ConnectedEvent).Target(connected);
			roomEntry.Event(DisconnectedEvent).Target(final);

			connected.Event(DisconnectedEvent).Target(final);
			connected.Event(MatchState.MatchEndedEvent).Target(disconnecting);
			
			reconnecting.Event(DisconnectedEvent).Target(disconnected);
			reconnecting.Event(ConnectedEvent).Target(connected);
			
			disconnected.Event(ReconnectEvent).OnTransition(Reconnect).Target(reconnecting);
			
			disconnecting.OnEnter(DisconnectQuantum);
			disconnecting.Event(DisconnectedEvent).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		/// <inheritdoc />
		public void OnConnected()
		{
			Debug.Log("OnConnected");
			
			_services.MessageBrokerService.Publish(new MatchConnectedMessage());
		}
		
		/// <inheritdoc />
		public void OnConnectedToMaster()
		{
			Debug.Log("OnConnectedToMaster");
			
			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var selectedRoomEntryType = _dataProvider.AppDataProvider.SelectedRoomEntryType.Value;
			var selectedRoomName = _dataProvider.AppDataProvider.SelectedRoomName.Value;
			var mapConfig = _dataProvider.AppDataProvider.CurrentMapConfig;
			var enterParams = config.GetEnterRoomParams(mapConfig, selectedRoomName, selectedRoomEntryType == RoomEntryID.Matchmaking);
			var joinRandomParams = config.GetJoinRandomRoomParams(mapConfig);

			switch (selectedRoomEntryType)
			{
				case RoomEntryID.Matchmaking:
					_networkService.QuantumClient.OpJoinRandomOrCreateRoom(joinRandomParams, enterParams);
					break;
				
				case RoomEntryID.JoinRoom:
					_networkService.QuantumClient.OpJoinRoom(enterParams);
					break;
				
				case RoomEntryID.CreateRoom:
					_networkService.QuantumClient.OpCreateRoom(enterParams);
					break;
				
				case RoomEntryID.JoinOrCreateRoom:
					_networkService.QuantumClient.OpJoinOrCreateRoom(enterParams);
					break;
				
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <inheritdoc />
		public void OnDisconnected(DisconnectCause cause)
		{
			_statechartTrigger(DisconnectedEvent);
			
			Debug.Log("OnDisconnected " + cause);
			
			_services.MessageBrokerService.Publish(new MatchDisconnectedMessage { Cause = cause });
		}

		/// <inheritdoc />
		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			Debug.Log("OnRegionListReceived " + regionHandler.GetResults());
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
			Debug.Log("OnCustomAuthenticationResponse " + data.Count);
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationFailed(string debugMessage)
		{
			Debug.Log("OnCustomAuthenticationResponse " + debugMessage);
		}

		/// <inheritdoc />
		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
			Debug.Log("OnFriendListUpdate " + friendList.Count);
		}

		/// <inheritdoc />
		public void OnCreatedRoom()
		{
			Debug.Log("OnCreatedRoom");
		}

		/// <inheritdoc />
		public void OnCreateRoomFailed(short returnCode, string message)
		{
			_statechartTrigger(DisconnectedEvent);
			
			Debug.Log($"OnCreateRoomFailed: {returnCode.ToString()} - {message}");
		}

		/// <inheritdoc />
		public void OnJoinedRoom()
		{
			Debug.Log("OnJoinedRoom");
			
			_services.MessageBrokerService.Publish(new MatchJoinedRoomMessage());

			if (!_networkService.QuantumClient.CurrentRoom.IsOpen)
			{
				_statechartTrigger(ConnectedEvent);
				return;
			}

			StartLockRoomTimer();
		}

		/// <inheritdoc />
		public void OnJoinRoomFailed(short returnCode, string message)
		{
			Debug.Log($"OnJoinRoomFailed: {returnCode.ToString()} - {message}");
			
			// In case the player cannot rejoin anymore
			if (returnCode == ErrorCode.OperationNotAllowedInCurrentState)
			{
				_statechartTrigger(DisconnectedEvent);
				return;
			}

			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var selectedRoomEntryType = _dataProvider.AppDataProvider.SelectedRoomEntryType.Value;
			var selectedRoomName = _dataProvider.AppDataProvider.SelectedRoomName.Value;
			var mapConfig = _dataProvider.AppDataProvider.CurrentMapConfig;
			var enterParams = config.GetEnterRoomParams(mapConfig, selectedRoomName, selectedRoomEntryType == RoomEntryID.Matchmaking);

			_networkService.QuantumClient.OpCreateRoom(enterParams);
		}

		/// <inheritdoc />
		public void OnJoinRandomFailed(short returnCode, string message)
		{
			OnJoinRoomFailed(returnCode, message);
		}

		/// <inheritdoc />
		public void OnLeftRoom()
		{
			Debug.Log("OnLeftRoom");
		}

		/// <inheritdoc />
		public void OnPlayerEnteredRoom(Player player)
		{
			Debug.Log($"OnPlayerEnteredRoom {player.NickName}");

			_services.MessageBrokerService.Publish(new PlayerJoinedMatchMessage{ player = player });
			
			if (_networkService.QuantumClient.CurrentRoom.PlayerCount ==
			    _networkService.QuantumClient.CurrentRoom.MaxPlayers)
			{
				LockRoom();
			}
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player player)
		{
			Debug.Log($"OnPlayerLeftRoom {player.NickName}");
			_services.MessageBrokerService.Publish(new PlayerLeftMatchMessage(){ player = player });
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			Debug.Log("OnRoomPropertiesUpdate");

			if (propertiesThatChanged.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				_statechartTrigger(ConnectedEvent);
			}
		}

		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			Debug.Log("OnPlayerPropertiesUpdate " + targetPlayer.NickName);
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			Debug.Log("OnMasterClientSwitched " + newMasterClient.NickName);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
			_services.TickService.SubscribeOnUpdate(TickQuantumServer, 0.1f, true, true);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.TickService?.UnsubscribeAll(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private void OnApplicationQuit(ApplicationQuitMessage data)
		{
			DisconnectQuantum();
		}

		private void TickQuantumServer(float deltaTime)
		{
			_networkService.QuantumClient.Service();
			// TODO: Make the lag check in the game
			//_networkService.CheckLag();
		}

		private void DisconnectQuantum()
		{
			_networkService.QuantumClient.Disconnect();
		}

		private void ConnectToPhoton()
		{
			var settings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings.AppSettings;

			_networkService.QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			_networkService.QuantumClient.NickName = _dataProvider.PlayerDataProvider.Nickname;
			_networkService.QuantumClient.EnableProtocolFallback = true;
			
			_networkService.QuantumClient.ConnectUsingSettings(settings, _dataProvider.PlayerDataProvider.Nickname);
		}

		private void Reconnect()
		{
			_networkService.QuantumClient.ReconnectAndRejoin();
		}

		private void StartLockRoomTimer()
		{
			if (_services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().IsDevMode)
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
	}
}