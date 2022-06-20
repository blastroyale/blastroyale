using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Services;
using FirstLight.Statechart;
using FirstLight.UiService;
using I2.Loc;
using MoreMountains.NiceVibrations;
using PlayFab;
using PlayFab.ClientModels;
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
		private readonly NetworkState _networkState;
		private readonly GameLogic _gameLogic;
		private readonly CoreLoopState _coreLoopState;
		private readonly IGameServices _services;
		private readonly IConfigsAdder _configsAdder;
		private readonly IGameUiServiceInit _uiService;
		private readonly IDataService _dataService;

		/// <inheritdoc cref="IStatechart.LogsEnabled"/>
		public bool LogsEnabled
		{
			get => _statechart.LogsEnabled;
			set => _statechart.LogsEnabled = value;
		}

		public GameStateMachine(GameLogic gameLogic, IGameServices services, IGameUiServiceInit uiService, 
		                        IGameBackendNetworkService networkService, IConfigsAdder configsAdder, 
		                        IAssetAdderService assetAdderService, IDataService dataService,
		                        IVfxInternalService<VfxId> vfxService)
		{
			_gameLogic = gameLogic;
			_services = services;
			_uiService = uiService;
			_dataService = dataService;
			_configsAdder = configsAdder;
			_initialLoadingState = new InitialLoadingState(services, uiService, assetAdderService, configsAdder, vfxService, Trigger);
			_authenticationState = new AuthenticationState(services, uiService, dataService, networkService, Trigger);
			_networkState = new NetworkState(gameLogic, services, networkService, Trigger);
			_coreLoopState = new CoreLoopState(services, uiService, gameLogic, assetAdderService, Trigger);
			_statechart = new Statechart.Statechart(Setup);
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
			var internetCheck = stateFactory.Choice("Internet Check");
			var initialLoading = stateFactory.Split("Initial Loading");
			var core = stateFactory.Split("Core");
			
			initial.Transition().Target(initialAssets);
			initial.OnExit(SubscribeEvents);

			initialAssets.WaitingFor(LoadCoreAssets).Target(internetCheck);
			
			internetCheck.Transition().Condition(InternetCheck).OnTransition(OpenNoInternetPopUp).Target(final);
			internetCheck.Transition().Target(initialLoading);

			initialLoading.Split(_initialLoadingState.Setup, _authenticationState.Setup).Target(core);
			initialLoading.OnExit(InitializeGame);
			
			core.Split(_networkState.Setup, _coreLoopState.Setup).Target(final);
			
			final.OnEnter(UnsubscribeEvents);
		}
		
		private void SubscribeEvents()
		{
			// Add any events to subscribe
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService.UnsubscribeAll(this);
		}

		private bool InternetCheck()
		{
			return Application.internetReachability == NetworkReachability.NotReachable;
		}

		private void InitializeGame()
		{
			_gameLogic.Init();

			_services.AudioFxService.AudioListener.enabled = true;
			MMVibrationManager.SetHapticsActive(_gameLogic.AppLogic.IsHapticOn);
			
			// Just marking the default name to avoid missing names
			if (string.IsNullOrWhiteSpace(_gameLogic.AppLogic.NicknameId.Value))
			{
				_services.PlayfabService.UpdateNickname(PlayerLogic.DefaultPlayerName);
			}
		}

		private void OpenNoInternetPopUp()
		{
			var button = new AlertButton
			{
				Callback = Application.Quit,
				Style = AlertButtonStyle.Negative,
				Text = ScriptLocalization.General.ExitGame
			};
			
			NativeUiService.ShowAlertPopUp(false, ScriptLocalization.General.NoInternet, 
			                               ScriptLocalization.General.NoInternetDescription, button);
		}

		private async Task LoadCoreAssets()
		{
			await VersionUtils.LoadVersionDataAsync();

			var uiAddress = AddressableId.Configs_Settings_UiConfigs.GetConfig().Address;
			var quantumAddress = AddressableId.Configs_Settings_QuantumRunnerConfigs.GetConfig().Address;
			var asset = await _services.AssetResolverService.LoadAssetAsync<UiConfigs>(uiAddress);
			var quantumAsset = await _services.AssetResolverService.LoadAssetAsync<QuantumRunnerConfigs>(quantumAddress);

			_uiService.Init(asset);
			_configsAdder.AddSingletonConfig(quantumAsset);
			_services.AssetResolverService.UnloadAsset(asset);
			
			await _uiService.LoadUiAsync<LoadingScreenPresenter>(true);
			await Task.Delay(1000); // Delays 1 sec to play the loading screen animation
			await Task.WhenAll(_uiService.LoadUiSetAsync((int) UiSetId.InitialLoadUi));
		}
	}
}