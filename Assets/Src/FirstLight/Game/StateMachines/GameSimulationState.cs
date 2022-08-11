using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;
using Quantum.Commands;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		public static readonly IStatechartEvent SimulationStartedEvent = new StatechartEvent("Simulation Ready Event");
		public static readonly IStatechartEvent GameCompleteExitEvent = new StatechartEvent("Game Complete Exit Event");
		public static readonly IStatechartEvent MatchEndedEvent = new StatechartEvent("Game Ended Event");
		public static readonly IStatechartEvent MatchQuitEvent = new StatechartEvent("Game Quit Event");

		private readonly DeathmatchState _deathmatchState;
		private readonly BattleRoyaleState _battleRoyaleState;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		private int _lastTrophyChange = 0;
		private uint _trophiesBeforeLastChange = 0;

		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService,
		                           Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_deathmatchState = new DeathmatchState(gameDataProvider, services, uiService, statechartTrigger);
			_battleRoyaleState = new BattleRoyaleState(services, uiService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Game Simulation state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");

			var deathmatch = stateFactory.Nest("Deathmatch Mode");
			var battleRoyale = stateFactory.Nest("Battle Royale Mode");
			var modeCheck = stateFactory.Choice("Game Mode Check");
			var startSimulation = stateFactory.State("Start Simulation");
			var gameEnded = stateFactory.State("Game Ended Screen");
			var gameResults = stateFactory.Wait("Game Results Screen");
			var rewardsCheck = stateFactory.Choice("Rewards Choice");
			var trophiesCheck = stateFactory.Choice("Trophies Choice");
			var resultsSpectatorCheck = stateFactory.Choice("Results Spectator Choice");
			var gameRewards = stateFactory.Wait("Game Rewards Screen");
			var trophiesGainLoss = stateFactory.Wait("Trophies Gain Loss Screen");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(modeCheck);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);
			startSimulation.OnExit(PublishMatchReadyMessage);

			modeCheck.OnEnter(OpenAdventureWorldHud);
			modeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			modeCheck.Transition().Target(battleRoyale);

			deathmatch.Nest(_deathmatchState.Setup).Target(gameEnded);
			deathmatch.Event(MatchEndedEvent).Target(gameEnded);
			deathmatch.Event(MatchQuitEvent).OnTransition(() => MatchEndAnalytics(true)).Target(final);
			deathmatch.OnExit(PublishMatchEnded);

			battleRoyale.Nest(_battleRoyaleState.Setup).Target(gameEnded);
			battleRoyale.Event(MatchEndedEvent).Target(gameEnded);
			battleRoyale.Event(MatchQuitEvent).OnTransition(() => MatchEndAnalytics(true)).Target(final);
			battleRoyale.OnExit(PublishMatchEnded);

			gameEnded.OnEnter(() => MatchEndAnalytics(false));
			gameEnded.OnEnter(OpenGameCompleteScreen);
			gameEnded.Event(GameCompleteExitEvent).Target(resultsSpectatorCheck);
			gameEnded.OnExit(CloseCompleteScreen);

			resultsSpectatorCheck.Transition().Condition(IsSpectator).Target(final);
			resultsSpectatorCheck.Transition().Target(gameResults);

			gameResults.OnEnter(GiveMatchRewards);
			gameResults.WaitingFor(ResultsScreen).Target(trophiesCheck);
			gameResults.OnExit(CloseResultScreen);

			trophiesCheck.Transition().Condition(HasTrophyChangeToDisplay).Target(trophiesGainLoss);
			trophiesCheck.Transition().Target(rewardsCheck);

			trophiesGainLoss.WaitingFor(OpenTrophiesScreen).Target(rewardsCheck);
			trophiesGainLoss.OnExit(CloseTrophiesScreen);

			rewardsCheck.Transition().Condition(HasRewardsToClaim).Target(gameRewards);
			rewardsCheck.Transition().Target(final);

			gameRewards.WaitingFor(OpenRewardsScreen).Target(final);
			gameRewards.OnExit(CloseRewardsScreen);

			final.OnEnter(StopSimulation);
			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);

			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, OnGameEnded);
			QuantumCallback.SubscribeManual<CallbackGameStarted>(this, OnGameStart);
			QuantumCallback.SubscribeManual<CallbackGameResynced>(this, OnGameResync);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
			QuantumEvent.UnsubscribeListener(this);
			QuantumCallback.UnsubscribeListener(this);
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}

		private bool HasRewardsToClaim()
		{
			return _gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0;
		}

		private bool HasTrophyChangeToDisplay()
		{
			return _lastTrophyChange != 0;
		}

		private bool IsDeathmatch()
		{
			return _services.NetworkService.CurrentRoomMapConfig.Value.GameMode == GameMode.Deathmatch;
		}

		private async void OnGameStart(CallbackGameStarted callback)
		{
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			// Delays one frame just to guarantee that the game objects are created before anything else
			await Task.Yield();

			_statechartTrigger(SimulationStartedEvent);
		}


		private async void OnGameResync(CallbackGameResynced callback)
		{
			// Delays one frame just to guarantee that the game objects are created before anything else
			await Task.Yield();

			_statechartTrigger(SimulationStartedEvent);
		}

		private void OnGameEnded(EventOnGameEnded callback)
		{
			_statechartTrigger(MatchEndedEvent);
		}

		private void OnQuitGameScreenClickedMessage(QuitGameClickedMessage message)
		{
			var data = new QuitGameDialogPresenter.StateData {ConfirmClicked = QuitGameConfirmedClicked};

			_uiService.OpenUi<QuitGameDialogPresenter, QuitGameDialogPresenter.StateData>(data);
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_lastTrophyChange = message.TrophiesChange;
			_trophiesBeforeLastChange = message.TrophiesBeforeChange;
		}

		private void QuitGameConfirmedClicked()
		{
			if (!_services.NetworkService.QuantumClient.LocalPlayer.IsSpectator())
			{
				QuantumRunner.Default.Game.SendCommand(new PlayerQuitCommand());
			}
			
			_statechartTrigger(MatchQuitEvent);
		}

		private void GiveMatchRewards()
		{
			if (_gameDataProvider.AppDataProvider.SelectedGameMode.Value != GameMode.BattleRoyale)
			{
				return;
			}

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();

			_services.CommandService.ExecuteCommand(new EndOfGameCalculationsCommand
			{
				PlayersMatchData = gameContainer.GetPlayersMatchData(f, out _),
				LocalPlayerRef = game.GetLocalPlayers()[0],
				DidPlayerQuit = false,
				PlayedRankedMatch = _services.NetworkService.QuantumClient.CurrentRoom.IsRankedRoom()
			});
		}

		private void MatchEndAnalytics(bool playerQuit)
		{
			if (IsSpectator())
			{
				return;
			}

			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var gameContainer = f.GetSingleton<GameContainer>();
			var matchData = gameContainer.GetPlayersMatchData(f, out _);
			var localPlayerData = matchData[game.GetLocalPlayers()[0]];
			var totalPlayers = 0;

			for (var i = 0; i < matchData.Count; i++)
			{
				if (matchData[i].Data.IsValid && !f.Has<BotCharacter>(matchData[i].Data.Entity))
				{
					totalPlayers++;
				}
			}
   
			_services.AnalyticsService.MatchCalls.MatchEnd(totalPlayers, playerQuit, f.Time.AsFloat, localPlayerData);
		}

		private void StartSimulation()
		{
			var client = _services.NetworkService.QuantumClient;
			var configs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var room = client.CurrentRoom;
			var startPlayersCount = client.CurrentRoom.GetRealPlayerCapacity();

			if (room.CustomProperties.TryGetValue(GameConstants.Network.ROOM_PROPS_BOTS, out var gameHasBots) &&
			    !(bool) gameHasBots)
			{
				startPlayersCount = room.GetRealPlayerAmount();
			}

			var startParams = configs.GetDefaultStartParameters(startPlayersCount, IsSpectator());

			startParams.NetworkClient = client;
			
			QuantumRunner.StartGame(_services.NetworkService.UserId, startParams);
			_services.MessageBrokerService.Publish(new MatchSimulationStartedMessage());
		}

		private void StopSimulation()
		{
			_services.MessageBrokerService.Publish(new MatchSimulationEndedMessage());
			QuantumRunner.ShutdownAll();
		}

		private void PublishMatchEnded()
		{
			_services.MessageBrokerService.Publish(new MatchEndedMessage());
		}

		private void OpenAdventureWorldHud()
		{
			_uiService.OpenUi<MatchWorldHudPresenter>();
		}

		private void OpenGameCompleteScreen()
		{
			var data = new GameCompleteScreenPresenter.StateData {ContinueClicked = ContinueClicked};

			_uiService.OpenUi<GameCompleteScreenPresenter, GameCompleteScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				_statechartTrigger(GameCompleteExitEvent);
			}
		}

		private void CloseCompleteScreen()
		{
			_uiService.CloseUi<GameCompleteScreenPresenter>();
		}

		private void ResultsScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new ResultsScreenPresenter.StateData
			{
				ContinueButtonClicked = () => cacheActivity.Complete(),
				HomeButtonClicked = () => cacheActivity.Complete(),
			};

			_uiService.OpenUiAsync<ResultsScreenPresenter, ResultsScreenPresenter.StateData>(data);
		}

		private void CloseResultScreen()
		{
			_uiService.CloseUi<ResultsScreenPresenter>(false, true);
		}

		private void OpenRewardsScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new RewardsScreenPresenter.StateData {MainMenuClicked = ContinueClicked};

			_uiService.OpenUiAsync<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				cacheActivity.Complete();
			}
		}

		private void CloseRewardsScreen()
		{
			_uiService.CloseUi<RewardsScreenPresenter>(false, true);
		}

		private void OpenTrophiesScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new TrophiesScreenPresenter.StateData
			{
				ExitTrophyScreen = ContinueClicked,
				LastTrophyChange = () => _lastTrophyChange,
				TrophiesBeforeLastChange = () => _trophiesBeforeLastChange
			};

			_uiService.OpenUiAsync<TrophiesScreenPresenter, TrophiesScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				cacheActivity.Complete();
			}
		}

		private void CloseTrophiesScreen()
		{
			_uiService.CloseUi<TrophiesScreenPresenter>(false, true);
		}

		private void CloseMatchmakingScreen()
		{
			_uiService.CloseUi<MatchmakingLoadingScreenPresenter>(false, true);
		}

		private void PublishMatchReadyMessage()
		{
			if (_services.NetworkService.IsJoiningNewMatch)
			{
				MatchStartAnalytics();
				SetPlayerMatchData();
			}

			CloseMatchmakingScreen();

			_services.MessageBrokerService.Publish(new MatchReadyMessage());
		}

		private void SetPlayerMatchData()
		{
			var info = _gameDataProvider.PlayerDataProvider.PlayerInfo;
			var game = QuantumRunner.Default.Game;
			var loadout = _gameDataProvider.EquipmentDataProvider.Loadout;
			var inventory = _gameDataProvider.EquipmentDataProvider.Inventory;
			var spawnPosition = _uiService.GetUi<MatchmakingLoadingScreenPresenter>().MapSelectionView
			                              .NormalizedSelectionPoint;

			if (!IsSpectator())
			{
				game.SendPlayerData(game.GetLocalPlayers()[0], new RuntimePlayer
				{
					PlayerId = _gameDataProvider.AppDataProvider.PlayerId,
					PlayerName = _gameDataProvider.AppDataProvider.Nickname,
					Skin = info.Skin,
					DeathMarker = info.DeathMarker,
					PlayerLevel = info.Level,
					PlayerTrophies = info.TotalTrophies,
					NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
					Loadout = loadout.ReadOnlyDictionary.Values.Select(id => inventory[id]).ToArray()
				});
			}
		}

		private void MatchStartAnalytics()
		{
			_services.AnalyticsService.MatchCalls.MatchStart();
		}
	}
}