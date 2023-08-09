using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Src.FirstLight.Game.Commands.QuantumLogicCommands;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Modules.Commands;
using FirstLight.Services;
using FirstLight.Statechart;
using Photon.Deterministic;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

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
		
		private readonly GameSimulationState _gameSimulationState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IInternalGameNetworkService _networkService;
		private readonly IGameUiService _uiService;
		private readonly IDataService _dataService;
		private readonly IAssetAdderService _assetAdderService;
		private IMatchServices _matchServices;
		private bool _arePlayerAssetsLoaded = false;
		private Action<IStatechartEvent> _statechartTrigger;

		public MatchState(IGameServices services, IDataService dataService, IInternalGameNetworkService networkService, IGameUiService uiService, IGameDataProvider gameDataProvider, 
		                  IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_statechartTrigger = statechartTrigger;
			_services = services;
			_dataProvider = gameDataProvider;
			_networkService = networkService;
			_dataService = dataService;
			_uiService = uiService;
			_assetAdderService = assetAdderService;
			_gameSimulationState = new GameSimulationState(gameDataProvider, services, networkService, uiService, statechartTrigger);

			_services.NetworkService.QuantumClient.AddCallbackTarget(this);
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var loading = stateFactory.Transition("Loading Assets");
			var openScreen = stateFactory.TaskWait("Open Screen");
			var roomCheck = stateFactory.Choice("Room Check");
			var matchmaking = stateFactory.State("Matchmaking");
			var playerReadyCheck = stateFactory.Choice("Player Ready Check");
			var playerReadyWait = stateFactory.TaskWait("Player Ready Wait");
			var gameSimulation = stateFactory.Nest("Game Simulation");
			var disconnected = stateFactory.State("Disconnected");
			var postDisconnectCheck = stateFactory.Choice("Post Reload Check");
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
			var matchStateEnding = stateFactory.TaskWait("Publish Wait Match State Ending");
			
			initial.Transition().Target(openScreen);
			initial.OnExit(SubscribeEvents);
			
			openScreen.WaitingFor(OpenMatchmakingScreen).Target(loading);
			
			loading.OnEnter(StartMatchLoading);
			loading.Transition().Target(roomCheck);
			loading.OnExit(CloseSwipeTransitionTutorialCheck);
			
			roomCheck.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(transitionToMenu);
			roomCheck.Transition().Condition(IsSkipMatchmakingScreen).Target(playerReadyCheck);
			roomCheck.Transition().Target(matchmaking);
			
			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(disconnected);
			matchmaking.Event(NetworkState.RoomReadyEvent).Target(playerReadyCheck);
			matchmaking.Event(NetworkState.LeftRoomEvent).Target(randomLeftRoom);

			randomLeftRoom.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(disconnected);
			randomLeftRoom.Transition().Target(transitionToMenu);
			
			//playerReadyCheck.Transition().Condition(IsMatchOver).Target(gameEndedChoice);
			playerReadyCheck.Transition().Condition(AreAllPlayersReady).Target(gameSimulation);
			playerReadyCheck.Transition().Condition(HasGameAlreadyStarted).Target(gameSimulation);
			playerReadyCheck.Transition().Target(playerReadyWait);
			
			playerReadyWait.WaitingFor(WaitPlayersReady).Target(gameSimulation);
			playerReadyWait.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(transitionToMenu);
			playerReadyWait.Event(NetworkState.LeftRoomEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(transitionToMenu);
			
			gameSimulation.Nest(_gameSimulationState.Setup).OnTransition(() => HandleSimulationEnd(false)).Target(gameEndedChoice);
			gameSimulation.Event(MatchErrorEvent).Target(transitionToMenu);
			gameSimulation.Event(MatchEndedEvent).OnTransition(() => HandleSimulationEnd(false)).Target(gameEndedChoice);
			gameSimulation.Event(MatchQuitEvent).OnTransition(() => HandleSimulationEnd(true)).Target(transitionToMenu);
			gameSimulation.Event(NetworkState.PhotonCriticalDisconnectedEvent).OnTransition(OnCriticalDisconnectDuringSimulation).Target(disconnected);
			
			gameEndedChoice.OnEnter(CloseMatchHud);
			gameEndedChoice.Transition().Condition(IsSimulationStopped).Target(final);
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
			
			disconnected.Event(NetworkState.RoomReadyEvent).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinedPlayfabMatchmaking).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(transitionToMenu);
			disconnected.Event(NetworkState.DcScreenBackEvent).Target(transitionToMenu);
			
			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringMatchmaking).Target(roomCheck);
			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringSimulation).OnTransition(CloseCurrentScreen).Target(roomCheck);
			postDisconnectCheck.Transition().OnTransition(CloseCurrentScreen).Target(transitionToMenu);

			matchStateEnding.WaitingFor(MatchStateEndTrigger).Target(final);
			matchStateEnding.OnExit(UnloadMainMenuAssetConfigs);
			
			final.OnEnter(DisposeMatchServices);
			final.OnEnter(UnsubscribeEvents);
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

		private async Task WaitPlayersReady()
		{
			DismissGenericPopups();
			_services.MessageBrokerService.Publish(new WaitingMandatoryMatchAssetsMessage());
			var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(10);
			while (DateTime.UtcNow < timeout && !AreAllPlayersReady()) await Task.Delay(10);
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
				MatchType = _services.NetworkService.QuantumClient.CurrentRoom.GetMatchType()
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
		}
		
		private void OpenWinnerScreen()
		{
			var data = new WinnerScreenPresenter.StateData
			{
				ContinueClicked = () => _statechartTrigger(MatchCompleteExitEvent)
			};

			_uiService.OpenScreen<WinnerScreenPresenter, WinnerScreenPresenter.StateData>(data);
		}

		private bool IsMatchmakingTimerComplete()
		{
			if (!_networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom())
			{
				return false;
			}
			var roomCreationTime = _networkService.QuantumClient.CurrentRoom.GetRoomCreationDateTime();
			var qConfig = _services.ConfigsProvider.GetConfig<QuantumGameConfig>();
			var waitSeconds = NetworkUtils.GetMatchmakingTime(_networkService.CurrentRoomMatchType.Value, _networkService.CurrentRoomGameModeConfig.Value, qConfig);
			var matchmakingEndTime = roomCreationTime.AddSeconds(waitSeconds);
			return DateTime.UtcNow > matchmakingEndTime;
		}

		private bool IsSkipMatchmakingScreen()
		{
			var skip = IsMatchmakingTimerComplete() || !_networkService.QuantumClient.CurrentRoom.IsOpen ||
				_networkService.JoinSource.Value.IsSnapshotAutoConnect() ||
				_networkService.QuantumClient.CurrentRoom.HaveStartedGame();
			
			if(skip) FLog.Verbose($"Skipping matchmaking screen TimerComplete={IsMatchmakingTimerComplete()} Room Closed={!_networkService.QuantumClient.CurrentRoom.IsOpen} Started={_networkService.QuantumClient.CurrentRoom.HaveStartedGame()}");
			return skip;
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
			_uiService.CloseUi<MatchmakingScreenPresenter>();
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

		private bool HasGameAlreadyStarted()
		{
			return _networkService.QuantumClient.CurrentRoom.HaveStartedGame();
		}

		private bool AreAllPlayersReady()
		{
			return _services.NetworkService.QuantumClient.CurrentRoom.AreAllPlayersReady();
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
			await _matchServices.MatchAssetService.UnloadAllMatchAssets();
			_statechartTrigger(MatchUnloadedEvent);
		}
		
		private async Task MatchStateEndTrigger()
		{
			// Workaround to triggering statechart events on enter/exit
			// Necessary for audio to play at correct time, but this can't be called OnEnter or OnExit, or the 
			// state machine ends up working very strangely.
			_statechartTrigger(MatchStateEndingEvent);
			await Task.Yield();
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
			FLog.Verbose("Match End");
			PublishMatchEnded(false, playerQuit, QuantumRunner.Default.Game);
			
			_services.AnalyticsService.MatchCalls.MatchEnd(QuantumRunner.Default.Game, playerQuit, _matchServices.MatchEndDataService.LocalPlayerMatchData.PlayerRank);
			_matchServices.FrameSnapshotService.ClearFrameSnapshot();
			if (playerQuit)
			{
				_services.MessageBrokerService.Publish(new LeftBeforeMatchFinishedMessage());
			}
		}

		private bool IsSimulationStopped()
		{
			return QuantumRunner.Default == null || QuantumRunner.Default.IsDestroyed();
		}

		private async Task UnloadMatchAndTransition()
		{
			FLog.Verbose("Unloading Match State");
			CloseCurrentScreen();
			
			StopSimulation();

			await SwipeScreenPresenter.StartSwipe();
			await UnloadAllMatchAssets();

			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MainMenuAssetConfigs>());
		}
		
		private void UnloadMainMenuAssetConfigs()
		{
			// Unload the assets loaded in UnloadMatchAssets method
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
			_services.NetworkService.EnableClientUpdate(true);
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
		
		private async Task OpenMatchmakingScreen()
		{
			FLog.Verbose("Entering Match State");
			_services.AnalyticsService.MatchCalls.MatchInitiate();

			if (_networkService.CurrentRoom == null) return;

			// TODO: Reconnection screen but for now its MM screen
			var isRejoining =
				_networkService.QuantumClient.CurrentRoom.HaveStartedGame() || _networkService.JoinSource.Value.IsSnapshotAutoConnect();
			if (isRejoining || _networkService.QuantumClient.CurrentRoom.HaveStartedGame() || _networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom() || 
				_services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.META_GUIDE_AND_MATCH)
			{
				var data = new MatchmakingScreenPresenter.StateData
				{
					LeaveRoomClicked = () => _statechartTrigger(LeaveRoomClicked)
				};
				
				await _uiService.OpenScreenAsync<MatchmakingScreenPresenter, MatchmakingScreenPresenter.StateData>(data);
			}
			else
			{
				var data = new CustomLobbyScreenPresenter.StateData
				{
					LeaveRoomClicked = () => _statechartTrigger(LeaveRoomClicked)
				};
				await _uiService.OpenScreenAsync<CustomLobbyScreenPresenter, CustomLobbyScreenPresenter.StateData>(data);
			}
		}

		private void CloseSwipeTransitionTutorialCheck()
		{
			// If a tutorial is running (first match tutorial) - the transition will be closed later, in game simulation state
			// This is case for the FIRST_GUIDE_MATCH tutorial only
			if ((!_services.TutorialService.IsTutorialRunning || _services.TutorialService.CurrentRunningTutorial.Value == TutorialSection.META_GUIDE_AND_MATCH))
			{
				 _ = SwipeScreenPresenter.Finish();
			}
		}
		
		private void CloseSwipeTransition() => _ = SwipeScreenPresenter.Finish();
		#endregion
	}
}