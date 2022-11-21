using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
using Newtonsoft.Json;
using Quantum;
using Quantum.Commands;
using UnityEngine;
using UnityEngine.SceneManagement;
using PlayerMatchData = FirstLight.Game.Services.PlayerMatchData;

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
		private readonly IAssetAdderService _assetAdderService;
		private IMatchServices _matchServices;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly IGameNetworkService _network;
		private readonly IGameBackendNetworkService _networkService;

		private int _lastTrophyChange = 0;
		private uint _trophiesBeforeLastChange = 0;

		public GameSimulationState(IGameDataProvider gameDataProvider, IGameServices services, IGameBackendNetworkService networkService, 
								   IGameUiService uiService, Action<IStatechartEvent> statechartTrigger, IAssetAdderService assetAdderService)
		{
			_gameDataProvider = gameDataProvider;
			_services = services;
			_networkService = networkService;
			_uiService = uiService;
			_statechartTrigger = statechartTrigger;
			_deathmatchState = new DeathmatchState(gameDataProvider, services, uiService, statechartTrigger);
			_battleRoyaleState = new BattleRoyaleState(services, uiService, statechartTrigger);
			_assetAdderService = assetAdderService;
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
			var winners = stateFactory.Wait("Winners Screen");
			var gameResults = stateFactory.Wait("Game Results Screen");
			var rewardsCheck = stateFactory.Choice("Rewards Choice");
			var trophiesCheck = stateFactory.Choice("Trophies Choice");
			var quitCheck = stateFactory.Choice("Quit Check");
			var gameRewards = stateFactory.Wait("Game Rewards Screen");
			var trophiesGainLoss = stateFactory.Wait("Trophies Gain Loss Screen");
			var disconnectedPlayerCheck = stateFactory.Choice("Disconnected Player Check");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCritical = stateFactory.State("Disconnected Critical");
			
			initial.Transition().Target(startSimulation);
			initial.OnExit(SubscribeEvents);
			initial.OnExit(OpenLowConnectionScreen);

			startSimulation.OnEnter(StartSimulation);
			startSimulation.Event(SimulationStartedEvent).Target(modeCheck);
			startSimulation.Event(NetworkState.LeftRoomEvent).Target(final);

			modeCheck.OnEnter(OpenAdventureWorldHud);
			modeCheck.Transition().Condition(ShouldUseDeathmatchSM).Target(deathmatch);
			modeCheck.Transition().Condition(ShouldUseBattleRoyaleSM).Target(battleRoyale);
			modeCheck.Transition().Target(battleRoyale);

			deathmatch.Nest(_deathmatchState.Setup).OnTransition(() => MatchEndAnalytics(false)).Target(gameEnded);
			deathmatch.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringSimulation).Target(disconnectedPlayerCheck);
			deathmatch.Event(MatchEndedEvent).OnTransition(() => MatchEndAnalytics(false)).Target(gameEnded);
			deathmatch.Event(MatchQuitEvent).OnTransition(() => MatchEndAnalytics(true)).Target(quitCheck);
			deathmatch.OnExit(CleanUpMatch);
			deathmatch.OnExit(PublishMatchEnded);

			battleRoyale.Nest(_battleRoyaleState.Setup).OnTransition(() => MatchEndAnalytics(false)).Target(gameEnded);
			battleRoyale.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringSimulation).Target(disconnectedPlayerCheck);
			battleRoyale.Event(MatchEndedEvent).OnTransition(() => MatchEndAnalytics(false)).Target(gameEnded);
			battleRoyale.Event(MatchQuitEvent).OnTransition(() => MatchEndAnalytics(true)).Target(quitCheck);
			battleRoyale.OnExit(CleanUpMatch);
			battleRoyale.OnExit(PublishMatchEnded);
			
			disconnectedPlayerCheck.Transition().Condition(IsSoloGame).OnTransition(OpenDisconnectedMatchEndDialog).Target(final);
			disconnectedPlayerCheck.Transition().Target(disconnected);
			
			disconnected.OnEnter(StopSimulation);
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(startSimulation);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(disconnectedCritical);

			disconnectedCritical.OnEnter(NotifyCriticalDisconnection);
			
			quitCheck.Transition().Condition(IsCustomMatch).Target(final);
			quitCheck.Transition().Condition(IsSpectator).Target(final);
			quitCheck.Transition().Target(gameEnded);
			
			gameEnded.OnEnter(OpenGameCompleteScreen);
			gameEnded.Event(GameCompleteExitEvent).Target(winners);

			winners.OnEnter(StopSimulation);
			winners.OnEnter(UnsubscribeEvents);
			winners.OnEnter(UnloadAllMatchAssets);
			winners.OnEnter(UnloadMatchAssetConfigs);
			winners.WaitingFor(OpenWinnersScreen).Target(gameResults);
			
			gameResults.WaitingFor(ResultsScreen).Target(trophiesCheck);

			trophiesCheck.Transition().Condition(HasTrophyChangeToDisplay).Target(trophiesGainLoss);
			trophiesCheck.Transition().Target(rewardsCheck);

			trophiesGainLoss.WaitingFor(OpenTrophiesScreen).Target(rewardsCheck);

			rewardsCheck.Transition().Condition(HasRewardsToClaim).Target(gameRewards);
			rewardsCheck.Transition().Target(final);

			gameRewards.WaitingFor(OpenRewardsScreen).Target(final);
			
			final.OnEnter(UnloadMatchEnd);
		}

		private void UnloadMatchEnd()
		{
			var configProvider = _services.ConfigsProvider;
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MainMenuAssetConfigs>());
			MainInstaller.CleanDispose<IMatchServices>();
		}

		private void UpdateMatchEndData()
		{
			var game = QuantumRunner.Default.Game;
			var frame = game.Frames.Verified;
			
			var quantumPlayerMatchData = frame.GetSingleton<GameContainer>().GetPlayersMatchData(frame, out _);

			_matchServices.MatchDataService.QuantumPlayerMatchData = quantumPlayerMatchData;

			_matchServices.MatchDataService.PlayerMatchData = new Dictionary<PlayerRef, PlayerMatchData>();
			
			foreach (var quantumPlayerData in quantumPlayerMatchData)
			{
				Equipment weapon = default;
				List<Equipment> loadout = null;

				var playerRuntimeData = frame.GetPlayerData(quantumPlayerData.Data.Player);
				if (playerRuntimeData != null)
				{
					weapon = playerRuntimeData.Weapon;
					loadout = playerRuntimeData.Loadout.ToList();
				}

				var playerData = new PlayerMatchData(quantumPlayerData.Data.Player, quantumPlayerData, weapon, loadout??new List<Equipment>());
				_matchServices.MatchDataService.PlayerMatchData.Add(playerData.PlayerRef, playerData);
			}

			_matchServices.MatchDataService.ShowUIStandingsExtraInfo =
				frame.Context.GameModeConfig.ShowUIStandingsExtraInfo;
			_matchServices.MatchDataService.LocalPlayer = QuantumRunner.Default.Game.GetLocalPlayers()[0];

			GiveMatchRewards();
		}

		private bool IsSoloGame()
		{
			return _services.NetworkService.LastMatchPlayers.Count == 1;
		}
		
		private void OnDisconnectDuringSimulation()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Simulation;
		}
		
		private void NotifyCriticalDisconnection()
		{
			_statechartTrigger(NetworkState.PhotonCriticalDisconnectedEvent);
		}

		private void OpenLowConnectionScreen()
		{
			_uiService.OpenScreen<LowConnectionPresenter>();
		}

		private void OpenDisconnectedMatchEndDialog()
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			
			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info, ScriptLocalization.MainMenu.DisconnectedMatchEndInfo.ToUpper(), false, confirmButton);
		}

		private void SubscribeEvents()
		{
			_matchServices = MainInstaller.Resolve<IMatchServices>();
			
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
		
		private bool IsCustomMatch()
		{
			return _services.NetworkService.QuantumClient.CurrentRoom.GetMatchType() == MatchType.Custom;
		}

		private bool HasRewardsToClaim()
		{
			return _gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0;
		}

		private bool HasTrophyChangeToDisplay()
		{
			return _lastTrophyChange != 0;
		}

		private bool ShouldUseDeathmatchSM()
		{
			return _services.NetworkService.CurrentRoomGameModeConfig.Value.AudioStateMachine ==
			       AudioStateMachine.Deathmatch;
		}
		
		private bool ShouldUseBattleRoyaleSM()
		{
			return _services.NetworkService.CurrentRoomGameModeConfig.Value.AudioStateMachine ==
			       AudioStateMachine.BattleRoyale;
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

			PublishMatchStartedMessage(callback.Game, false);
			_statechartTrigger(SimulationStartedEvent);
		}

		private async void OnGameResync(CallbackGameResynced callback)
		{
			// Delays one frame just to guarantee that the game objects are created before anything else
			await Task.Yield();

			PublishMatchStartedMessage(callback.Game, true);
			_statechartTrigger(SimulationStartedEvent);
		}

		private void OnGameEnded(EventOnGameEnded callback)
		{
			_statechartTrigger(MatchEndedEvent);
		}

		private void OnQuitGameScreenClickedMessage(QuitGameClickedMessage message)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = QuitGameConfirmedClicked
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.confirmation,
				ScriptLocalization.AdventureMenu.AreYouSureQuit,
				true, confirmButton);
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
			if (IsSpectator()) return;
			
			var game = QuantumRunner.Default.Game;
			var f = game.Frames.Verified;
			var command = new EndOfGameCalculationsCommand();
			command.FromFrame(f, new QuantumValues()
			{
				ExecutingPlayer = game.GetLocalPlayers()[0],
				MatchType = _services.NetworkService.QuantumClient.CurrentRoom.GetMatchType()
			});
			_services.CommandService.ExecuteCommand(command);
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

			var startParams = configs.GetDefaultStartParameters(startPlayersCount, IsSpectator(), new FrameSnapshot());
			
			// Unused for now, once local snapshot issues are ironed out, resyncing solo games can be readded
			if (!_services.NetworkService.IsJoiningNewMatch && _services.NetworkService.LastMatchPlayers.Count == 1)
			{
				startParams = configs.GetDefaultStartParameters(_services.NetworkService.LastMatchPlayers.Count, IsSpectator(), _matchServices.FrameSnapshotService.GetLastStoredMatchSnapshot());
			}

			startParams.NetworkClient = client;
			
			QuantumRunner.StartGame(_services.NetworkService.UserId, startParams);
			_services.MessageBrokerService.Publish(new MatchSimulationStartedMessage());
		}

		private void StopSimulation()
		{
			UpdateMatchEndData();
			_services.MessageBrokerService.Publish(new MatchSimulationEndedMessage());
			QuantumRunner.ShutdownAll();
		}
		
		private void CleanUpMatch()
		{
			_services.VfxService.DespawnAll();
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

			_uiService.OpenScreen<GameCompleteScreenPresenter, GameCompleteScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				_statechartTrigger(GameCompleteExitEvent);
			}
		}

		private async void OpenWinnersScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new WinnersScreenPresenter.StateData {ContinueClicked = () => cacheActivity.Complete()};

			await _uiService.OpenScreen<WinnersScreenPresenter, WinnersScreenPresenter.StateData>(data);
		}

		private async void ResultsScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new ResultsScreenPresenter.StateData
			{
				ContinueButtonClicked = () => cacheActivity.Complete(),
				HomeButtonClicked = () => cacheActivity.Complete(),
			};
			
			await _uiService.OpenScreen<ResultsScreenPresenter, ResultsScreenPresenter.StateData>(data);
		}

		private async void OpenRewardsScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new RewardsScreenPresenter.StateData {MainMenuClicked = ContinueClicked};

			await _uiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				cacheActivity.Complete();
			}
		}

		private async void OpenTrophiesScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new TrophiesScreenPresenter.StateData
			{
				ExitTrophyScreen = ContinueClicked,
				LastTrophyChange = _lastTrophyChange,
				TrophiesBeforeLastChange = _trophiesBeforeLastChange
			};

			await _uiService.OpenScreen<TrophiesScreenPresenter, TrophiesScreenPresenter.StateData>(data);

			void ContinueClicked()
			{
				cacheActivity.Complete();
			}
		}
		private void UnloadMatchAssetConfigs()
		{
			var configProvider = _services.ConfigsProvider;
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets<EquipmentRarity, GameObject>(false);
			_services.AssetResolverService.UnloadAssets<IndicatorVfxId, GameObject>(false);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MatchAssetConfigs>());
			_assetAdderService.AddConfigs(configProvider.GetConfig<MainMenuAssetConfigs>());
		}
		

		private async void UnloadAllMatchAssets()
		{
			var scene = SceneManager.GetActiveScene();
			var configProvider = _services.ConfigsProvider;
			
			_uiService.UnloadUiSet((int) UiSetId.MatchUi);
			_services.AudioFxService.DetachAudioListener();
			await _services.AssetResolverService.UnloadSceneAsync(scene);
			
			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);

			Resources.UnloadUnusedAssets();
		}

		private void PublishMatchStartedMessage(QuantumGame game, bool isResync)
		{
			if (_services.NetworkService.IsJoiningNewMatch)
			{
				MatchStartAnalytics();
				SetPlayerMatchData(game);
			}

			_services.MessageBrokerService.Publish(new MatchStartedMessage { Game = game, IsResync = isResync });
		}

		private void SetPlayerMatchData(QuantumGame game)
		{
			var info = _gameDataProvider.PlayerDataProvider.PlayerInfo;
			var loadout = _gameDataProvider.EquipmentDataProvider.Loadout;
			var inventory = _gameDataProvider.EquipmentDataProvider.Inventory;
			var f = game.Frames.Verified;
			var spawnPosition = _services.MatchmakingService.NormalizedMapSelectedPosition;

			var spawnWithloadout = f.Context.GameModeConfig.SpawnWithGear || f.Context.GameModeConfig.SpawnWithWeapon;

			var finalLoadOut = new List<Equipment>();
			foreach(var item in loadout.ReadOnlyDictionary.Values.ToList())
			{
				var itemId = inventory[item.Id];
				if(itemId.GameId.IsInGroup(GameIdGroup.Gear) && !f.Context.GameModeConfig.SpawnWithGear)
				{
					continue;
				}
				else if (itemId.GameId.IsInGroup(GameIdGroup.Weapon) && 
					(!f.Context.GameModeConfig.SpawnWithWeapon || f.Context.TryGetMutatorByType(MutatorType.HammerTime, out _)))
				{
					continue;
				}
				finalLoadOut.Add(inventory[item.Id]);
			}

			if (!IsSpectator())
			{
				game.SendPlayerData(game.GetLocalPlayers()[0], new RuntimePlayer
				{
					PlayerId = _gameDataProvider.AppDataProvider.PlayerId,
					PlayerName = _gameDataProvider.AppDataProvider.DisplayNameTrimmed,
					Skin = info.Skin,
					DeathMarker = info.DeathMarker,
					PlayerLevel = info.Level,
					PlayerTrophies = info.TotalTrophies,
					NormalizedSpawnPosition = spawnPosition.ToFPVector2(),
					Loadout = spawnWithloadout ? 
						finalLoadOut.ToArray() : loadout.ReadOnlyDictionary.Values.Select(id => inventory[id]).ToArray()
				});
			}
		}

		private void MatchStartAnalytics()
		{
			_services.AnalyticsService.MatchCalls.MatchStart();
		}
	}
}