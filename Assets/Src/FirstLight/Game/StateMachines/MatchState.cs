using System;
using System.Threading.Tasks;
using Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.RoomService;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;
using FirstLight.Statechart;
using I2.Loc;
using Photon.Realtime;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the match in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class MatchState
	{
		public static readonly IStatechartEvent MatchUnloadedEvent = new StatechartEvent("Match Unloaded Ready");
		public static readonly IStatechartEvent MatchErrorEvent = new StatechartEvent("Match Error Event");
		public static readonly IStatechartEvent MatchEndedEvent = new StatechartEvent("Game Ended Event");
		public static readonly IStatechartEvent MatchQuitEvent = new StatechartEvent("Game Quit Event");
		public static readonly IStatechartEvent MatchEndedExitEvent = new StatechartEvent("Match Ended Exit Event");
		public static readonly IStatechartEvent MatchCompleteExitEvent = new StatechartEvent("Game Complete Exit Event");
		public static readonly IStatechartEvent LeaveRoomClicked = new StatechartEvent("Leave Room Requested");
		public static readonly IStatechartEvent MatchStateEndingEvent = new StatechartEvent("Match Flow Leaving Event");
		public static readonly IStatechartEvent JoinedQuantumMatchmaking = new StatechartEvent("Enter Matchmaking");


		public static readonly IStatechartEvent RoomGameStartEvent = new StatechartEvent("NETWORK - Room Game Start Event");
		public static readonly IStatechartEvent CustomGameLoadStart = new StatechartEvent("NETWORK - Custom Game Load Start");

		private readonly GameSimulationState _gameSimulationState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly IRoomService _roomService;
		private readonly IGameUiService _uiService;
		private readonly IDataService _dataService;
		private readonly IAssetAdderService _assetAdderService;
		private IMatchServices _matchServices;
		private bool _arePlayerAssetsLoaded = false;
		private Action<IStatechartEvent> _statechartTrigger;

		public MatchState(IGameServices services, IDataService dataService, IInternalGameNetworkService networkService, IGameUiService uiService,
						  IGameDataProvider gameDataProvider,
						  IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger, IRoomService roomService)
		{
			_statechartTrigger = statechartTrigger;
			_roomService = roomService;
			_services = services;
			_dataProvider = gameDataProvider;
			_networkService = networkService;
			_dataService = dataService;
			_uiService = uiService;
			_assetAdderService = assetAdderService;
			_gameSimulationState = new GameSimulationState(gameDataProvider, services, networkService, uiService, statechartTrigger);

			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
			_roomService.OnMatchStarted += () =>
			{
				statechartTrigger(RoomGameStartEvent);
			};
			_roomService.OnCustomGameLoadStart += () =>
			{
				statechartTrigger(CustomGameLoadStart);
			};
			_roomService.OnLocalPlayerKicked += OnLocalPlayerKicked;
		}
        

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var loading = stateFactory.Transition("Loading Assets");
			var openLoadingScreen = stateFactory.TaskWait("Open Pre Game Screen");
			var openCustomGameScreen = stateFactory.TaskWait("Open CustomGame Screen");
			var roomCheck = stateFactory.Choice("Room Check");
			var gameLoading = stateFactory.State("Matchmaking");
			var gameSimulation = stateFactory.Nest("Game Simulation");
			var disconnected = stateFactory.State("Disconnected");
			var postDisconnectCheck = stateFactory.Choice("Post Reload Check");
			var customGameCheck = stateFactory.Choice("Custom Game Check");
			var customGameLobby = stateFactory.State("Custom Game Lobby");
			var gameEndedChoice = stateFactory.Choice("Game Ended Check");
			var leaderboardsCheck = stateFactory.Choice("Tutorial Check");
			var gameEnded = stateFactory.State("Game Ended Screen");
			var showWinner = stateFactory.State("Show Winner Screen");
			var transitionToWinners = stateFactory.TaskWait("Unload to Winners UI");
			var transitionToGameResults = stateFactory.TaskWait("Unload to Game Results UI");
			var transitionToMenu = stateFactory.TaskWait("Unload to Menu");
			var winners = stateFactory.Wait("Winners Screen");
			var randomLeftRoom = stateFactory.Choice("Oddly left room");
			var gameResults = stateFactory.Wait("Game Results Screen");
			var matchStateEnding = stateFactory.Wait("Publish Wait Match State Ending");

			initial.Transition().Target(customGameCheck);
			initial.OnExit(SubscribeEvents);

			// Reconnection is first, because if the custom game is running we skip the custom game screen
			customGameCheck.Transition().Condition(IsInstantLoad).Target(loading);
			customGameCheck.Transition().Condition(IsReconnection).Target(loading);
			customGameCheck.Transition().Condition(IsCustomGame).Target(openCustomGameScreen);
			customGameCheck.Transition().Target(openLoadingScreen);

			openCustomGameScreen.WaitingFor(OpenCustomLobbyScreen).Target(customGameLobby);

			customGameLobby.OnEnter(CloseSwipeTransition);
			customGameLobby.Event(NetworkState.LeftRoomEvent).Target(randomLeftRoom);
			customGameLobby.Event(CustomGameLoadStart).Target(openLoadingScreen);

			openLoadingScreen.WaitingFor(OpenPreGameScreen).Target(loading);
			openLoadingScreen.OnExit(CloseSwipeTransition);

			loading.OnEnter(StartMatchLoading);
			loading.Transition().Target(roomCheck);

			roomCheck.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(transitionToMenu);
			roomCheck.Transition().Condition(IsGameStarted).Target(gameSimulation);
			roomCheck.Transition().Target(gameLoading);

			gameLoading.Event(RoomGameStartEvent).Target(gameSimulation);
			gameLoading.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(disconnected);
			gameLoading.Event(NetworkState.LeftRoomEvent).Target(randomLeftRoom);

			randomLeftRoom.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(disconnected);
			randomLeftRoom.Transition().Target(transitionToMenu);
			
			/// This state makes a fork and both default OnTransition and gameSimulation.Event(MatchErrorEvent).Target(transitionToMenu); executes
			/// https://tree.taiga.io/project/firstlightgames-blast-royale-reloaded/issue/2737
			gameSimulation.Nest(_gameSimulationState.Setup).OnTransition(() => HandleSimulationEnd(false)).Target(gameEndedChoice);
			gameSimulation.Event(MatchErrorEvent).Target(transitionToMenu);
			gameSimulation.Event(MatchEndedEvent).OnTransition(() => HandleSimulationEnd(false)).Target(gameEndedChoice);
			gameSimulation.Event(MatchQuitEvent).OnTransition(() => HandleSimulationEnd(true)).Target(transitionToMenu);
			gameSimulation.Event(NetworkState.PhotonCriticalDisconnectedEvent).OnTransition(OnCriticalDisconnectDuringSimulation)
				.Target(disconnected);

			gameEndedChoice.OnEnter(CloseMatchHud);
			gameEndedChoice.Transition().Condition(IsSimulationBroken).Target(final);
			gameEndedChoice.Transition().Condition(HasLeftBeforeMatchEnded).Target(transitionToGameResults);
			gameEndedChoice.Transition().Target(gameEnded);

			gameEnded.OnEnter(OpenMatchEndScreen);
			gameEnded.Event(MatchEndedExitEvent).Target(showWinner);
			gameEnded.Event(MatchCompleteExitEvent).Target(transitionToWinners);

			showWinner.OnEnter(OpenWinnerScreen);
			showWinner.Event(MatchCompleteExitEvent).Target(leaderboardsCheck);

			leaderboardsCheck.Transition().Condition(IsPlayingFirstTutorial).Target(transitionToMenu);
			leaderboardsCheck.Transition().Condition(HasValidPlayer).Target(transitionToWinners);
			leaderboardsCheck.Transition().Target(transitionToMenu);

			transitionToMenu.WaitingFor(UnloadMatchAndTransition).Target(matchStateEnding);
			transitionToWinners.WaitingFor(UnloadMatchAndTransition).Target(winners);
			transitionToGameResults.WaitingFor(UnloadMatchAndTransition).Target(gameResults);

			winners.OnEnter(CloseSwipeTransition);
			winners.WaitingFor(OpenWinnersScreen).Target(gameResults);

			gameResults.OnEnter(CloseSwipeTransition);
			gameResults.WaitingFor(OpenLeaderboardAndRewardsScreen).Target(matchStateEnding);
			gameResults.OnExit(DisposeMatchServices);
			gameResults.OnExit(() => _ = OpenSwipeTransition());

			disconnected.Event(RoomGameStartEvent).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinedPlayfabMatchmaking).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(transitionToMenu);

			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringMatchmaking).Target(roomCheck);
			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringSimulation).OnTransition(CloseCurrentScreen).Target(roomCheck);
			postDisconnectCheck.Transition().OnTransition(CloseCurrentScreen).Target(transitionToMenu);
			
			matchStateEnding.WaitingFor(a => _ = MatchStateEndTrigger(a)).Target(final);
			matchStateEnding.OnExit(UnloadMainMenuAssetConfigs);

			final.OnEnter(DisposeMatchServices);
			final.OnEnter(UnsubscribeEvents);
		}
		

		private bool IsInstantLoad()
		{
			return _services.RoomService.CurrentRoom.GameModeConfig.InstantLoad;
		}

		private bool IsReconnection()
		{
			return _roomService.CurrentRoom.GameStarted || _networkService.JoinSource.Value.IsSnapshotAutoConnect();
		}


		private bool IsCustomGame()
		{
			return _roomService.CurrentRoom.Properties.MatchType.Value == MatchType.Custom;
		}

		private bool IsGameStarted()
		{
			return _roomService.CurrentRoom.GameStarted;
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, OnGameEnded);
			QuantumEvent.SubscribeManual<EventFireQuantumServerCommand>(this, OnServerCommand);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
		}

		private void CloseMatchHud()
		{
			_uiService.CloseUi<HUDScreenPresenter>();
		}

		private bool HasLeftBeforeMatchEnded()
		{
			return _matchServices.MatchEndDataService.LeftBeforeMatchFinished;
		}

		private bool HasValidPlayer()
		{
			var players = QuantumRunner.Default?.Game?.Frames?.Verified?.GetSingleton<GameContainer>().PlayersData;
			if (!players.HasValue)
			{
				return false;
			}

			for (var x = 0; x < players.Value.Length; x++)
			{
				if (players.Value[x].IsValid)
				{
					return true;
				}
			}

			return false;
		}

		private bool IsPlayingFirstTutorial()
		{
			return _services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.FIRST_GUIDE_MATCH;
		}


		/// <summary>
		/// Whenever the simulation wants to fire logic commands.
		/// This will also run on quantum server and will be sent to logic service from there.
		/// </summary>
		private void OnServerCommand(EventFireQuantumServerCommand ev)
		{
			var game = ev.Game;
			if (!game.PlayerIsLocal(ev.Player))
			{
				return;
			}

			FLog.Verbose("Quantum Logic Command Received: " + ev.CommandType.ToString());
			var command = QuantumLogicCommandFactory.BuildFromEvent(ev);
			var room = _services.NetworkService.QuantumClient.CurrentRoom;
			command.FromFrame(game.Frames.Verified, new QuantumValues()
			{
				MatchId = room.Name,
				ExecutingPlayer = game.GetLocalPlayers()[0],
				MatchType = _roomService.CurrentRoom.Properties.MatchType.Value,
				AllowedRewards = _roomService.CurrentRoom.Properties.AllowedRewards.Value
			});
			_services.CommandService.ExecuteCommand(command as IGameCommand);
		}

		private void OpenMatchEndScreen()
		{
			var data = new MatchEndScreenPresenter.StateData
			{
				OnTimeToLeave = () => _statechartTrigger(MatchEndedExitEvent),
			};

			_uiService.OpenScreen<MatchEndScreenPresenter, MatchEndScreenPresenter.StateData>(data);
			_matchServices.FrameSnapshotService.ClearFrameSnapshot();
		}

		private void OpenWinnerScreen()
		{
			var data = new WinnerScreenPresenter.StateData
			{
				ContinueClicked = () => _statechartTrigger(MatchCompleteExitEvent)
			};

			_uiService.OpenScreen<WinnerScreenPresenter, WinnerScreenPresenter.StateData>(data);
			_matchServices.FrameSnapshotService.ClearFrameSnapshot();
		}


		private void OnGameEnded(EventOnGameEnded callback)
		{
			_statechartTrigger(MatchEndedEvent);
		}

		private void OnDisconnectDuringMatchmaking()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Matchmaking;
		}

		private void OnDisconnectDuringFinalPreload()
		{
			FLog.Warn("Disconnected during final preload");
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.FinalPreload;
			_uiService.CloseUi<CustomLobbyScreenPresenter>();
			_uiService.CloseUi<PreGameLoadingScreenPresenter>();
		}

		private void OnCriticalDisconnectDuringSimulation()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Simulation;

			PublishMatchEnded(true, false, null);
		}

		private void CloseCurrentScreen()
		{
			_uiService.CloseCurrentScreen();
		}

		private bool HasDisconnectedDuringMatchmaking()
		{
			return _networkService.LastDisconnectLocation.Value == LastDisconnectionLocation.Matchmaking;
		}

		private bool HasDisconnectedDuringSimulation()
		{
			return _networkService.LastDisconnectLocation.Value == LastDisconnectionLocation.Simulation;
		}
        

		private void StartMatchLoading()
		{
			var entityService = new GameObject(nameof(EntityViewUpdaterService))
				.AddComponent<EntityViewUpdaterService>();
			_matchServices = new MatchServices(entityService, _services, _dataProvider, _dataService);
			MainInstaller.Bind(_matchServices);
			_matchServices.MatchAssetService.StartMandatoryAssetLoad();
			_matchServices.MatchAssetService.StartOptionalAssetLoad();
		}

		private async Task UnloadAllMatchAssets()
		{
			if (_matchServices == null) return;
			await _matchServices.MatchAssetService.UnloadAllMatchAssets();
			_statechartTrigger(MatchUnloadedEvent);
		}

		private async UniTaskVoid MatchStateEndTrigger(IWaitActivity activity)
		{
			// Workaround to triggering statechart events on enter/exit
			// Necessary for audio to play at correct time, but this can't be called OnEnter or OnExit, or the 
			// state machine ends up working very strangely.
			// FIX THIS SHIT = 5000 TACOS
			await UniTask.NextFrame();
			_statechartTrigger(MatchStateEndingEvent);
			await UniTask.NextFrame();
			activity.Complete();
		}

		private void PublishMatchEnded(bool isDisconnected, bool isPlayerQuit, QuantumGame game)
		{
			_services.MessageBrokerService.Publish(new MatchEndedMessage()
			{
				Game = game,
				IsDisconnected = isDisconnected,
				IsPlayerQuit = isPlayerQuit
			});
		}

		private void HandleSimulationEnd(bool playerQuit)
		{
			FLog.Verbose("[MatchState] Match End");
			PublishMatchEnded(false, playerQuit, QuantumRunner.Default.Game);

			_services.AnalyticsService.MatchCalls.MatchEnd(QuantumRunner.Default.Game, playerQuit,
				_matchServices.MatchEndDataService.LocalPlayerMatchData.PlayerRank);
			_matchServices.FrameSnapshotService.ClearFrameSnapshot();
			if (playerQuit)
			{
				_services.MessageBrokerService.Publish(new LeftBeforeMatchFinishedMessage());
			}
		}

		private bool IsSimulationBroken()
		{
			return QuantumRunner.Default == null || QuantumRunner.Default.IsDestroyed() || NetworkUtils.IsOfflineOrDisconnected();
		}

		private async Task UnloadMatchAndTransition()
		{
			FLog.Verbose("[MatchState] Unloading Match State");
			CloseCurrentScreen();

			StopSimulation();

			await SwipeScreenPresenter.StartSwipe();
			await UnloadAllMatchAssets();

			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MainMenuAssetConfigs>());
			FLog.Verbose("[MatchState] Finished Unloading Match State");
		}

		private void UnloadMainMenuAssetConfigs()
		{
			FLog.Verbose("Unloading Main Menu Asssets");
			_services.AssetResolverService.UnloadAssets(true, _services.ConfigsProvider.GetConfig<MainMenuAssetConfigs>());
		}

		private void StopSimulation()
		{
			_services.MessageBrokerService.Publish(new SimulationEndedMessage {Game = QuantumRunner.Default?.Game});
			if (QuantumRunner.Default != null && QuantumRunner.Default.IsRunning)
			{
#if UNITY_EDITOR
				if (FeatureFlags.GetLocalConfiguration().RecordQuantumInput)
				{
					Quantum.Editor.ReplayMenu.ExportDialogReplayAndDB(QuantumRunner.Default.Game, new QuantumUnityJsonSerializer(), ".json");
				}
#endif
				_matchServices.MatchEndDataService.Reload();
				QuantumRunner.ShutdownAll(true);
			}
		}

		private void DisposeMatchServices()
		{
			if (MainInstaller.TryResolve<IMatchServices>(out var services))
			{
				services.Dispose();

				MainInstaller.Clean<IMatchServices>();
			}
		}

		//////////////
		/// UI CODE //
		//////////////

		#region UI Handling

		private void DismissGenericPopups()
		{
			_services.GenericDialogService.CloseDialog();
		}

		private void OpenWinnersScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new WinnersScreenPresenter.StateData {ContinueClicked = () => cacheActivity.Complete()};
			_uiService.OpenScreenAsync<WinnersScreenPresenter, WinnersScreenPresenter.StateData>(data);
		}

		private void OpenLeaderboardAndRewardsScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new LeaderboardAndRewardsScreenPresenter.StateData
			{
				ContinueClicked = () => cacheActivity.Complete()
			};

			_uiService.OpenScreen<LeaderboardAndRewardsScreenPresenter, LeaderboardAndRewardsScreenPresenter.StateData>(data);
		}

		private async Task OpenSwipeTransition()
		{
			_uiService.CloseCurrentScreen();
			await SwipeScreenPresenter.StartSwipe();
		}

		private async Task OpenPreGameScreen()
		{
			await OpenSwipeTransition();
			FLog.Verbose("Entering Match State");
			_services.AnalyticsService.MatchCalls.MatchInitiate();

			if (!_roomService.InRoom) return;

			// TODO: Reconnection screen but for now its MM screen
			var data = new PreGameLoadingScreenPresenter.StateData
			{
				LeaveRoomClicked = () => _statechartTrigger(LeaveRoomClicked)
			};

			await _uiService.OpenScreenAsync<PreGameLoadingScreenPresenter, PreGameLoadingScreenPresenter.StateData>(data);
		}

		private async Task OpenCustomLobbyScreen()
		{
			var data = new CustomLobbyScreenPresenter.StateData
			{
				LeaveRoomClicked = () => _statechartTrigger(LeaveRoomClicked)
			};
			await _uiService.OpenScreenAsync<CustomLobbyScreenPresenter, CustomLobbyScreenPresenter.StateData>(data);
		}

		private void OnLocalPlayerKicked()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK.ToUpper(),
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.MainMenu.MatchmakingKickedNotification.ToUpper(), false, confirmButton);
		}

		private void CloseSwipeTransition() => _ = SwipeScreenPresenter.Finish();

		#endregion
	}
}