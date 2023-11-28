using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Server.SDK.Models;
using FirstLight.Server.SDK.Modules.GameConfiguration;
using FirstLight.Services;
using FirstLight.Statechart;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// The State Machine that controls the entire flow of the game
	/// </summary>
	public class GameStateMachine
	{
		private readonly IStatechart _statechart;
		private readonly InitialLoadingState _initialLoadingState;
		private readonly AuthenticationState _authenticationState;
		private readonly AudioState _audioState;
		private readonly NetworkState _networkState;
		private readonly TutorialState _tutorialState;
		private readonly GameLogic _gameLogic;
		private readonly CoreLoopState _coreLoopState;
		private readonly ReconnectionState _reconnection;
		private readonly IGameServices _services;
		private readonly IDataService _dataService;
		private readonly IConfigsAdder _configsAdder;
		private readonly IGameUiServiceInit _uiService;

		public GameStateMachine(GameLogic gameLogic, IGameServices services, IGameUiServiceInit uiService,
								IInternalGameNetworkService networkService, IInternalTutorialService tutorialService,
								IConfigsAdder configsAdder,
								IAssetAdderService assetAdderService, IDataService dataService,
								IVfxInternalService<VfxId> vfxService)
		{
			_dataService = dataService;
			_gameLogic = gameLogic;
			_services = services;
			_uiService = uiService;
			_configsAdder = configsAdder; 
			_initialLoadingState = new InitialLoadingState(services, uiService, assetAdderService, dataService, configsAdder, vfxService, Trigger);
			_authenticationState = new AuthenticationState(services, uiService, dataService, Trigger);
			_audioState = new AudioState(gameLogic, services, Trigger);
			_reconnection = new ReconnectionState(services, gameLogic, networkService, uiService, Trigger);
			_networkState = new NetworkState(gameLogic, services, networkService, Trigger);
			_tutorialState = new TutorialState(gameLogic, services, tutorialService, Trigger);
			_coreLoopState = new CoreLoopState(_reconnection, services, gameLogic, dataService, networkService, uiService, gameLogic, assetAdderService, Trigger,services.RoomService);
			_statechart = new Statechart.Statechart(Setup);
#if DEVELOPMENT_BUILD
			Statechart.Statechart.OnStateTimed += (state, millis) =>
			{
				FLog.Info($"[State Time] {state} took {millis}ms");
				services.AnalyticsService.LogEvent("state-time", new AnalyticsData()
				{
					{"state", state},
					{"milliseconds", millis},
					{"device-memory-mb", SystemInfo.systemMemorySize },
					{"device-model", SystemInfo.deviceModel },
					{"device-name", SystemInfo.deviceName },
					{"cpu", SystemInfo.processorType }
				});
			};
#endif
		}
		

		/// <inheritdoc cref="IStatechart.Run"/>
		public void Run()
		{
			_statechart.Run();
		}

		private void Trigger(IStatechartEvent eventTrigger)
		{
			_statechart.Trigger(eventTrigger);
		}
		
		

		private void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var initialAssets = stateFactory.TaskWait("Initial Asset");
			var initialConfigs = stateFactory.TaskWait("Initial Configs");
			var internetCheck = stateFactory.Choice("Internet Check");
			var initialLoading = stateFactory.Nest("Initial Loading");
			var authentication = stateFactory.Nest("Authentication");
			var core = stateFactory.Split("Core");

			initial.Transition().Target(initialConfigs);
			initial.OnExit(SubscribeEvents);
			
			initialConfigs.OnEnter(CheckDeviceSetup);
			initialConfigs.WaitingFor(LoadInitialConfigs).Target(initialAssets);

			initialAssets.OnEnter(_authenticationState.QuickAsyncLogin);
			initialAssets.WaitingFor(LoadCoreAssets).Target(internetCheck);
			
			internetCheck.Transition().Condition(NetworkUtils.IsOffline).OnTransition(OpenNoInternetPopUp)
				.Target(final);
			internetCheck.Transition().Target(initialLoading);

			initialLoading.Nest(_initialLoadingState.Setup).Target(authentication);
			initialLoading.OnExit(InitializeLocalLogic);

			authentication.Nest(_authenticationState.Setup).Target(core);
			authentication.OnExit(InitializeRemainingLogic);

			core.Split(_networkState.Setup, _audioState.Setup, _tutorialState.Setup, _coreLoopState.Setup).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}


		private async Task LoadInitialConfigs()
		{
			_gameLogic.InitLocal();
			await LoadRequiredAuthenticationConfigs();
			await VersionUtils.LoadVersionDataAsync();
		}

		private void SubscribeEvents()
		{
		}

		private void UnsubscribeEvents()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
		}

		private void InitializeLocalLogic()
		{
			_services.AudioFxService.AudioListener.Listener.enabled = true;

			// TODO: REMOVE BELOW if works properly by uncommenting AppLogic Init lines
			_gameLogic.AppLogic.SetDetailLevel();
			_gameLogic.AppLogic.SetFpsTarget();
		}

		private void CheckDeviceSetup()
		{
		//var test = 123;
			_dataService.LoadData<AppData>();
#if UNITY_ANDROID
			if (SystemInfo.systemMemorySize <= 5000)
			{
				var appData = _dataService.GetData<AppData>();
				if (appData.IsFirstSession || string.IsNullOrEmpty(appData.DeviceId))
				{
					appData.CurrentDetailLevel = GraphicsConfig.DetailLevel.Low;
				}
			}
#endif
		}

		private void InitializeRemainingLogic()
		{
			_gameLogic.Init();
			_services.GameModeService.Init();
			_services.IAPService.Init();
			_services.AnalyticsService.SessionCalls.GameLoaded();
		}

		private void OpenNoInternetPopUp()
		{
#if UNITY_EDITOR
			var desc = string.Format(ScriptLocalization.General.NoInternet);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.ExitGame,
				ButtonOnClick = () => { _services.QuitGame("Closing no internet popup"); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, desc, false, confirmButton);
#else
			var button = new FirstLight.NativeUi.AlertButton
			{
				Callback = () => { _services.QuitGame("Closing no internet popup"); },
				Style = FirstLight.NativeUi.AlertButtonStyle.Negative,
				Text = ScriptLocalization.General.ExitGame
			};

			FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NoInternet,
				ScriptLocalization.General.NoInternetDescription, button);
#endif
		}
		
		private async Task LoadRequiredAuthenticationConfigs()
		{
			var quantumAsset = await _services.AssetResolverService.LoadAssetAsync<QuantumRunnerConfigs>(AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address);
			_configsAdder.AddSingletonConfig(quantumAsset);
			_services.AssetResolverService.UnloadAsset(quantumAsset);
			
			var liveopsFeatureFlags = await _services.AssetResolverService.LoadAssetAsync<LiveopsFeatureFlagConfigs>(AddressableId.Configs_LiveopsFeatureFlagConfigs.GetConfig().Address);
			_configsAdder.AddConfigs(data => data.UniqueIdentifier(), liveopsFeatureFlags.Configs);
			_services.AssetResolverService.UnloadAsset(liveopsFeatureFlags);
		}

		private async Task LoadCoreAssets()
		{
			var time = Time.realtimeSinceStartup;
			var uiAddress = AddressableId.Configs_Settings_UiConfigs.GetConfig().Address;
			var asset = await _services.AssetResolverService.LoadAssetAsync<UiConfigs>(uiAddress);
			_uiService.Init(asset);
		
			_services.AssetResolverService.UnloadAsset(asset);

			await _uiService.LoadUiAsync<GenericDialogPresenter>();
			await _uiService.LoadUiAsync<LoadingScreenPresenter>(true);
			await Task.WhenAll(_uiService.LoadUiSetAsync((int) UiSetId.InitialLoadUi));
			
			var dic = new Dictionary<string, object>
			{
				{"client_version", VersionUtils.VersionInternal},
				{"total_time", Time.realtimeSinceStartup - time},
				{"vendor_id", SystemInfo.deviceUniqueIdentifier}
			};
			_services.AnalyticsService.LogEvent(AnalyticsEvents.LoadCoreAssetsComplete, dic);
		}
	}
}