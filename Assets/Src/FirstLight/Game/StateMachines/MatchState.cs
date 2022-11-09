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
		
		private readonly GameSimulationState _gameSimulationState;
		private readonly IGameServices _services;
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
			_networkService = networkService;
			_dataService = dataService;
			_uiService = uiService;
			_assetAdderService = assetAdderService;
			_gameSimulationState = new GameSimulationState(gameDataProvider, services, uiService, statechartTrigger);

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
			var unloading = stateFactory.State("Unloading");
			var disconnectedMm = stateFactory.State("Disconnected Matchmaking");
			var disconnectedGame = stateFactory.State("Disconnected Game Simulation");
			var postDisconnectCheck = stateFactory.Choice("Post Reload Check");
			var mmDisconnectCheck = stateFactory.Choice("Disconnect Check");
		
			initial.Transition().Target(loading);
			initial.OnExit(SubscribeEvents);

			loading.OnEnter(OpenMatchmakingScreen);
			loading.WaitingFor(LoadMatchAssets).Target(roomCheck);
			
			roomCheck.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(unloading);
			roomCheck.Transition().Target(matchmaking);

			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(mmDisconnectCheck);
			matchmaking.Event(NetworkState.LeftRoomEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(mmDisconnectCheck);
			matchmaking.Event(NetworkState.RoomClosedEvent).Target(playerReadyCheck);
			
			mmDisconnectCheck.Transition().Condition(NetworkUtils.IsOnlineAndConnected).Target(unloading);
			mmDisconnectCheck.Transition().Target(disconnectedMm);
			
			playerReadyCheck.OnEnter(CheckPlayerAssetsLoaded);
			playerReadyCheck.Transition().Condition(AreAllPlayersReady).Target(gameSimulation);
			playerReadyCheck.Transition().Target(playerReadyWait);
			
			playerReadyWait.OnEnter(PreloadPlayerMatchAssets);
			playerReadyWait.Event(AllPlayersReadyEvent).Target(gameSimulation);
			playerReadyWait.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(unloading);
			playerReadyWait.Event(NetworkState.LeftRoomEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(unloading);
			
			gameSimulation.Nest(_gameSimulationState.Setup).Target(unloading);
			gameSimulation.Event(NetworkState.PhotonCriticalDisconnectedEvent).OnTransition(OnDisconnectDuringSimulation).Target(disconnectedGame);

			disconnectedMm.OnEnter(OpenDisconnectedScreen);
			disconnectedMm.Event(NetworkState.JoinedRoomEvent).Target(postDisconnectCheck);
			disconnectedMm.Event(NetworkState.JoinRoomFailedEvent).Target(unloading);
			disconnectedMm.Event(NetworkState.DcScreenBackEvent).Target(unloading);
			
			disconnectedGame.OnEnter(OpenDisconnectedScreen);
			disconnectedGame.Event(NetworkState.PhotonMasterConnectedEvent).Target(postDisconnectCheck);
			disconnectedGame.Event(NetworkState.JoinRoomFailedEvent).Target(unloading);
			disconnectedGame.Event(NetworkState.DcScreenBackEvent).Target(unloading);

			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringMatchmaking).OnTransition(OnReloadToMatchmaking).Target(matchmaking);
			postDisconnectCheck.Transition().Condition(HasDisconnectedDuringSimulation).OnTransition(CloseCurrentScreen).Target(playerReadyCheck);
			postDisconnectCheck.Transition().OnTransition(CloseCurrentScreen).Target(unloading);
			
			unloading.OnEnter(OpenLoadingScreen);
			unloading.OnEnter(UnloadAllMatchAssets);
			unloading.Event(MatchUnloadedEvent).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}

		private void OnReloadToMatchmaking()
		{
			SendCoreAssetsLoadedMessage();
			OpenMatchmakingScreen();
		}

		private void SubscribeEvents()
		{
			
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private void OnDisconnectDuringMatchmaking()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Matchmaking;
		}
		
		private void OnDisconnectDuringFinalPreload()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.FinalPreload;
			_uiService.CloseUi<MatchmakingLoadingScreenPresenter>();
		}

		private void OnDisconnectDuringSimulation()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Simulation;
		}

		private void OpenMatchmakingScreen()
		{
			var data = new MatchmakingLoadingScreenPresenter.StateData();

			_services.AnalyticsService.MatchCalls.MatchInitiate();
			_uiService.OpenScreen<MatchmakingLoadingScreenPresenter, MatchmakingLoadingScreenPresenter.StateData>(data);
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

		private void OpenLoadingScreen()
		{
			_uiService.OpenScreen<LoadingScreenPresenter>();
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
			var matchServices = new MatchServices(entityService, _services, _dataService);
			
			MainInstaller.Bind<IMatchServices>(matchServices);
			
			var runnerConfigs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var sceneTask = _services.AssetResolverService.LoadSceneAsync($"Scenes/{map}.unity", LoadSceneMode.Additive);
			
			// TODO ROB _assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AudioAdventureAssetConfigs>());
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
			
			SendCoreAssetsLoadedMessage();

#if UNITY_EDITOR
			SetQuantumMultiClient(runnerConfigs, entityService);
#endif
		}

		private async void UnloadAllMatchAssets()
		{
			var scene = SceneManager.GetActiveScene();
			var configProvider = _services.ConfigsProvider;

			MainInstaller.CleanDispose<IMatchServices>();
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

		private void SendCoreAssetsLoadedMessage()
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
