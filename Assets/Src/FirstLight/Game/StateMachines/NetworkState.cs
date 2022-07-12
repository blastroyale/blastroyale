using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Statechart;
using I2.Loc;
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
	public class NetworkState : IConnectionCallbacks, IMatchmakingCallbacks, IInRoomCallbacks
	{
		public static readonly IStatechartEvent PhotonMasterConnectedEvent = new StatechartEvent("NETWORK - Photon Master Connected Event");
		public static readonly IStatechartEvent PhotonDisconnectedEvent = new StatechartEvent("NETWORK - Photon Disconnected Event");
		public static readonly IStatechartEvent DisconnectedScreenBackEvent = new StatechartEvent("NETWORK - Disconnected Screen Back Event");
		public static readonly IStatechartEvent CreateRoomFailedEvent = new StatechartEvent("NETWORK - Create Room Failed Event");
		public static readonly IStatechartEvent JoinedRoomEvent = new StatechartEvent("NETWORK - Joined Room Event");
		public static readonly IStatechartEvent JoinRoomFailedEvent = new StatechartEvent("NETWORK - Join Room Fail Event");
		public static readonly IStatechartEvent LeftRoomEvent = new StatechartEvent("NETWORK - Left Room Event");
		public static readonly IStatechartEvent RoomClosedEvent = new StatechartEvent("NETWORK - Room Closed Event");
		public static readonly IStatechartEvent AttemptReconnectEvent = new StatechartEvent("NETWORK - Attempt Reconnect Event");
		
		private readonly IGameServices _services; 
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameUiService _uiService;
		private readonly IGameBackendNetworkService _networkService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private QuantumRunnerConfigs QuantumRunnerConfigs => _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

		public NetworkState(IGameLogic gameLogic, IGameServices services, IGameUiService uiService,
		                    IGameBackendNetworkService networkService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
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
			var disconnectedScreen = stateFactory.State("NETWORK - Disconnected Screen");
			var reconnecting = stateFactory.State("NETWORK - Reconnecting Screen");
			
			initial.Transition().Target(initialConnection);
			initial.OnExit(SubscribeEvents);

			initialConnection.OnEnter(ConnectPhoton);
			initialConnection.Event(PhotonMasterConnectedEvent).Target(connected);
			
			connected.Event(PhotonDisconnectedEvent).Target(disconnectedScreen);
			
			disconnectedScreen.OnEnter(OpenDisconnectedScreen);
			disconnectedScreen.Event(AttemptReconnectEvent).Target(reconnecting);
			disconnectedScreen.Event(DisconnectedScreenBackEvent).OnTransition(CloseDisconnectedScreen).Target(disconnected);

			reconnecting.OnEnter(DimDisconnectedScreen);
			reconnecting.Event(PhotonMasterConnectedEvent).Target(connected);
			reconnecting.Event(JoinedRoomEvent).Target(connected);
			reconnecting.Event(JoinRoomFailedEvent).Target(connected);
			reconnecting.OnExit(UndimDisconnectedScreen);
			reconnecting.OnExit(CloseDisconnectedScreen);
			
			disconnected.OnEnter(UpdateDisconnectionLocation);
			disconnected.OnEnter(ConnectPhoton);
			disconnected.Event(PhotonMasterConnectedEvent).Target(connected);

			final.OnEnter(UnsubscribeEvents);
		}

		private void UpdateDisconnectionLocation()
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

		private void OpenDisconnectedScreen()
		{
			var data = new DisconnectedScreenPresenter.StateData
			{
				ReconnectClicked = OnAttemptReconnectClicked,
				BackClicked = () => { _statechartTrigger(DisconnectedScreenBackEvent);}
			};

			_uiService.OpenUiAsync<DisconnectedScreenPresenter, DisconnectedScreenPresenter.StateData>(data);
		}

		private void OnAttemptReconnectClicked()
		{
			_statechartTrigger(AttemptReconnectEvent);
					
			if (CurrentSceneIsMatch())
			{
				_networkService.IsJoiningNewMatch.Value = false;
						
				if (_networkService.LastDisconnectLocation.Value == LastDisconnectionLocation.Matchmaking)
				{
					_networkService.IsJoiningNewMatch.Value = true;
					SetSpectatePlayerProperty(false);
				}

				_networkService.QuantumClient.ReconnectAndRejoin();
			}
			else
			{
				_networkService.QuantumClient.ReconnectToMaster();
			}
		}

		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void CloseDisconnectedScreen()
		{
			_uiService.CloseUi<DisconnectedScreenPresenter>(false, true);
		}
		
		private void DimDisconnectedScreen()
		{
			_uiService.GetUi<DisconnectedScreenPresenter>().SetFrontDimBlockerActive(true);
		}
		
		private void UndimDisconnectedScreen()
		{
			_uiService.GetUi<DisconnectedScreenPresenter>().SetFrontDimBlockerActive(false);
		}
		
		private void SubscribeEvents()
		{
			_services.TickService.SubscribeOnUpdate(TickQuantumServer, 0.1f, true, true);
			_services.MessageBrokerService.Subscribe<ApplicationQuitMessage>(OnApplicationQuit);
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
		}
		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services?.TickService?.UnsubscribeAll(this);
			QuantumCallback.UnsubscribeListener(this);
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
		}

		/// <inheritdoc />
		public void OnDisconnected(DisconnectCause cause)
		{
			FLog.Info("OnDisconnected " + cause);
			
			var dictionary = new Dictionary<string, object>
			{
				{"disconnected_cause", _services.NetworkService.QuantumClient.DisconnectedCause}
			};
			
			_services.AnalyticsService.LogEvent("disconnected", dictionary);
			_services.AnalyticsService.CrashLog($"Disconnected - {_services.NetworkService.QuantumClient.DisconnectedCause}");

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

			var title = string.Format(ScriptLocalization.MainMenu.RoomError, message);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenDialog(title, false, confirmButton);
			
			_statechartTrigger(CreateRoomFailedEvent);
		}

		/// <inheritdoc />
		public void OnJoinedRoom()
		{
			FLog.Info("OnJoinedRoom");

			_statechartTrigger(JoinedRoomEvent);
			
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

				// Set the game mode to match the map of the room (can't be set earlier if joining rooms in custom games)
				int mapId = (int) _networkService.QuantumClient.CurrentRoom.CustomProperties[GameConstants.Network.ROOM_PROPS_MAP];
				QuantumMapConfig mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(mapId);
				_gameDataProvider.AppDataProvider.SelectedGameMode.Value = mapConfig.GameMode;
			}

			if (QuantumRunnerConfigs.IsOfflineMode)
			{
				LockRoom();
			}
			else if (_networkService.IsCurrentRoomForMatchmaking)
			{
				StartMatchmakingLockRoomTimer();
			}
		}

		/// <inheritdoc />
		public void OnJoinRoomFailed(short returnCode, string message)
		{
			FLog.Info($"OnJoinRoomFailed: {returnCode.ToString()} - {message}");
			
			var title = string.Format(ScriptLocalization.MainMenu.RoomError, message);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK, 
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenDialog(title, false, confirmButton);
			
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
			
			if (changedProps.TryGetValue(GameConstants.Network.PLAYER_PROPS_ALL_LOADED, out var loadedMatch) && (bool) loadedMatch)
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
			_networkService.QuantumClient.CurrentRoom.SetCustomProperties(new Hashtable{{GameConstants.Network.ROOM_PROPS_BOTS, message.AddBots}});
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
				{ GameConstants.Network.PLAYER_PROPS_SPECTATOR, isSpectator }
			};
			
			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		private void OnRoomLeaveClickedMessage(RoomLeaveClickedMessage msg)
		{
			LeaveRoom();
		}
		
		private void OnMatchSimulationEndedMessage(MatchSimulationEndedMessage msg)
		{
			LeaveRoom();
		}

		private void OnPlayMatchmakingReadyMessage(PlayMatchmakingReadyMessage msg)
		{
			var mapConfig = NetworkUtils.GetRotationMapConfig(_gameDataProvider.AppDataProvider.SelectedGameMode.Value, _services);
			StartRandomMatchmaking(mapConfig);
		}
		
		private void OnPlayMapClickedMessage(PlayMapClickedMessage msg)
		{
			var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>(msg.MapId);
			StartRandomMatchmaking(mapConfig);
		}
		
		private void OnPlayCreateRoomClickedMessage(PlayCreateRoomClickedMessage msg)
		{
			CreateRoom(msg.MapConfig, msg.RoomName);
		}
		
		private void OnPlayJoinRoomClickedMessage(PlayJoinRoomClickedMessage msg)
		{
			JoinRoom(msg.RoomName);
		}
		
		private void OnCoreMatchAssetsLoaded(CoreMatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{ GameConstants.Network.PLAYER_PROPS_CORE_LOADED, true }
			};
			
			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		private void OnAllMatchAssetsLoaded(AllMatchAssetsLoadedMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{ GameConstants.Network.PLAYER_PROPS_ALL_LOADED, true }
			};
			
			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}
		
		private void OnAssetReloadRequiredMessage(AssetReloadRequiredMessage msg)
		{
			var playerPropsUpdate = new Hashtable
			{
				{ GameConstants.Network.PLAYER_PROPS_CORE_LOADED, false },
				{ GameConstants.Network.PLAYER_PROPS_ALL_LOADED, false }
			};
			
			_services.NetworkService.QuantumClient.LocalPlayer.SetCustomProperties(playerPropsUpdate);
		}

		private void OnApplicationQuit(ApplicationQuitMessage data)
		{
			_networkService.QuantumClient.Disconnect();
		}

		private void StartRandomMatchmaking(QuantumMapConfig mapConfig)
		{
			var gridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var enterParams = NetworkUtils.GetRoomCreateParams(mapConfig, gridConfigs, null);
			var joinRandomParams = NetworkUtils.GetJoinRandomRoomParams(mapConfig);

			QuantumRunnerConfigs.IsOfflineMode = mapConfig.PlayersLimit == 1;

			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				_networkService.QuantumClient.OpJoinRandomOrCreateRoom(joinRandomParams, enterParams);
			}
		}

		private void JoinRoom(string roomName)
		{
			var enterParams = NetworkUtils.GetRoomEnterParams(roomName);
			
			QuantumRunnerConfigs.IsOfflineMode = false;
			
			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				_networkService.QuantumClient.OpJoinRoom(enterParams);
			}
		}

		private void CreateRoom(QuantumMapConfig mapConfig, string roomName)
		{
			var gridConfigs = _services.ConfigsProvider.GetConfig<MapGridConfigs>();
			var enterParams = NetworkUtils.GetRoomCreateParams(mapConfig, gridConfigs, roomName);

			QuantumRunnerConfigs.IsOfflineMode = false;

			UpdateQuantumClientProperties();

			if (!_networkService.QuantumClient.InRoom)
			{
				SetSpectatePlayerProperty(false);
				_networkService.IsJoiningNewMatch.Value = true;
				_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.None;
				_networkService.QuantumClient.OpCreateRoom(enterParams);
			}
		}
		
		private void TickQuantumServer(float deltaTime)
		{
			_networkService.QuantumClient.Service();

			// TODO: Make the lag check in the game
			//_networkService.CheckLag();
		}
		
		private void StartMatchmakingLockRoomTimer()
		{
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

		private void ConnectPhoton()
		{
			var settings = QuantumRunnerConfigs.PhotonServerSettings.AppSettings;
			
			UpdateQuantumClientProperties();
			_networkService.QuantumClient.ConnectUsingSettings(settings, _gameDataProvider.AppDataProvider.Nickname);
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
			_networkService.QuantumClient.NickName = _gameDataProvider.AppDataProvider.Nickname;

			var preloadIds = new List<int>();
			
			foreach (var item in _gameDataProvider.EquipmentDataProvider.Loadout)
			{
				var equipmentDataInfo = _gameDataProvider.EquipmentDataProvider.Inventory[item.Value];
				preloadIds.Add((int) equipmentDataInfo.GameId);
			}

			preloadIds.Add((int) _gameDataProvider.PlayerDataProvider.CurrentSkin.Value);
			
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