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
		
		private readonly GameSimulationState _gameSimulationState;
		private readonly IGameServices _services;
		private readonly IGameBackendNetworkService _networkService;
		private readonly IGameUiService _uiService;
		private readonly IAssetAdderService _assetAdderService;
		private bool _arePlayerAssetsLoaded = false;
		
		public MatchState(IGameServices services, IGameBackendNetworkService networkService, IGameUiService uiService, IGameDataProvider gameDataProvider, 
		                  IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_networkService = networkService;
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
			var disconnectReload = stateFactory.TaskWait("Reloading Assets");
			var roomCheck = stateFactory.Choice("Room Check");
			var matchmaking = stateFactory.State("Matchmaking");
			var playerReadyCheck = stateFactory.Choice("Player Ready Check");
			var playerReadyWait = stateFactory.State("Player Ready Wait");
			var gameSimulation = stateFactory.Nest("Game Simulation");
			var unloading = stateFactory.TaskWait("Unloading");
			var disconnectCheck = stateFactory.Choice("Disconnect Check");
			var disconnected = stateFactory.State("Disconnected");
			var postDisconnectReloadCheck = stateFactory.Choice("Post Reload Check");
		
			initial.Transition().Target(loading);
			initial.OnExit(SubscribeEvents);

			loading.OnEnter(OpenMatchmakingScreen);
			loading.OnEnter(CloseLoadingScreen);
			loading.WaitingFor(LoadMatchAssets).Target(roomCheck);
			
			roomCheck.Transition().Condition(IsDisconnected).OnTransition(CloseMatchmakingScreen).Target(unloading);
			roomCheck.Transition().Condition(IsRoomClosed).Target(playerReadyCheck);
			roomCheck.Transition().Target(matchmaking);

			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(unloading);
			matchmaking.Event(NetworkState.LeftRoomEvent).OnTransition(OnDisconnectDuringMatchmaking).Target(unloading);
			matchmaking.Event(NetworkState.RoomClosedEvent).Target(playerReadyCheck);
			
			playerReadyCheck.OnEnter(CheckPlayerAssetsLoaded);
			playerReadyCheck.Transition().Condition(AreAllPlayersReady).Target(gameSimulation);
			playerReadyCheck.Transition().Target(playerReadyWait);
			
			playerReadyWait.OnEnter(PreloadPlayerMatchAssets);
			playerReadyWait.Event(AllPlayersReadyEvent).Target(gameSimulation);
			playerReadyWait.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringFinalPreload).Target(unloading);
			
			gameSimulation.Nest(_gameSimulationState.Setup).Target(unloading);
			gameSimulation.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(OnDisconnectDuringSimulation).Target(unloading);
			gameSimulation.Event(NetworkState.LeftRoomEvent).OnTransition(OnDisconnectDuringSimulation).Target(unloading);
			
			unloading.OnEnter(OpenLoadingScreen);
			unloading.WaitingFor(UnloadAllMatchAssets).Target(disconnectCheck);
			
			disconnectCheck.Transition().Condition(IsPhotonConnected).Target(final);
			disconnectCheck.Transition().Target(disconnected);
			
			disconnected.OnEnter(CloseLoadingScreen);
			disconnected.Event(NetworkState.JoinedRoomEvent).OnTransition(OpenMatchmakingScreen).Target(disconnectReload);
			disconnected.Event(NetworkState.JoinRoomFailedEvent).Target(final);
			disconnected.Event(NetworkState.DisconnectedScreenBackEvent).Target(final);
			
			disconnectReload.WaitingFor(LoadMatchAssets).Target(postDisconnectReloadCheck);
			
			postDisconnectReloadCheck.Transition().Condition(IsRoomReadyForSimulation).Target(playerReadyCheck);
			postDisconnectReloadCheck.Transition().Target(roomCheck);
			
			final.OnEnter(OpenLoadingScreen);
			final.OnEnter(UnsubscribeEvents);
		}
		
		public bool IsPhotonConnected()
		{
			return _services.NetworkService.QuantumClient.IsConnected;
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
			_uiService.CloseUi<MatchmakingLoadingScreenPresenter>();
		}
		
		private void OnDisconnectDuringFinalPreload()
		{
			_networkService.LastDisconnectLocation.Value = LastDisconnectionLocation.Loading;
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
			_uiService.OpenUiAsync<MatchmakingLoadingScreenPresenter, MatchmakingLoadingScreenPresenter.StateData>(data);
		}

		private void CloseMatchmakingScreen()
		{
			_uiService.CloseUi<MatchmakingLoadingScreenPresenter>(false, true);
		}

		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void CloseLoadingScreen()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
		}

		private bool IsDisconnected()
		{
			return !_services.NetworkService.QuantumClient.IsConnected;
		}

		private bool IsRoomClosed()
		{
			return _services.NetworkService.QuantumClient.CurrentRoom.IsOpen == false;
		}
		
		private bool IsRoomReadyForSimulation()
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
			var map = config.Map.ToString();
			var entityService = new GameObject(nameof(EntityViewUpdaterService)).AddComponent<EntityViewUpdaterService>();
			var matchServices = new MatchServices(entityService, _services);
			var runnerConfigs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var sceneTask = _services.AssetResolverService.LoadSceneAsync($"Scenes/{map}.unity", LoadSceneMode.Additive);
			
			MainInstaller.Bind<IMatchServices>(matchServices);
			MainInstaller.Bind<IEntityViewUpdaterService>(entityService);
			// TODO ROB _assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AudioAdventureAssetConfigs>());
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AdventureAssetConfigs>());
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<EquipmentRarityAssetConfigs>());
			runnerConfigs.SetRuntimeConfig(config);

			tasks.Add(sceneTask);
			tasks.AddRange(LoadQuantumAssets(map));
			tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.MatchUi));
			
			switch (_services.NetworkService.CurrentRoomMapConfig.Value.GameMode)
			{
				case GameMode.Deathmatch : tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.DeathMatchMatchUi));
					break;
				case GameMode.BattleRoyale : tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.BattleRoyaleMatchUi));
					break;
			}
			tasks.AddRange(PreloadGameAssets());

			await Task.WhenAll(tasks);

			SceneManager.SetActiveScene(sceneTask.Result);

			await Task.WhenAll(PreloadMapAssets());
			
			_services.MessageBrokerService.Publish(new CoreMatchAssetsLoadedMessage());

#if UNITY_EDITOR
			SetQuantumMultiClient(runnerConfigs, entityService);
#endif
		}

		private async Task UnloadAllMatchAssets()
		{
			var scene = SceneManager.GetActiveScene();
			var configProvider = _services.ConfigsProvider;
			var entityService = MainInstaller.Resolve<IEntityViewUpdaterService>();

			MainMenuInstaller.Resolve<IMatchServices>().Dispose();
			MainInstaller.Clean<IEntityViewUpdaterService>();
			MainInstaller.Clean<IMatchServices>();
			_uiService.UnloadUiSet((int) UiSetId.MatchUi);
			_services.AudioFxService.DetachAudioListener();

			await _services.AssetResolverService.UnloadSceneAsync(scene);

			Object.Destroy(((EntityViewUpdaterService) entityService).gameObject);
			
			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMatchAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AdventureAssetConfigs>());
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<EquipmentRarityAssetConfigs>());

			Resources.UnloadUnusedAssets();

			_arePlayerAssetsLoaded = false;
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
