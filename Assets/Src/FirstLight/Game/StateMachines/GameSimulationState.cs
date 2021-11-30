using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Quantum;
using Quantum.Commands;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		private readonly IStatechartEvent _simulationReadyEvent = new StatechartEvent("Simulation Ready Event");
		private readonly IStatechartEvent _localPlayerDeadEvent = new StatechartEvent("Local Player Dead");
		private readonly IStatechartEvent _localPlayerAliveEvent = new StatechartEvent("Local Player Alive");
		private readonly IStatechartEvent _gameEndedEvent = new StatechartEvent("Game Ended Event");
		private readonly IStatechartEvent _gameQuitEvent = new StatechartEvent("Game Quit Event");

		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly Dictionary<PlayerRef, Pair<int, int>> _killsDictionary = new Dictionary<PlayerRef, Pair<int, int>>();
		
		/// <summary>
		/// True if the player marked the game state to repeat again
		/// </summary>
		public bool IsPlayAgainMarked { get; private set; }
		
		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService,
		                           Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
	
			var gameplay = stateFactory.Nest("Game Play Running");
			var startSimulation = stateFactory.State("Start Simulation");
			var gameEnded = stateFactory.Wait("Game Ended Screen");
			var gameResults = stateFactory.Wait("Game Results Screen");
			var gameRewards = stateFactory.Wait("Game Rewards Screen");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(_simulationReadyEvent).Target(gameplay);
			startSimulation.OnEnter(PublishMatchReady);
			startSimulation.OnExit(CloseLoadingScreen);
			startSimulation.OnExit(PlayMusic);

			gameplay.OnEnter(OpenAdventureWorldHud);
			gameplay.Nest(GameplaySetup);
			gameplay.Event(_gameEndedEvent).Target(gameEnded);
			gameplay.Event(_gameQuitEvent).Target(final);
			gameplay.OnExit(CloseAdventureHud);
			gameplay.OnExit(PublishMatchEnded);

			gameEnded.OnEnter(SendGameplayDataAnalytics);
			gameEnded.WaitingFor(GameCompleteScreen).Target(gameResults);
			gameEnded.OnExit(CloseCompleteScreen);

			gameResults.WaitingFor(ResultsScreen).Target(gameRewards);
			gameResults.OnExit(CloseResultScreen);

			gameRewards.WaitingFor(RewardsScreen).Target(final);
			gameRewards.OnExit(CloseRewardScreen);

			final.OnEnter(StopSimulation);
			final.OnEnter(UnsubscribeEvents);
		}

		private void GameplaySetup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var startCheck = stateFactory.Choice("Start Game Check");
			var countdown = stateFactory.TaskWait("Countdown Hud");
			var ftueSpawning = stateFactory.State("FTUE spawning");
			var alive = stateFactory.State("Alive Hud");
			var dead = stateFactory.State("Dead Hud");

			initial.Transition().Target(startCheck);

			startCheck.Transition().Condition(IsFtueLevel).Target(ftueSpawning);
			startCheck.Transition().Target(countdown);
			startCheck.OnExit(SetPlayerMatchData);
			startCheck.OnExit(MatchStartAnalytics);
			
			ftueSpawning.Event(_localPlayerAliveEvent).Target(alive);
			ftueSpawning.OnExit(PublishMatchStarted);
			
			countdown.OnEnter(OpenAdventureHud);
			countdown.OnEnter(ShowCountdownHud);
			countdown.WaitingFor(Countdown).Target(alive);
			countdown.OnExit(PublishMatchStarted);
			
			alive.OnEnter(OpenControlsHud);
			alive.Event(_localPlayerDeadEvent).Target(dead);
			alive.OnExit(CloseControlsHud);

			dead.OnEnter(CloseAdventureHud);
			dead.OnEnter(OpenSpectatorHud);
			dead.Event(_localPlayerAliveEvent).OnTransition(OpenAdventureHud).Target(alive);
			dead.OnExit(CloseSpectatorHud);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);
			_services.MessageBrokerService.Subscribe<FtueEndedMessage>(OnFtueEndedMessage);
			
			QuantumEvent.SubscribeManual<EventOnLocalPlayerAlive>(this, OnLocalPlayerAlive);
			QuantumEvent.SubscribeManual<EventOnLocalPlayerDead>(this, OnLocalPlayerDead);
			QuantumEvent.SubscribeManual<EventOnPlayerKilledPlayer>(this, OnEventOnPlayerKilledPlayer);
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

		private void OnLocalPlayerAlive(EventOnLocalPlayerAlive callback)
		{
			_statechartTrigger(_localPlayerAliveEvent);
		}

		private void OnGameEnded(EventOnGameEnded callback)
		{
			_statechartTrigger(_gameEndedEvent);
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
			
			_statechartTrigger(_simulationReadyEvent);
		}

		
		private async void OnGameResync(CallbackGameResynced callback)
		{
			// paused on Start means waiting for Snapshot
			if (callback.Game.Session.IsPaused)
			{
				return;
			}

			// Delays one frame just to guarantee that the game objects are created before anything else
			await Task.Yield();
			
			_statechartTrigger(_simulationReadyEvent);
		}

		private void OnLocalPlayerDead(EventOnLocalPlayerDead callback)
		{
			_statechartTrigger(_localPlayerDeadEvent);
		}

		private void OnEventOnPlayerKilledPlayer(EventOnPlayerKilledPlayer callback)
		{
			var killerData = callback.PlayersMatchData[callback.PlayerKiller];
			var deadData = callback.PlayersMatchData[callback.PlayerDead];
			
			// "Key" = Number of times I killed this player, "Value" = number of times that player killed me.
			if (deadData.IsLocalPlayer || killerData.IsLocalPlayer)
			{
				var recordName = deadData.IsLocalPlayer ? killerData.Data.Player : deadData.Data.Player;
				
				if (!_killsDictionary.TryGetValue(recordName, out var recordPair))
				{
					recordPair = new Pair<int, int>();
					
					_killsDictionary.Add(recordName, recordPair);
				}
				
				recordPair.Key += deadData.IsLocalPlayer ? 0 : 1;
				recordPair.Value += deadData.IsLocalPlayer ? 1 : 0;
				
				_killsDictionary[recordName] = recordPair;
			}
		}

		private void OnQuitGameScreenClickedMessage(QuitGameClickedMessage message)
		{
			var data = new QuitGameDialogPresenter.StateData { ConfirmClicked = QuitGameConfirmedClicked };

			_uiService.OpenUi<QuitGameDialogPresenter, QuitGameDialogPresenter.StateData>(data);
		}

		private void OnFtueEndedMessage(FtueEndedMessage message)
		{
			SendGameplayData(false);
			
			_statechartTrigger(_gameQuitEvent);
		}
		
		private bool IsFtueLevel()
		{
			return _gameDataProvider.AdventureDataProvider.AdventureSelectedId.Value == 0;
		}

		private void QuitGameConfirmedClicked()
		{
			SendGameplayData(true);
			QuantumRunner.Default.Game.SendCommand(new PlayerQuitCommand());
			_statechartTrigger(_gameQuitEvent);
		}

		private void SendGameplayDataAnalytics()
		{
			SendGameplayData(false);
		}
		
		private void SendGameplayData(bool playerQuit)
		{
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var playersData = f.GetSingleton<GameContainer>().PlayersData;
			var data = new QuantumPlayerMatchData(f, playersData[game.GetLocalPlayers()[0]]);
			var totalPlayers = 0;

			for(var i = 0; i < playersData.Length; i++) 
			{
				if (playersData[i].IsValid && !f.Has<BotCharacter>(playersData[i].Entity))
				{
					totalPlayers++;
				}
			}

			_services.CommandService.ExecuteCommand(new GameCompleteRewardsCommand
			{
				PlayerMatchData = data,
				DidPlayerQuit = playerQuit
			});
			
			MatchEndAnalytics(f, data, totalPlayers, playerQuit);
			_gameDataProvider.AdventureDataProvider.IncrementLevel();
		}

		private void StartSimulation()
		{
			var info = _gameDataProvider.AdventureDataProvider.AdventureSelectedInfo;
			var configs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var startParams = configs.GetDefaultStartParameters(info);

			IsPlayAgainMarked = false;
			startParams.NetworkClient = _services.NetworkService.QuantumClient;

			QuantumRunner.StartGame(_services.NetworkService.UserId, startParams);
			_services.MessageBrokerService.Publish(new MatchSimulationStartedMessage());
		}

		private void StopSimulation()
		{
			_services.MessageBrokerService.Publish(new MatchSimulationEndedMessage());
			
			QuantumRunner.ShutdownAll();
		}

		private void PlayMusic()
		{
			_services.AudioFxService.PlayMusic(AudioId.AdventureMainLoop);
			_services.AudioFxService.PlayClip2D(AudioId.AdventureStart1);
		}

		private void PublishMatchStarted()
		{
			_services.MessageBrokerService.Publish(new MatchStartedMessage());
		}

		private void PublishMatchReady()
		{
			_services.MessageBrokerService.Publish(new MatchReadyMessage());
		}

		private void PublishMatchEnded()
		{
			_services.MessageBrokerService.Publish(new MatchEndedMessage());
		}

		private void CloseLoadingScreen()
		{
			if (_uiService.HasUiPresenter<MatchmakingLoadingScreenPresenter>())
			{
				_uiService.UnloadUi<MatchmakingLoadingScreenPresenter>();
			}
			else
			{
				_uiService.CloseUi<LoadingScreenPresenter>();
			}
		}
		
		private void OpenAdventureWorldHud()
		{
			_uiService.OpenUi<AdventureWorldHudPresenter>();
		}
		
		private void OpenAdventureHud()
		{
			_uiService.OpenUi<AdventureHudPresenter>();
		}

		private void CloseAdventureHud()
		{
			_uiService.CloseUi<AdventureHudPresenter>();
		}

		private void GameCompleteScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new GameCompleteScreenPresenter.StateData {ContinueClicked = ContinueClicked};

			_uiService.OpenUi<GameCompleteScreenPresenter, GameCompleteScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				cacheActivity.Complete();
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
				ContinueButtonClicked = PlayAgainClicked, 
				HomeButtonClicked = () => cacheActivity.Complete(),
			};

			_uiService.OpenUi<ResultsScreenPresenter, ResultsScreenPresenter.StateData>(data);
			
			void PlayAgainClicked()
			{
				IsPlayAgainMarked = true;
				cacheActivity.Complete();
			}
		}

		private void CloseResultScreen()
		{
			_uiService.CloseUi<ResultsScreenPresenter>();
		}

		private void RewardsScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new RewardsScreenPresenter.StateData { MainMenuClicked = ContinueClicked};

			_uiService.OpenUi<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				cacheActivity.Complete();
			}
		}

		private void CloseRewardScreen()
		{
			_uiService.CloseUi<RewardsScreenPresenter>();
		}
		
		private void OpenControlsHud()
		{
			_uiService.OpenUi<AdventureControlsHudPresenter>();
		}
		
		private void CloseControlsHud()
		{
			_uiService.CloseUi<AdventureControlsHudPresenter>();
		}
		
		private async Task PlayerLocalDissolve()
		{
			await Task.Delay((int)((GameConstants.DissolveDelay + GameConstants.DissolveDuration) * 1000));
		}
		
		private void OpenSpectatorHud()
		{
			var data = new AdventureSpectatorHudPresenter.StateData
			{
				KillerData = _killsDictionary
			};
			
			_uiService.OpenUi<AdventureSpectatorHudPresenter, AdventureSpectatorHudPresenter.StateData>(data);
		}
		
		private void CloseSpectatorHud()
		{
			_uiService.CloseUi<AdventureSpectatorHudPresenter>();
		}

		private async Task Countdown()
		{
			await Task.Delay(3000);
		}
		
		private void ShowCountdownHud()
		{
			_uiService.OpenUi<GameCountdownScreenPresenter>();
		}

		private void SetPlayerMatchData()
		{
			var game = QuantumRunner.Default.Game;
			var info = _gameDataProvider.EquipmentDataProvider.GetLoadOutInfo();
			
			_killsDictionary.Clear();
			
			game.SendPlayerData(game.GetLocalPlayers()[0], new RuntimePlayer
			{
				PlayerName = _gameDataProvider.PlayerDataProvider.Nickname,
				Skin = _gameDataProvider.PlayerDataProvider.CurrentSkin.Value,
				Weapon = info.Weapon.Value,
				Gear = info.Gear.ConvertAll(item => (Equipment) item).ToArray(),
				PlayerLevel = _gameDataProvider.PlayerDataProvider.Level.Value
			});
		}
		
		private void MatchStartAnalytics()
		{
			var info = _gameDataProvider.AdventureDataProvider.AdventureSelectedInfo;
			var totalPlayers = _services.NetworkService.QuantumClient.CurrentRoom.PlayerCount;
			
			var dictionary = new Dictionary<string, object> 
			{
				{"player_level", _gameDataProvider.PlayerDataProvider.Level.Value},
				{"total_players", totalPlayers},
				{"total_bots", info.Config.TotalFightersLimit - totalPlayers},
				{"map_id", info.Config.Id},
				{"map_name", info.Config.Map},
			};

			_services.AnalyticsService.LogEvent("match_start", dictionary);
		}

		private void MatchEndAnalytics(Frame f, QuantumPlayerMatchData matchData, int totalPlayers, bool isQuitGame)
		{
			var info = _gameDataProvider.AdventureDataProvider.AdventureSelectedInfo;
			
			var analytics = new Dictionary<string, object>
			{
				{"player_level", _gameDataProvider.PlayerDataProvider.Level.Value},
				{"total_players", totalPlayers},
				{"total_kills_amount", matchData.Data.PlayersKilledCount},
				{"total_specials_used", matchData.Data.SpecialsUsedCount},
				{"total_deaths_amount", matchData.Data.DeathCount},
				{"suicides_amount", matchData.Data.SuicideCount},
				{"player_rank", matchData.PlayerRank },
				{"map_id", info.Config.Id},
				{"end_state", isQuitGame ? "quit" : "ended" },
				{"match_time", f.Time.AsFloat}
			};
			
			_services.AnalyticsService.LogEvent("match_end", analytics);
		}
	}
}