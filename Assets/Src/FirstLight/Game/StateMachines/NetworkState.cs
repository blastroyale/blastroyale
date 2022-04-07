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
			var matchmaking = stateFactory.State("Matchmaking");
			var reconnecting = stateFactory.State("Reconnecting");
			var connected = stateFactory.State("Connected");
			var disconnected = stateFactory.State("Disconnected");
			var disconnecting = stateFactory.State("Disconnecting");

			initial.Transition().Target(matchmaking);
			initial.OnExit(SubscribeEvents);

			matchmaking.OnEnter(StartMatchmaking);
			matchmaking.Event(ConnectedEvent).Target(connected);
			matchmaking.Event(DisconnectedEvent).Target(final);

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
			FLog.Info("OnConnected");

			_services.MessageBrokerService.Publish(new MatchConnectedMessage());
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
		}

		/// <inheritdoc />
		public void OnDisconnected(DisconnectCause cause)
		{
			_statechartTrigger(DisconnectedEvent);

			FLog.Info("OnDisconnected " + cause);

			_services.MessageBrokerService.Publish(new MatchDisconnectedMessage {Cause = cause});
		}

		/// <inheritdoc />
		public void OnRegionListReceived(RegionHandler regionHandler)
		{
			FLog.Info("OnRegionListReceived " + regionHandler.GetResults());
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

		/// <inheritdoc />
		public void OnCreatedRoom()
		{
			FLog.Info("OnCreatedRoom");
		}

		/// <inheritdoc />
		public void OnCreateRoomFailed(short returnCode, string message)
		{
			_statechartTrigger(DisconnectedEvent);

			FLog.Info($"OnCreateRoomFailed: {returnCode.ToString()} - {message}");
		}

		/// <inheritdoc />
		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");

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
		}

		/// <inheritdoc />
		public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
		{
			FLog.Info("OnRoomPropertiesUpdate");

			if (propertiesThatChanged.TryGetValue(GamePropertyKey.IsOpen, out var isOpen) && !(bool) isOpen)
			{
				_statechartTrigger(ConnectedEvent);
			}
		}

		/// <inheritdoc />
		public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
		{
			FLog.Info("OnPlayerPropertiesUpdate " + targetPlayer.NickName);
		}

		/// <inheritdoc />
		public void OnMasterClientSwitched(Player newMasterClient)
		{
			FLog.Info("OnMasterClientSwitched " + newMasterClient.NickName);
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

		private void StartMatchmaking()
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
	}
}
