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
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure Game Simulation State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class GameSimulationState
	{
		private readonly IStatechartEvent _simulationReadyEvent = new StatechartEvent("Simulation Ready Event");
		private readonly IStatechartEvent _gameEndedEvent = new StatechartEvent("Game Ended Event");
		private readonly IStatechartEvent _gameQuitEvent = new StatechartEvent("Game Quit Event");

		private readonly DeathmatchState _deathmatchState;
		private readonly BattleRoyaleState _battleRoyaleState;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService,
		                           Action<IStatechartEvent> statechartTrigger)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_deathmatchState = new DeathmatchState(gameDataProvider, services, uiService, statechartTrigger);
			_battleRoyaleState = new BattleRoyaleState(gameDataProvider, services, uiService, statechartTrigger);
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
			var gameEnded = stateFactory.Wait("Game Ended Screen");
			var gameResults = stateFactory.Wait("Game Results Screen");
			var gameRewards = stateFactory.Wait("Game Rewards Screen");

			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(_simulationReadyEvent).Target(modeCheck);
			startSimulation.OnExit(PrepareMatch);
			
			modeCheck.OnEnter(OpenAdventureWorldHud);
			modeCheck.Transition().Condition(IsDeathmatch).Target(deathmatch);
			modeCheck.Transition().Target(battleRoyale);
			modeCheck.OnExit(PlayMusic);

			deathmatch.Nest(_deathmatchState.Setup);
			deathmatch.Event(_gameEndedEvent).Target(gameEnded);
			deathmatch.Event(_gameQuitEvent).Target(final);
			deathmatch.OnExit(PublishMatchEnded);
			
			battleRoyale.Nest(_battleRoyaleState.Setup).Target(gameResults);
			battleRoyale.Event(_gameEndedEvent).Target(gameEnded);
			battleRoyale.Event(_gameQuitEvent).Target(final);
			battleRoyale.OnExit(PublishMatchEnded);

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

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<QuitGameClickedMessage>(OnQuitGameScreenClickedMessage);
			_services.MessageBrokerService.Subscribe<FtueEndedMessage>(OnFtueEndedMessage);
			
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

		private bool IsDeathmatch()
		{
			return _gameDataProvider.AdventureDataProvider.SelectedMapConfig.GameMode == GameMode.Deathmatch;
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
			_gameDataProvider.AdventureDataProvider.IncrementMapLevel();
		}

		private void StartSimulation()
		{
			var info = _gameDataProvider.AdventureDataProvider.SelectedMapConfig;
			var configs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var startParams = configs.GetDefaultStartParameters(info);

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
				ContinueButtonClicked = () => cacheActivity.Complete(),
				HomeButtonClicked = () => cacheActivity.Complete(),
			};

			_uiService.OpenUi<ResultsScreenPresenter, ResultsScreenPresenter.StateData>(data);
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

		private void PrepareMatch()
		{
			MatchStartAnalytics();
			SetPlayerMatchData();
			CloseLoadingScreen();
			
			_services.MessageBrokerService.Publish(new MatchReadyMessage());
		}

		private void SetPlayerMatchData()
		{
			var game = QuantumRunner.Default.Game;
			var info = _gameDataProvider.EquipmentDataProvider.GetLoadOutInfo();
			var position = _uiService.GetUi<MatchmakingLoadingScreenPresenter>().MapSelectionView.NormalizedSelectionPoint;
			
			game.SendPlayerData(game.GetLocalPlayers()[0], new RuntimePlayer
			{
				PlayerName = _gameDataProvider.PlayerDataProvider.Nickname,
				Skin = _gameDataProvider.PlayerDataProvider.CurrentSkin.Value,
				PlayerLevel = _gameDataProvider.PlayerDataProvider.Level.Value,
				NormalizedSpawnPosition = position.ToFPVector2(),
				Weapon = info.Weapon.Value,
				Gear = info.Gear.ConvertAll(item => (Equipment) item).ToArray()
			});
		}
		
		private void MatchStartAnalytics()
		{
			var config = _gameDataProvider.AdventureDataProvider.SelectedMapConfig;
			var totalPlayers = _services.NetworkService.QuantumClient.CurrentRoom.PlayerCount;
			
			var dictionary = new Dictionary<string, object> 
			{
				{"player_level", _gameDataProvider.PlayerDataProvider.Level.Value},
				{"total_players", totalPlayers},
				{"total_bots", config.PlayersLimit - totalPlayers},
				{"map_id", config.Id},
				{"map_name", config.Map},
			};

			_services.AnalyticsService.LogEvent("match_start", dictionary);
		}

		private void MatchEndAnalytics(Frame f, QuantumPlayerMatchData matchData, int totalPlayers, bool isQuitGame)
		{
			var config = _gameDataProvider.AdventureDataProvider.SelectedMapConfig;
			
			var analytics = new Dictionary<string, object>
			{
				{"player_level", _gameDataProvider.PlayerDataProvider.Level.Value},
				{"total_players", totalPlayers},
				{"total_kills_amount", matchData.Data.PlayersKilledCount},
				{"total_specials_used", matchData.Data.SpecialsUsedCount},
				{"total_deaths_amount", matchData.Data.DeathCount},
				{"suicides_amount", matchData.Data.SuicideCount},
				{"player_rank", matchData.PlayerRank },
				{"map_id", config.Id},
				{"end_state", isQuitGame ? "quit" : "ended" },
				{"match_time", f.Time.AsFloat}
			};
			
			_services.AnalyticsService.LogEvent("match_end", analytics);
		}
	}
}