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
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Adventure State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class AdventureState
	{
		public static readonly IStatechartEvent GameEndedEvent = new StatechartEvent("Game Ended Event");
		
		private static readonly IStatechartEvent _leaveAdventureEvent = new StatechartEvent("Leave Adventure Event");
		
		private readonly IGameServices _services;
		private readonly IGameUiService _uiService;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		
		public AdventureState(IGameDataProvider gameDataProvider, IGameServices services, IGameUiService uiService, 
		                      IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_assetAdderService = assetAdderService;
			_gameDataProvider = gameDataProvider;
			_statechartTrigger = statechartTrigger;
		}

		/// <summary>
		/// Setups the Adventure gameplay state
		/// </summary>
		public void Setup(IStateFactory stateFactory, GameSimulationState simulationState)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var loading = stateFactory.TaskWait("Loading Adventure");
			var connecting = stateFactory.State("Connecting Screen");
			var connectedCheck = stateFactory.Choice("Connected Check");
			var gameSimulation = stateFactory.Nest("Game Simulation");
			var adventureUnloading  = stateFactory.TaskWait("Adventure Unloading");
			var disconnected = stateFactory.State("Disconnected Screen");
			
			initial.Transition().Target(loading);
			initial.OnExit(SubscribeEvents);
			
			loading.WaitingFor(LoadAdventure).Target(connectedCheck);
			
			connectedCheck.Transition().Condition(IsConnected).Target(gameSimulation);
			connectedCheck.Transition().Condition(IsDisconnected).Target(disconnected);
			connectedCheck.Transition().Target(connecting);
			
			connecting.Event(NetworkState.ConnectedEvent).Target(gameSimulation);
			connecting.Event(NetworkState.DisconnectedEvent).Target(disconnected);
			
			gameSimulation.Nest(simulationState.Setup).Target(adventureUnloading);
			gameSimulation.Event(NetworkState.DisconnectedEvent).Target(disconnected);
			
			disconnected.OnEnter(OpenDisconnectedScreen);
			disconnected.OnEnter(CloseLoadingScreen);
			disconnected.Event(NetworkState.ReconnectEvent).Target(connecting);
			disconnected.Event(_leaveAdventureEvent).Target(adventureUnloading);
			disconnected.OnExit(OpenLoadingScreen);
			disconnected.OnExit(CloseDisconnectedScreen);

			adventureUnloading.OnEnter(OpenLoadingScreen);
			adventureUnloading.WaitingFor(UnloadAdventure).Target(final);

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
				MainMenuClicked = () => _statechartTrigger(_leaveAdventureEvent),
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

		private async Task LoadAdventure()
		{
			var tasks = new List<Task>();
			var adventureServices = new AdventureServices(_services.AssetResolverService);
			var info = _gameDataProvider.AdventureDataProvider.AdventureSelectedInfo;
			var map = info.Config.Map.ToString();
			var operation = _services.AssetResolverService.LoadSceneAsync($"Scenes/{map}.unity", LoadSceneMode.Additive);
			
			AdventureInstaller.Bind<IAdventureServices>(adventureServices);

			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AudioAdventureAssetConfigs>());
			_assetAdderService.AddConfigs(_services.ConfigsProvider.GetConfig<AdventureAssetConfigs>());
			_services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().SetRuntimeConfig(info);
			
			tasks.Add(operation);
			tasks.AddRange(LoadQuantumAssets(map));
			tasks.AddRange(_uiService.LoadUiSetAsync((int) UiSetId.AdventureUi));
			
			await Task.WhenAll(tasks);

			SceneManager.SetActiveScene(operation.Result);
			
			_services.AudioFxService.AudioListener.transform.SetParent(Camera.main.transform);
		}

		private async Task UnloadAdventure()
		{
			var mapId = _services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>().RuntimeConfig.MapId;
			var scene = SceneManager.GetSceneByName(mapId.ToString());
			var adventureServices = AdventureInstaller.Resolve<IAdventureServices>();
			var configProvider = _services.ConfigsProvider;
			
			Camera.main.gameObject.SetActive(false);
			_uiService.UnloadUiSet((int) UiSetId.AdventureUi);
			_services.AudioFxService.DetachAudioListener();
			_statechartTrigger(GameEndedEvent);
			
			await _services.AssetResolverService.UnloadSceneAsync(scene);
			
			_services.VfxService.DespawnAll();
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AudioAdventureAssetConfigs>());
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AdventureAssetConfigs>());
			adventureServices.Dispose();
			AdventureInstaller.Clean();
			Resources.UnloadUnusedAssets();
		}
	}
}