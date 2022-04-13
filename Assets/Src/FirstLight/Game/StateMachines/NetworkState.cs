using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Statechart;
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

		public NetworkState(IGameDataProvider dataProvider, IGameServices services,
		                    IGameBackendNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
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
			var initialConnection = stateFactory.State("Initial Connection");
			var reconnecting = stateFactory.State("Reconnecting");
			var connected = stateFactory.State("Connected");
			var disconnected = stateFactory.State("Disconnected");
			var disconnecting = stateFactory.State("Disconnecting");

			initial.Transition().Target(initialConnection);
			initial.OnExit(SubscribeEvents);

			initialConnection.OnEnter(ConnectQuantum);
			initialConnection.Event(ConnectedEvent).Target(connected);
			initialConnection.Event(DisconnectedEvent).Target(final);

			connected.Event(DisconnectedEvent).Target(final);
			connected.Event(MatchState.MatchEndedEvent).Target(disconnecting);

			reconnecting.Event(DisconnectedEvent).Target(disconnected);
			reconnecting.Event(ConnectedEvent).Target(connected);

			disconnected.Event(ReconnectEvent).OnTransition(ReconnectQuantum).Target(reconnecting);

			disconnecting.OnEnter(DisconnectQuantum);
			disconnecting.Event(DisconnectedEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
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
		
		private void DisconnectQuantum()
		{
			_networkService.QuantumClient.Disconnect();
		}

		private void ConnectQuantum()
		{
			var settings = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().PhotonServerSettings.AppSettings;

			_networkService.QuantumClient.AuthValues.AuthType = CustomAuthenticationType.Custom;
			_networkService.QuantumClient.NickName = _dataProvider.PlayerDataProvider.Nickname;
			_networkService.QuantumClient.EnableProtocolFallback = true;

			var preloadIds = new List<int>();
			foreach (var (key, value) in _dataProvider.EquipmentDataProvider.EquippedItems)
			{
				var equipmentDataInfo = _dataProvider.EquipmentDataProvider.GetEquipmentDataInfo(value);
				preloadIds.Add((int) equipmentDataInfo.GameId);
			}

			preloadIds.Add((int) _dataProvider.PlayerDataProvider.CurrentSkin.Value);

			_networkService.QuantumClient.LocalPlayer.SetCustomProperties(new Hashtable
			{
				{"PreloadIds", preloadIds.ToArray()}
			});

			_networkService.QuantumClient.ConnectUsingSettings(settings, _dataProvider.PlayerDataProvider.Nickname);
		}

		private void ReconnectQuantum()
		{
			_networkService.QuantumClient.ReconnectAndRejoin();
		}

		/// <inheritdoc />
		public void OnConnected()
		{
			FLog.Info("OnConnected");

			_services.MessageBrokerService.Publish(new QuantumBaseConnectedMessage());
		}

		/// <inheritdoc />
		public void OnConnectedToMaster()
		{
			FLog.Info("OnConnectedToMaster");

			var config = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			var info = _dataProvider.AppDataProvider.CurrentMapConfig;
			var enterParams = config.GetEnterRoomParams(info);
			var joinParams = config.GetJoinRandomRoomParams(info);

			_networkService.QuantumClient.OpJoinRandomOrCreateRoom(joinParams, enterParams);

			_services.MessageBrokerService.Publish(new QuantumMasterConnectedMessage());
		}

		/// <inheritdoc />
		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("OnDisconnected " + cause);

			_statechartTrigger(DisconnectedEvent);
			_services.MessageBrokerService.Publish(new QuantumDisconnectedMessage() {Cause = cause});
		}

		/// <inheritdoc />
		public void OnCreatedRoom()
		{
			FLog.Info("OnCreatedRoom");
			_services.MessageBrokerService.Publish(new CreatedRoomMessage());
		}

		/// <inheritdoc />
		public void OnCreateRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnCreateRoomFailed: {returnCode.ToString()} - {message}");

			_statechartTrigger(DisconnectedEvent);
			_services.MessageBrokerService.Publish(new CreateRoomFailedMessage());
		}

		/// <inheritdoc />
		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");

			_services.MessageBrokerService.Publish(new JoinedRoomMessage());
			_statechartTrigger(ConnectedEvent);
		}

		/// <inheritdoc />
		public void OnJoinRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnJoinRoomFailed: {returnCode.ToString()} - {message}");

			// In case the player cannot rejoin anymore
			if (returnCode == ErrorCode.OperationNotAllowedInCurrentState)
			{
				_statechartTrigger(DisconnectedEvent);
				return;
			}

			var info = _dataProvider.AppDataProvider.CurrentMapConfig;
			var properties = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().GetEnterRoomParams(info);

			_networkService.QuantumClient.OpCreateRoom(properties);
			_services.MessageBrokerService.Publish(new JoinRoomFailedMessage()
				                                       {ReturnCode = returnCode, Message = message});
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
			_services.MessageBrokerService.Publish(new LeftRoomMessage());
		}

		/// <inheritdoc />
		public void OnPlayerEnteredRoom(Player player)
		{
			FLog.Info($"OnPlayerEnteredRoom {player.NickName}");
			_services.MessageBrokerService.Publish(new PlayerJoinedRoomMessage() {Player = player});
		}

		/// <inheritdoc />
		public void OnPlayerLeftRoom(Player player)
		{
			FLog.Info($"OnPlayerLeftRoom {player.NickName}");
			_services.MessageBrokerService.Publish(new PlayerLeftRoomMessage() {Player = player});
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable changedProps)
		{
			FLog.Info("OnRoomPropertiesUpdate");
			_services.MessageBrokerService.Publish(new RoomPropertiesUpdatedMessage() {ChangedProps = changedProps});
		}

		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			FLog.Info("OnPlayerPropertiesUpdate " + targetPlayer.NickName);
			_services.MessageBrokerService.Publish(new PlayerPropertiesUpdatedMessage()
				                                       {Player = targetPlayer, ChangedProps = changedProps});
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			FLog.Info("OnMasterClientSwitched " + newMasterClient.NickName);
			_services.MessageBrokerService.Publish(new MasterClientSwichedMessage() {NewMaster = newMasterClient});
		}
		
		/// <inheritdoc />
		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			FLog.Info("OnRegionListReceived " + regionHandler.GetResults());
			_services.MessageBrokerService.Publish(new RegionListReceivedMessage() {RegionHandler = regionHandler});
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
		{
			FLog.Info("OnCustomAuthenticationResponse " + data.Count);
			_services.MessageBrokerService.Publish(new CustomAuthResponseMessage() {Data = data});
		}

		/// <inheritdoc />
		public void OnCustomAuthenticationFailed(string debugMessage)
		{
			FLog.Info("OnCustomAuthenticationResponse " + debugMessage);
			_services.MessageBrokerService.Publish(new CustomAuthFailedMessage() {Message = debugMessage});
		}

		/// <inheritdoc />
		public void OnFriendListUpdate(List<FriendInfo> friendList)
		{
			FLog.Info("OnFriendListUpdate " + friendList.Count);
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
			if (_services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().IsDevMode)
			{
				return;
			}

			_services.CoroutineService.StartCoroutine(LockRoomCoroutine());

			IEnumerator LockRoomCoroutine()
			{
				yield return new WaitForSeconds(_services.ConfigsProvider.GetConfig<QuantumGameConfig>().MatchmakingTime
				                                         .AsFloat);

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
			DisconnectQuantum();
		}
	}
}