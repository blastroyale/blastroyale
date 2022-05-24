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
		private readonly IGameUiService _uiService;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;

		public MatchState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService,
		                  IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_assetAdderService = assetAdderService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
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
			var unloading = stateFactory.TaskWait("Unloading Assets");

			initial.Transition().Target(loading);
			initial.OnExit(SubscribeEvents);

			loading.OnEnter(OpenMatchmakingScreen);
			loading.OnEnter(CloseLoadingScreen);
			loading.WaitingFor(LoadMatchAssets).Target(roomCheck);

			roomCheck.Transition().Condition(IsDisconnected).OnTransition(CloseMatchmakingScreen).Target(unloading);
			roomCheck.Transition().Condition(IsRoomClosed).Target(playerReadyCheck);
			roomCheck.Transition().Target(matchmaking);

			matchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(CloseMatchmakingScreen)
			           .Target(unloading);
			matchmaking.Event(NetworkState.LeftRoomEvent).OnTransition(CloseMatchmakingScreen).Target(unloading);
			matchmaking.Event(NetworkState.RoomClosedEvent).Target(playerReadyCheck);

			playerReadyCheck.Transition().Condition(AreAllPlayersReady).Target(gameSimulation);
			playerReadyCheck.Transition().Target(playerReadyWait);

			playerReadyWait.OnEnter(PreloadAllPlayersAssets);
			playerReadyWait.Event(AllPlayersReadyEvent).Target(gameSimulation);
			playerReadyWait.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(CloseMatchmakingScreen)
			               .Target(unloading);

			gameSimulation.Nest(_gameSimulationState.Setup).Target(unloading);
			gameSimulation.Event(NetworkState.PhotonDisconnectedEvent).Target(unloading);
			gameSimulation.Event(NetworkState.LeftRoomEvent).Target(unloading);

			unloading.OnEnter(OpenLoadingScreen);
			unloading.WaitingFor(UnloadMatchAssets).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Do Nothing
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private void OpenMatchmakingScreen()
		{
			/*
			 This is ugly but unfortunately saving the main character view state will over-engineer the simplicity to
			 just request the object and also to Inject the UiService to a presenter and give it control to the entire UI.
			 Because this is only executed once before the loading of a the game map, it is ok to have garbage and a slow 
			 call as it all be cleaned up in the end of the loading.
			 Feel free to improve this solution if it doesn't over-engineer the entire tech.
			 */
			var data = new MatchmakingLoadingScreenPresenter.StateData
			{
				UiService = _uiService
			};

			_uiService.OpenUi<MatchmakingLoadingScreenPresenter, MatchmakingLoadingScreenPresenter.StateData>(data);
		}

		private void CloseMatchmakingScreen()
		{
			_uiService.CloseUi<MatchmakingLoadingScreenPresenter>();
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

		private bool AreAllPlayersReady()
		{
			return _services.NetworkService.QuantumClient.CurrentRoom.AreAllPlayersReady();
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
			var entityService =
				new GameObject(nameof(EntityViewUpdaterService)).AddComponent<EntityViewUpdaterService>();
			var runnerConfigs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var sceneTask =
				_services.AssetResolverService.LoadSceneAsync($"Scenes/{map}.unity", LoadSceneMode.Additive);

			MainInstaller.Bind<IEntityViewUpdaterService>(entityService);
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AudioAdventureAssetConfigs>());
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AdventureAssetConfigs>());
			runnerConfigs.SetRuntimeConfig(config);

			tasks.Add(sceneTask);
			tasks.AddRange(LoadQuantumAssets(map));
			tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.MatchUi));
			tasks.AddRange(PreloadGameAssets());

			await Task.WhenAll(tasks);

			_services.MessageBrokerService.Publish(new CoreMatchAssetsLoadedMessage());

			SceneManager.SetActiveScene(sceneTask.Result);

#if UNITY_EDITOR
			SetQuantumMultiClient(runnerConfigs, entityService);
#endif
		}

		private async Task UnloadMatchAssets()
		{
			var scene = SceneManager.GetActiveScene();
			var configProvider = _services.ConfigsProvider;
			var entityService = MainInstaller.Resolve<IEntityViewUpdaterService>();

			MainInstaller.Clean<IEntityViewUpdaterService>();
			Camera.main.gameObject.SetActive(false);
			_uiService.UnloadUiSet((int) UiSetId.MatchUi);
			_services.AudioFxService.DetachAudioListener();

			await _services.AssetResolverService.UnloadSceneAsync(scene);

			Object.Destroy(((EntityViewUpdaterService) entityService).gameObject);
			_services.VfxService.DespawnAll();
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AudioAdventureAssetConfigs>());
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AdventureAssetConfigs>());
			Resources.UnloadUnusedAssets();
		}

		private List<Task> PreloadGameAssets()
		{
			var tasks = new List<Task>();

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

			return tasks;
		}

		private async void PreloadAllPlayersAssets()
		{
			_services.MessageBrokerService.Publish(new StartedFinalPreloadMessage());

			var tasks = new List<Task>();

			// Preload players assets
			foreach (var player in _services.NetworkService.QuantumClient.CurrentRoom.Players)
			{
				var preloadIds = (int[]) player.Value.CustomProperties[GameConstants.Data.PLAYER_PROPS_PRELOAD_IDS];

				foreach (var item in preloadIds)
				{
					tasks.Add(_services.AssetResolverService.RequestAsset<GameId, GameObject>((GameId) item, true,
						          false));
				}
			}

			await Task.WhenAll(tasks);

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
						new Equipment(GameId.AK47,
						              rarity: EquipmentRarity.Common,
						              adjective: EquipmentAdjective.Cool,
						              material: EquipmentMaterial.Carbon,
						              manufacturer: EquipmentManufacturer.Futuristic,
						              faction: EquipmentFaction.Chaos)
					}
				};
			}
		}
	}
}