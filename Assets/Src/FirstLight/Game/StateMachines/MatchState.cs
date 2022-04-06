using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using Photon.Deterministic;
using Photon.Realtime;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the match in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class MatchState
	{
		public static readonly IStatechartEvent MatchEndedEvent = new StatechartEvent("Match Ended Event");
		
		private static readonly IStatechartEvent _leaveMatchEvent = new StatechartEvent("Leave Match Event");
		
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
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var loading = stateFactory.TaskWait("Loading Assets");
			var connecting = stateFactory.State("Connecting Screen");
			var connectedCheck = stateFactory.Choice("Connected Check");
			var gameSimulation = stateFactory.Nest("Game Simulation");
			var unloading  = stateFactory.TaskWait("Unloading Assets");
			var disconnected = stateFactory.State("Disconnected Screen");
			
			initial.Transition().Target(loading);
			initial.OnExit(SubscribeEvents);
			
			loading.WaitingFor(LoadMatchAssets).Target(connectedCheck);
			
			connectedCheck.Transition().Condition(IsConnected).Target(gameSimulation);
			connectedCheck.Transition().Condition(IsDisconnected).Target(disconnected);
			connectedCheck.Transition().Target(connecting);
			
			connecting.Event(NetworkState.ConnectedEvent).Target(gameSimulation);
			connecting.Event(NetworkState.DisconnectedEvent).Target(disconnected);
			
			gameSimulation.Nest(_gameSimulationState.Setup).Target(unloading);
			gameSimulation.Event(NetworkState.DisconnectedEvent).Target(disconnected);
			
			disconnected.OnEnter(OpenDisconnectedScreen);
			disconnected.OnEnter(CloseLoadingScreen);
			disconnected.Event(NetworkState.ReconnectEvent).Target(connecting);
			disconnected.Event(_leaveMatchEvent).Target(unloading);
			disconnected.OnExit(OpenLoadingScreen);
			disconnected.OnExit(CloseDisconnectedScreen);

			unloading.OnEnter(OpenLoadingScreen);
			unloading.WaitingFor(UnloadMatchAssets).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void SubscribeEvents()
		{
			// Subscribe to messages here
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}
		
		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void CloseLoadingScreen()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
		}

		private void OpenDisconnectedScreen()
		{
			var data = new DisconnectedScreenPresenter.StateData
			{
				MainMenuClicked = () => _statechartTrigger(_leaveMatchEvent),
				ReconnectClicked = () => _statechartTrigger(NetworkState.ReconnectEvent)
			};
			
			_uiService.OpenUi<DisconnectedScreenPresenter, DisconnectedScreenPresenter.StateData>(data);
		}

		private void CloseDisconnectedScreen()
		{
			_uiService.CloseUi<DisconnectedScreenPresenter>();
		}

		private bool IsConnected()
		{
			return _services.NetworkService.QuantumClient.IsConnectedAndReady;
		}

		private bool IsDisconnected()
		{
			return _services.NetworkService.QuantumClient.DisconnectedCause != DisconnectCause.None;
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
			var config = _gameDataProvider.AppDataProvider.CurrentMapConfig;
			var map = config.Map.ToString();
			var entityService = new GameObject(nameof(EntityViewUpdaterService)).AddComponent<EntityViewUpdaterService>();
			var runnerConfigs = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();
			var sceneTask = _services.AssetResolverService.LoadSceneAsync($"Scenes/{map}.unity", LoadSceneMode.Additive);
			
			MainInstaller.Bind<IEntityViewUpdaterService>(entityService);
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AudioAdventureAssetConfigs>());
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AdventureAssetConfigs>());
			runnerConfigs.SetRuntimeConfig(config);
			
			tasks.Add(sceneTask);
			tasks.AddRange(LoadQuantumAssets(map));
			tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.MatchUi));
			
			await Task.WhenAll(tasks);

			SceneManager.SetActiveScene(sceneTask.Result);
			
#if UNITY_EDITOR
			SetQuantumMultiClient(runnerConfigs, entityService);
#endif
		}

		private async Task UnloadMatchAssets()
		{
			var mapId = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().RuntimeConfig.MapId;
			var scene = SceneManager.GetSceneByName(mapId.ToString());
			var configProvider = _services.ConfigsProvider;
			var entityService = MainInstaller.Resolve<IEntityViewUpdaterService>();
			
			MainInstaller.Clean<IEntityViewUpdaterService>();
			Camera.main.gameObject.SetActive(false);
			_uiService.UnloadUiSet((int) UiSetId.MatchUi);
			_services.AudioFxService.DetachAudioListener();
			_statechartTrigger(MatchEndedEvent);
			
			await _services.AssetResolverService.UnloadSceneAsync(scene);
			
			GameObject.Destroy((entityService as EntityViewUpdaterService).gameObject);
			_services.VfxService.DespawnAll();
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AudioAdventureAssetConfigs>());
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AdventureAssetConfigs>());
			Resources.UnloadUnusedAssets();
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
					Gear = null,
					Weapon = new Equipment(GameId.AK47, ItemRarity.Common, ItemAdjective.Cool, ItemMaterial.Carbon, 
					                       ItemManufacturer.Futuristic, ItemFaction.Chaos, 1, 1)
				};
			}
		}
	}
}