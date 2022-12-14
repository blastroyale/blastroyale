using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
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
		public static readonly IStatechartEvent AllPlayersReadyEvent = new StatechartEvent("All Players Ready");
		public static readonly IStatechartEvent MatchUnloadedEvent = new StatechartEvent("Match Unloaded Ready");
		public static readonly IStatechartEvent MatchEndedEvent = new StatechartEvent("Game Ended Event");
		public static readonly IStatechartEvent MatchQuitEvent = new StatechartEvent("Game Quit Event");
		public static readonly IStatechartEvent MatchCompleteExitEvent = new StatechartEvent("Game Complete Exit Event");
		public static readonly IStatechartEvent LeaveRoomClicked = new StatechartEvent("Leave Room Requested");
		public static readonly IStatechartEvent MatchStateEndingEvent = new StatechartEvent("Match Flow Leaving Event");
		
		private readonly GameSimulationState _gameSimulationState;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _dataProvider;
		private readonly IGameBackendNetworkService _networkService;
		private readonly IGameUiService _uiService;
		private readonly IDataService _dataService;
		private readonly IAssetAdderService _assetAdderService;
		private IMatchServices _matchServices;
		private bool _arePlayerAssetsLoaded = false;
		private Action<IStatechartEvent> _statechartTrigger;
		
		public MatchState(IGameServices services, IDataService dataService, IGameBackendNetworkService networkService, IGameUiService uiService, IGameDataProvider gameDataProvider, 
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
			var loading = stateFactory.TaskWait("Loading Assets");
			var roomCheck = stateFactory.Choice("Room Check");
			var matchmaking = stateFactory.State("Matchmaking");
			var playerReadyCheck = stateFactory.Choice("Player Ready Check");
			var playerReadyWait = stateFactory.State("Player Ready Wait");
			var gameSimulation = stateFactory.Nest("Game Simulation");
			var unloadToFinal = stateFactory.TaskWait("Unload Match Assets");
			var disconnected = stateFactory.State("Disconnected");
			var postDisconnectCheck = stateFactory.Choice("Post Reload Check");
			var gameEndedChoice = stateFactory.Choice("Game Ended Check");
			var gameEnded = stateFactory.State("Game Ended Screen");
			var transitionToWinners = stateFactory.Wait("Unload to Game End UI");
			var transitionToGameResults = stateFactory.Wait("Unload to Game Results UI");
			var winners = stateFactory.Wait("Winners Screen");
			var gameResults = stateFactory.Wait("Game Results Screen");
			var matchStateEnding = stateFactory.TaskWait("Publish Wait Match State Ending");
			
			initial.Transition().Target(loading);
			initial.OnExit(SubscribeEvents);
			
			loading.OnEnter(OpenMatchmakingScreen);
			loading.WaitingFor(LoadMatchAssets).Target(roomCheck);
			loading.OnExit(CloseSwipeTransition);
			
			roomCheck.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(unloadToFinal);
			roomCheck.Transition().Condition(IsRoomClosed).Target(playerReadyCheck);
			roomCheck.Transition().Target(matchmaking);

			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(disconnected);
			matchmaking.Event(NetworkState.RoomClosedEvent).Target(playerReadyCheck);
			matchmaking.Event(LeaveRoomClicked).Target(unloadToFinal);

			playerReadyCheck.OnEnter(CheckPlayerAssetsLoaded);
			playerReadyCheck.Transition().Condition(AreAllPlayersReady).Target(gameSimulation);
			playerReadyCheck.Transition().Target(playerReadyWait);
			
			playerReadyWait.OnEnter(PreloadPlayerMatchAssets);
			playerReadyWait.OnEnter(DismissGenericPopups);
			playerReadyWait.Event(AllPlayersReadyEvent).Target(gameSimulation);
			playerReadyWait.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(unloadToFinal);
			playerReadyWait.Event(NetworkState.LeftRoomEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(unloadToFinal);
			
			gameSimulation.Nest(_gameSimulationState.Setup).OnTransition(() => HandleSimulationEnd(false)).Target(gameEndedChoice);
			gameSimulation.Event(MatchEndedEvent).OnTransition(() => HandleSimulationEnd(false)).Target(gameEndedChoice);
			gameSimulation.Event(MatchQuitEvent).OnTransition(() => HandleSimulationEnd(true)).Target(unloadToFinal);
			gameSimulation.Event(NetworkState.PhotonCriticalDisconnectedEvent).OnTransition(OnDisconnectDuringSimulation).Target(disconnected);

			gameEndedChoice.Transition().Condition(HasLeftBeforeMatchEnded).Target(transitionToGameResults);
			gameEndedChoice.Transition().Target(gameEnded);
			
			gameEnded.OnEnter(OpenWinnerScreen);
			gameEnded.Event(MatchCompleteExitEvent).Target(transitionToWinners);
			
			transitionToWinners.WaitingFor(UnloadMatchAndTransition).Target(winners);
			
			transitionToGameResults.WaitingFor(UnloadMatchAndTransition).Target(gameResults);
			
			winners.WaitingFor(OpenWinnersScreen).Target(gameResults);

			gameResults.OnEnter(CloseSwipeTransition);
			gameResults.WaitingFor(OpenLeaderboardAndRewardsScreen).Target(matchStateEnding);
			gameResults.OnExit(UnloadMainMenuAssetConfigs);
			gameResults.OnExit(DisposeMatchServices);
			gameResults.OnExit(OpenLoadingScreen);

			disconnected.OnEnter(OpenDisconnectedScreen);
			disconnected.Event(NetworkState.JoinedRoomEvent).Target(postDisconnectCheck);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(unloadToFinal);
			disconnected.Event(NetworkState.DcScreenBackEvent).Target(unloadToFinal);
			
			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringMatchmaking).OnTransition(OnReloadToMatchmaking).Target(matchmaking);
			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringSimulation).OnTransition(CloseCurrentScreen).Target(playerReadyCheck);
			postDisconnectCheck.Transition().OnTransition(CloseCurrentScreen).Target(unloadToFinal);

			unloadToFinal.OnEnter(OpenLoadingScreen);
			unloadToFinal.WaitingFor(UnloadAllMatchAssets).Target(matchStateEnding);
			unloadToFinal.OnExit(DisposeMatchServices);
			
			matchStateEnding.WaitingFor(PublishMatchStateEnding).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private bool HasLeftBeforeMatchEnded()
		{
			return _matchServices.MatchEndDataService.LeftBeforeMatchFinished;
		}

		private bool IsSpectator()
		{
			return _services.NetworkService.QuantumClient.LocalPlayer.IsSpectator();
		}
		
		private void OpenWinnerScreen()
		{
			var data = new WinnerScreenPresenter.StateData
			{
				ContinueClicked = () => _statechartTrigger(MatchCompleteExitEvent)
			};

			_uiService.OpenScreen<WinnerScreenPresenter, WinnerScreenPresenter.StateData>(data);
		}
		
		private async void OpenWinnersScreen(IWaitActivity activity)
		{
			var cacheActivity = activity;
			var data = new WinnersScreenPresenter.StateData {ContinueClicked = () => cacheActivity.Complete()};

			await _uiService.OpenScreenAsync<WinnersScreenPresenter, WinnersScreenPresenter.StateData>(data);

			CloseSwipeTransition();
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

		private bool IsRoomClosed()
		{
			return !_networkService.QuantumClient.CurrentRoom.IsOpen;
		}

		private void OnReloadToMatchmaking()
		{
			PublishCoreAssetsLoadedMessage();
			OpenMatchmakingScreen();
		}

		private void SubscribeEvents()
		{
			QuantumEvent.SubscribeManual<EventOnGameEnded>(this, OnGameEnded);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
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
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.FinalPreload;

			if (_uiService.HasUiPresenter <CustomLobbyScreenPresenter>())
			{
				_uiService.CloseUi<CustomLobbyScreenPresenter>();
			}
		}

		private void OnDisconnectDuringSimulation()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Simulation;
		}

		private void CloseSwipeTransition()
		{
			if (_uiService.HasUiPresenter<SwipeScreenPresenter>())
			{
				_uiService.CloseUi<SwipeScreenPresenter>(true);
			}
		}

		private async void OpenMatchmakingScreen()
		{
			if (_networkService.QuantumClient.CurrentRoom.IsMatchmakingRoom())
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
			
			_services.AnalyticsService.MatchCalls.MatchInitiate();
		}

		private void OpenDisconnectedScreen()
		{
			var data = new DisconnectedScreenPresenter.StateData
			{
				ReconnectClicked = () => _services.MessageBrokerService.Publish(new AttemptManualReconnectionMessage()),
				BackClicked = () => _statechartTrigger(NetworkState.DcScreenBackEvent)
			};

			_uiService.OpenScreen<DisconnectedScreenPresenter, DisconnectedScreenPresenter.StateData>(data);
		}
		
		private async void OpenLoadingScreen()
		{
			_uiService.CloseCurrentScreen();
			await _uiService.OpenUiAsync<LoadingScreenPresenter>();
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

		private bool AreAllPlayersReady()
		{
			return _services.NetworkService.QuantumClient.CurrentRoom.AreAllPlayersReady() && _arePlayerAssetsLoaded;
		}
		
		private void CheckPlayerAssetsLoaded()
		{
			if (!_arePlayerAssetsLoaded)
			{
				_services.MessageBrokerService.Publish(new AssetReloadRequiredMessage());
			}
		}

		private List<Task> LoadQuantumAssets(string map)
		{
			var assets = UnityDB.CollectAddressableAssets();
			var tasks = new List<Task>(assets.Count);

			foreach (var asset in assets)
			{
				if (asset.Item1.StartsWith("Maps/") && !asset.Item1.Contains(map))
				{
					continue;
				}

				tasks.Add(_assetAdderService.LoadAssetAsync<AssetBase>(asset.Item1));
			}

			return tasks;
		}

		private async Task LoadMatchAssets()
		{
			var tasks = new List<Task>();
			var config = _services.NetworkService.CurrentRoomMapConfig.Value;
			var gameModeConfig = _services.NetworkService.CurrentRoomGameModeConfig.Value;
			var mutatorIds = _services.NetworkService.CurrentRoomMutatorIds;
			var map = config.Map.ToString();
			var entityService = new GameObject(nameof(EntityViewUpdaterService)).AddComponent<EntityViewUpdaterService>();
			_matchServices = new MatchServices(entityService, _services, _dataProvider, _dataService);
			
			MainInstaller.Bind<IMatchServices>(_matchServices);
			
			var runnerConfigs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var sceneTask = _services.AssetResolverService.LoadSceneAsync($"Scenes/{map}.unity", LoadSceneMode.Additive);
			
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MatchAssetConfigs>());
			runnerConfigs.SetRuntimeConfig(gameModeConfig, config, mutatorIds);

			tasks.Add(sceneTask);
			tasks.Add(_assetAdderService.LoadAllAssets<IndicatorVfxId, GameObject>());
			tasks.Add(_assetAdderService.LoadAllAssets<EquipmentRarity, GameObject>());
			tasks.AddRange(LoadQuantumAssets(map));
			tasks.AddRange(PreloadGameAssets());
			tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.MatchUi));
			
			// TODO: FIX THIS
			switch (_services.NetworkService.CurrentRoomGameModeConfig.Value.Id)
			{
				case "Deathmatch" : tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.DeathMatchMatchUi));
					break;
				case "BattleRoyale" : tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.BattleRoyaleMatchUi));
					break;
			}

			await Task.WhenAll(tasks);

			SceneManager.SetActiveScene(sceneTask.Result);

			await Task.WhenAll(PreloadMapAssets());
			
			PublishCoreAssetsLoadedMessage();

#if UNITY_EDITOR
			SetQuantumMultiClient(runnerConfigs, entityService);
#endif
		}

		private async Task UnloadAllMatchAssets()
		{
			var scene = SceneManager.GetActiveScene();
			var configProvider = _services.ConfigsProvider;
			
			_uiService.UnloadUiSet((int) UiSetId.MatchUi);
			_services.AudioFxService.DetachAudioListener();

			await _services.AssetResolverService.UnloadSceneAsync(scene);

			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets<EquipmentRarity, GameObject>(false);
			_services.AssetResolverService.UnloadAssets<IndicatorVfxId, GameObject>(false);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MatchAssetConfigs>());

			Resources.UnloadUnusedAssets();

			_arePlayerAssetsLoaded = false;
			
			_statechartTrigger(MatchUnloadedEvent);
		}
		
		private async Task PublishMatchStateEnding()
		{
			// Workaround to triggering statechart events on enter/exit
			// Necessary for audio to play at correct time, but this can't be called OnEnter or OnExit, or the 
			// state machine ends up working very strangely.
			_statechartTrigger(MatchStateEndingEvent);
			
			await Task.Yield();
		}
		
		private void HandleSimulationEnd(bool playerQuit)
		{
			if (playerQuit)
			{
				_services.MessageBrokerService.Publish(new LeftBeforeMatchFinishedMessage());
				StopSimulation();
			}
			
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
		
		private async void UnloadMatchAndTransition(IWaitActivity activity)
		{
			await _uiService.OpenUiAsync<SwipeScreenPresenter>();
			
			// Delay to let the swipe animation finish its intro without being choppy
			await Task.Delay(GameConstants.Visuals.SCREEN_SWIPE_TRANSITION_MS);

			StopSimulation();

			// Yield for a frame to give time for Quantum to unload all the memory before all assets are unloaded from Unity
			await Task.Yield();
			await UnloadAllMatchAssets();

			LoadMainMenuAssetConfigs();
			
			// Delay to make sure we can read the swipe transition message even if the rest is too fast
			await Task.Delay(1000);

			activity.Complete();
		}
		
		private void LoadMainMenuAssetConfigs()
		{
			// Load the Menu asset configs to show the player skins and visuals in the end game menus
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<MainMenuAssetConfigs>());
		}
		
		private void UnloadMainMenuAssetConfigs()
		{
			// Unload the assets loaded in UnloadMatchAssets method
			_services.AssetResolverService.UnloadAssets(true, _services.ConfigsProvider.GetConfig<MainMenuAssetConfigs>());
		}
		
		private void StopSimulation()
		{
			_services.MessageBrokerService.Publish(new MatchSimulationEndedMessage());
			QuantumRunner.ShutdownAll();
		}
		
		private void DisposeMatchServices()
		{
			MainInstaller.CleanDispose<IMatchServices>();
		}

		private void PublishCoreAssetsLoadedMessage()
		{
			_services.MessageBrokerService.Publish(new CoreMatchAssetsLoadedMessage());
		}
		
		private IEnumerable<Task> PreloadMapAssets()
		{
			var tasks = new List<Task>();

			// Preload spawners
			var spawners = Object.FindObjectsOfType<EntityComponentCollectablePlatformSpawner>();
			foreach (var spawner in spawners)
			{
				var id = (GameId) spawner.Prototype.GameId.Value;
				if (id != GameId.Random)
				{
					tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(id, true, false));
				}
			}
			
			return tasks;
		}

		private IEnumerable<Task> PreloadGameAssets()
		{
			var tasks = new List<Task>();
			
			// Preload Hammer
			tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(GameId.Hammer, true, false));

			// Preload collectables
			foreach (var id in GameIdGroup.Consumable.GetIds())
			{
				tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(id, true, false));
			}

			// Preload indicator VFX
			for (var i = 1; i < (int) IndicatorVfxId.TOTAL; i++)
			{
				tasks.Add(_services.AssetResolverService.RequestAsset<IndicatorVfxId, GameObject>((IndicatorVfxId) i,
					          true,
					          false));
			}

			// Preload bot items
			foreach (var id in GameIdGroup.BotItem.GetIds())
			{
				tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(id, true, false));
			}
			
			// Preload material VFX
			for (var i = 1; i < (int) MaterialVfxId.TOTAL; i++)
			{
				tasks.Add(_services.AssetResolverService.RequestAsset<MaterialVfxId, Material>((MaterialVfxId) i,
					          true,
					          false));
			}

			// Preload chests
			foreach (var id in GameIdGroup.Chest.GetIds())
			{
				tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(id, true, false));
			}
			
			// Preload Audio
			tasks.Add(_services.AudioFxService.LoadAudioClips(_services.ConfigsProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary));
			
			return tasks;
		}
		
		private void DismissGenericPopups()
		{
			_services.GenericDialogService.CloseDialog();
		}

		private async void PreloadPlayerMatchAssets()
		{
			_services.MessageBrokerService.Publish(new StartedFinalPreloadMessage());

			var tasks = new List<Task>();

			// Preload players assets
			foreach (var player in _services.NetworkService.QuantumClient.CurrentRoom.Players)
			{
				var preloadPropsInRoom = (int[]) player.Value.CustomProperties[GameConstants.Network.PLAYER_PROPS_PRELOAD_IDS];
				var preloadIds = new HashSet<GameId>();

				foreach (var prop in preloadPropsInRoom)
				{
					preloadIds.Add((GameId) prop);
				}
				
				foreach (var id in preloadIds)
				{
					tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>(id, true,
						          false));
				}
			}

			await Task.WhenAll(tasks);

			_arePlayerAssetsLoaded = true;
			_services.MessageBrokerService.Publish(new AllMatchAssetsLoadedMessage());
		}

		private void SetQuantumMultiClient(QuantumRunnerConfigs runnerConfigs, EntityViewUpdaterService entityService)
		{
			if (!SROptions.Current.IsMultiClient)
			{
				return;
			}

			var multiClient = Resources.Load<QuantumMultiClientRunner>(nameof(QuantumMultiClientRunner));

			multiClient.RuntimeConfig = runnerConfigs.RuntimeConfig;
			multiClient.EntityViewUpdaterTemplate = entityService;
			SROptions.Current.IsMultiClient = false;

			for (var i = 0; i < multiClient.RuntimePlayer.Length; i++)
			{
				multiClient.RuntimePlayer[i] = new RuntimePlayer
				{
					PlayerName = $"Test Name {i}",
					Skin = GameId.Male01Avatar,
					PlayerLevel = (uint) i,
					NormalizedSpawnPosition = new FPVector2(i * FP._0_50),
					Loadout = new[]
					{
						new Equipment(GameId.ModRifle,
						              rarity: EquipmentRarity.Common,
						              adjective: EquipmentAdjective.Cool,
						              material: EquipmentMaterial.Carbon,
						              manufacturer: EquipmentManufacturer.Military,
						              faction: EquipmentFaction.Chaos)
					}
				};
			}
		}
	}
}
