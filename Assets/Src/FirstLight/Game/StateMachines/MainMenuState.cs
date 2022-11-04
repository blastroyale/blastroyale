using System;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Main Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class MainMenuState
	{
		public static readonly IStatechartEvent MainMenuLoadedEvent = new StatechartEvent("Main Menu Loaded Event");
		public static readonly IStatechartEvent MainMenuUnloadedEvent = new StatechartEvent("Main Menu Unloaded Event");
		
		private readonly IStatechartEvent _tabButtonClickedEvent = new StatechartEvent("Tab Button Clicked Event");
		private readonly IStatechartEvent _currentTabButtonClickedEvent = new StatechartEvent("Current Tab Button Clicked Event");
		private readonly IStatechartEvent _playClickedEvent = new StatechartEvent("Play Clicked Event");
		private readonly IStatechartEvent _settingsMenuClickedEvent = new StatechartEvent("Settings Menu Button Clicked Event");
		private readonly IStatechartEvent _roomJoinCreateClickedEvent = new StatechartEvent("Room Join Create Button Clicked Event");
		private readonly IStatechartEvent _nameChangeClickedEvent = new StatechartEvent("Name Change Clicked Event");
		private readonly IStatechartEvent _chooseGameModeClickedEvent = new StatechartEvent("Game Mode Clicked Event");
		private readonly IStatechartEvent _gameModeSelectedFinishedEvent = new StatechartEvent("Game Mode Selected Finished Event");
		private readonly IStatechartEvent _leaderboardClickedEvent = new StatechartEvent("Leaderboard Clicked Event");
		private readonly IStatechartEvent _battlePassClickedEvent = new StatechartEvent("BattlePass Clicked Event");
		private readonly IStatechartEvent _storeClickedEvent = new StatechartEvent("Store Clicked Event");
		private readonly IStatechartEvent _roomJoinCreateCloseClickedEvent = new StatechartEvent("Room Join Create Close Button Clicked Event");
		private readonly IStatechartEvent _gameCompletedCheatEvent = new StatechartEvent("Game Completed Cheat Event");
		
		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly LootMenuState _lootMenuState;
		private readonly SettingsMenuState _settingsMenuState;
		private readonly EnterNameState _enterNameState;
		private Type _currentScreen;

		public MainMenuState(IGameServices services, IGameUiService uiService, IGameLogic gameLogic,
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameLogic;
			_assetAdderService = assetAdderService;
			_statechartTrigger = statechartTrigger;
			_lootMenuState = new LootMenuState(services, uiService, gameLogic, statechartTrigger);
			_enterNameState = new EnterNameState(services, uiService, gameLogic, statechartTrigger);
			_settingsMenuState = new SettingsMenuState(gameLogic, services, gameLogic, uiService, statechartTrigger);
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var mainMenuLoading = stateFactory.State("Main Menu Loading");
			var mainMenuUnloading = stateFactory.State("Main Menu Unloading");
			var mainMenu = stateFactory.Nest("Main Menu");
			var mainMenuTransition = stateFactory.Transition("Main Transition");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCheck = stateFactory.Choice("Disconnected Final Choice");
			
			initial.Transition().Target(mainMenuLoading);
			initial.OnExit(SubscribeEvents);

			mainMenuLoading.OnEnter(LoadMainMenu);
			mainMenuLoading.OnEnter(ValidateCurrentGameMode);
			mainMenuLoading.Event(MainMenuLoadedEvent).Target(mainMenu);
			mainMenuLoading.OnExit(LoadingComplete);

			mainMenu.Nest(TabsMenuSetup).Target(disconnectedCheck);
			mainMenu.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);
			mainMenu.Event(_tabButtonClickedEvent).Target(mainMenuTransition);

			mainMenuTransition.Transition().Target(mainMenu);
			
			disconnectedCheck.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(disconnected);
			disconnectedCheck.Transition().Target(mainMenuUnloading);
			
			disconnected.OnEnter(OpenDisconnectedScreen);
			disconnected.Event(NetworkState.PhotonMasterConnectedEvent).Target(mainMenu);

			mainMenuUnloading.OnEnter(OpenLoadingScreen);
			mainMenuUnloading.OnEnter(UnloadMainMenu);
			mainMenuUnloading.Event(MainMenuUnloadedEvent).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void TabsMenuSetup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var screenCheck = stateFactory.Choice("Main Screen Check");
			var homeMenu = stateFactory.State("Home Menu");
			var lootMenu = stateFactory.Nest("Loot Menu");
			var heroesMenu = stateFactory.State("Heroes Menu");
			var settingsMenu = stateFactory.Nest("Settings Menu");
			var playClickedCheck = stateFactory.Choice("Play Button Clicked Check");
			var roomWait = stateFactory.State("Room Joined Check");
			var chooseGameMode = stateFactory.State("Enter Choose Game Mode");
			var leaderboard = stateFactory.Wait("Leaderboard");
			var battlePass = stateFactory.Wait("BattlePass");
			var store = stateFactory.Wait("Store");
			var enterNameDialog = stateFactory.Nest("Enter Name Dialog");
			var roomJoinCreateMenu = stateFactory.State("Room Join Create Menu");
			var nftPlayRestricted = stateFactory.Wait("Nft Restriction Pop Up");
			var defaultNameCheck = stateFactory.Choice("Default Player Name Check");
			
			initial.Transition().Target(screenCheck);
			initial.OnExit(OpenUiVfxPresenter);
			
			screenCheck.Transition().Condition(IsCurrentScreen<HomeScreenPresenter>).Target(defaultNameCheck);
			screenCheck.Transition().Condition(IsCurrentScreen<LootScreenPresenter>).Target(lootMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<PlayerSkinScreenPresenter>).Target(heroesMenu);
			screenCheck.Transition().OnTransition(InvalidScreen).Target(final);
			
			defaultNameCheck.Transition().Condition(HasDefaultName).Target(enterNameDialog);
			defaultNameCheck.Transition().Target(homeMenu);

			homeMenu.OnEnter(OpenPlayMenuUI);
			homeMenu.OnEnter(TryClaimUncollectedRewards);
			homeMenu.Event(_playClickedEvent).Target(playClickedCheck);
			homeMenu.Event(_settingsMenuClickedEvent).Target(settingsMenu);
			homeMenu.Event(_gameCompletedCheatEvent).Target(screenCheck);
			homeMenu.Event(_nameChangeClickedEvent).Target(enterNameDialog);
			homeMenu.Event(_chooseGameModeClickedEvent).Target(chooseGameMode);
			homeMenu.Event(_leaderboardClickedEvent).Target(leaderboard);
			homeMenu.Event(_battlePassClickedEvent).Target(battlePass);
			homeMenu.Event(_storeClickedEvent).Target(store);

			playClickedCheck.Transition().Condition(EnoughNftToPlay).OnTransition(SendPlayReadyMessage).Target(roomWait);
			playClickedCheck.Transition().Target(nftPlayRestricted);

			roomWait.Event(NetworkState.JoinedRoomEvent).Target(final);
			roomWait.Event(NetworkState.JoinRoomFailedEvent).Target(homeMenu);
			roomWait.Event(NetworkState.CreateRoomFailedEvent).Target(homeMenu);

			chooseGameMode.OnEnter(OpenGameModeSelectionUI);
			chooseGameMode.Event(_gameModeSelectedFinishedEvent).Target(homeMenu);
			chooseGameMode.Event(_roomJoinCreateClickedEvent).Target(roomJoinCreateMenu);

			leaderboard.WaitingFor(OpenLeaderboardUI).Target(homeMenu);

			battlePass.WaitingFor(OpenBattlePassUI).Target(homeMenu);

			store.WaitingFor(OpenStore).Target(homeMenu);

			enterNameDialog.Nest(_enterNameState.Setup).Target(homeMenu);
			
			nftPlayRestricted.WaitingFor(OpenNftAmountInvalidDialog).Target(homeMenu);

			settingsMenu.Nest(_settingsMenuState.Setup).Target(homeMenu);
			
			lootMenu.Nest(_lootMenuState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);

			heroesMenu.OnEnter(OpenPlayerSkinScreenUI);

			roomJoinCreateMenu.OnEnter(OpenRoomJoinCreateMenuUI);
			roomJoinCreateMenu.Event(_playClickedEvent).Target(roomWait);
			roomJoinCreateMenu.Event(_roomJoinCreateCloseClickedEvent).Target(chooseGameMode);
			roomJoinCreateMenu.Event(NetworkState.JoinRoomFailedEvent).Target(chooseGameMode);
			roomJoinCreateMenu.Event(NetworkState.CreateRoomFailedEvent).Target(chooseGameMode);
		}

		private bool HasDefaultName()
		{
			return _gameDataProvider.AppDataProvider.DisplayNameTrimmed == GameConstants.PlayerName.DEFAULT_PLAYER_NAME ||
			       string.IsNullOrEmpty(_gameDataProvider.AppDataProvider.DisplayNameTrimmed);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_services.GameModeService.SelectedGameMode.Observe(OnGameModeChanged);
		}

		private void OnGameModeChanged(GameModeInfo previous, GameModeInfo next)
		{
			_gameDataProvider.AppDataProvider.LastGameMode = next.Entry;
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_statechartTrigger(_gameCompletedCheatEvent);
		}

		private void TryClaimUncollectedRewards()
		{
			if (_gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0)
			{
				_services.CommandService.ExecuteCommand(new CollectUnclaimedRewardsCommand());
			}
		}
		
		private void ValidateCurrentGameMode()
		{
			var lastGameMode = _gameDataProvider.AppDataProvider.LastGameMode;
			if (_services.GameModeService.IsRotationGameModeValid(lastGameMode))
			{
				_services.GameModeService.SelectedGameMode.Value = new GameModeInfo(lastGameMode);
				return;
			}
			var gameMode = _services.GameModeService.Slots.ReadOnlyList.FirstOrDefault(x => x.Entry.MatchType == MatchType.Casual);
			_services.GameModeService.SelectedGameMode.Value = gameMode;
		}

		private void SendPlayReadyMessage()
		{
			_services.MessageBrokerService.Publish(new PlayMatchmakingReadyMessage());
		}
		
		private bool EnoughNftToPlay()
		{
			return _services.GameModeService.SelectedGameMode.Value.Entry.MatchType == MatchType.Casual
				|| _gameDataProvider.EquipmentDataProvider.EnoughLoadoutEquippedToPlay();
		}

		private bool IsCurrentScreen<T>() where T : UiPresenter
		{
			return _currentScreen == typeof(T);
		}

		private void OpenNftAmountInvalidDialog(IWaitActivity activity)
		{
			var cacheActivity = activity;

			var confirmButton = new GenericDialogButton()
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () => { cacheActivity.Complete(); }
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error,
				ScriptLocalization.MainMenu.NftRestrictionText, false,
				confirmButton);
		}
		
		private void OpenGameModeSelectionUI()
		{
			var data = new GameModeSelectionPresenter.StateData
			{
				GameModeChosen = () =>
				{
					_services.MessageBrokerService.Publish(new SelectedGameModeMessage());
					_statechartTrigger(_gameModeSelectedFinishedEvent);
				},
				CustomGameChosen = () =>
				{
					_statechartTrigger(_roomJoinCreateClickedEvent);
				},
				LeaveGameModeSelection = () =>
				{
					_statechartTrigger(_gameModeSelectedFinishedEvent);
				}
			};
			
			_uiService.OpenScreen<GameModeSelectionPresenter, GameModeSelectionPresenter.StateData>(data);
		}

		private void CloseGameModeSelectionUI()
		{
			_uiService.CloseUi<GameModeSelectionPresenter>();
		}
		
		private void OpenLeaderboardUI(IWaitActivity activity)
		{
			var cacheActivity = activity;

			var data = new LeaderboardScreenPresenter.StateData
			{
				BackClicked = () => { cacheActivity.Complete(); }
			};
			
			_uiService.OpenScreen<LeaderboardScreenPresenter, LeaderboardScreenPresenter.StateData>(data);
		}

		private void CloseLeaderboardUI()
		{
			_uiService.CloseUi<LeaderboardScreenPresenter>();
		}
		
		private void OpenBattlePassUI(IWaitActivity activity)
		{
			var cacheActivity = activity;

			var data = new BattlePassScreenPresenter.StateData
			{
				BackClicked = () => { cacheActivity.Complete(); },
				UiService = _uiService
			};
			
			_uiService.OpenScreen<BattlePassScreenPresenter, BattlePassScreenPresenter.StateData>(data);
		}

		private void OpenStore(IWaitActivity activity)
		{
			var data = new StoreScreenPresenter.StateData
			{
				BackClicked = () => { activity.Complete();},
				OnPurchaseItem = PurchaseItem,
				UiService = _uiService,
				IapProcessingFinished = OnIapProcessingFinished
			};

			_uiService.OpenScreen<StoreScreenPresenter, StoreScreenPresenter.StateData>(data);
		}

		private void CloseStore()
		{
			_uiService.CloseUi<StoreScreenPresenter>();
		}

		private void PurchaseItem(string id)
		{
			_statechartTrigger(NetworkState.IapProcessStartedEvent);
			_services.IAPService.BuyProduct(id);
		}
		
		private void OnIapProcessingFinished()
		{
			_statechartTrigger(NetworkState.IapProcessFinishedEvent);
		}

		private void CloseBattlePassUI()
		{
			_uiService.CloseUi<BattlePassScreenPresenter>();
		}
		
		private void OpenPlayerSkinScreenUI()
		{
			var data = new PlayerSkinScreenPresenter.StateData
			{
				OnCloseClicked = OnTabClickedCallback<HomeScreenPresenter>,
			};

			_uiService.OpenScreen<PlayerSkinScreenPresenter, PlayerSkinScreenPresenter.StateData>(data);
		}

		private void ClosePlayerSkinScreenUI()
		{
			_uiService.CloseUi<PlayerSkinScreenPresenter>(true);
		}

		private void OpenRoomJoinCreateMenuUI()
		{
			var data = new RoomJoinCreateScreenPresenter.StateData
			{
				CloseClicked = RoomJoinCreateCloseClicked,
				PlayClicked = PlayButtonClicked
			};

			_uiService.OpenScreen<RoomJoinCreateScreenPresenter, RoomJoinCreateScreenPresenter.StateData>(data);
		}

		private void CloseRoomJoinCreateMenuUI()
		{
			_uiService.CloseUi<RoomJoinCreateScreenPresenter>(true);
		}

		private void OpenPlayMenuUI()
		{
			var data = new HomeScreenPresenter.StateData
			{
				OnPlayButtonClicked = PlayButtonClicked,
				OnSettingsButtonClicked = () => _statechartTrigger(_settingsMenuClickedEvent),
				OnLootButtonClicked = OnTabClickedCallback<LootScreenPresenter>,
				OnHeroesButtonClicked = OnTabClickedCallback<PlayerSkinScreenPresenter>,
				OnNameChangeClicked = () => _statechartTrigger(_nameChangeClickedEvent),
				OnGameModeClicked = () => _statechartTrigger(_chooseGameModeClickedEvent),
				OnLeaderboardClicked = () => _statechartTrigger(_leaderboardClickedEvent),
				OnBattlePassClicked = () => _statechartTrigger(_battlePassClickedEvent),
				OnStoreClicked = () => _statechartTrigger(_storeClickedEvent)
			};

			_uiService.OpenScreen<HomeScreenPresenter, HomeScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new PlayScreenOpenedMessage());
		}
		
		private void OpenDisconnectedScreen()
		{
			var data = new DisconnectedScreenPresenter.StateData
			{
				ReconnectClicked = () => _services.MessageBrokerService.Publish(new AttemptManualReconnectionMessage())
			};

			_uiService.OpenScreen<DisconnectedScreenPresenter, DisconnectedScreenPresenter.StateData>(data);
		}

		private void ClosePlayMenuUI()
		{
			_uiService.CloseUi<HomeScreenPresenter>();
		}
		
		private void DimDisconnectedScreen()
		{
			_uiService.GetUi<DisconnectedScreenPresenter>().SetFrontDimBlockerActive(true);
		}

		private void UndimDisconnectedScreen()
		{
			_uiService.GetUi<DisconnectedScreenPresenter>().SetFrontDimBlockerActive(false);
		}

		private void LoadingComplete()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
			SetCurrentScreen<HomeScreenPresenter>();
		}

		private void OpenLoadingScreen()
		{
			_uiService.OpenScreen<LoadingScreenPresenter>();
		}

		private void InvalidScreen()
		{
			throw new InvalidOperationException($"The current screen '{_currentScreen}' is invalid");
		}

		private void OpenUiVfxPresenter()
		{
			_uiService.OpenUi<UiVfxPresenter>();
		}

		private void PlayButtonClicked()
		{
			_statechartTrigger(_playClickedEvent);
		}

		private void RoomJoinCreateCloseClicked()
		{
			_statechartTrigger(_roomJoinCreateCloseClickedEvent);
		}

		private void OnTabClickedCallback<T>() where T : UiPresenter
		{
			var type = typeof(T);

			if (_currentScreen == type)
			{
				_statechartTrigger(_currentTabButtonClickedEvent);
				return;
			}

			_currentScreen = type;

			_statechartTrigger(_tabButtonClickedEvent);
		}

		private void SetCurrentScreen<T>() where T : UiPresenter
		{
			_currentScreen = typeof(T);
		}

		private async void LoadMainMenu()
		{
			var uiVfxService = new UiVfxService(_services.AssetResolverService);
			var mainMenuServices = new MainMenuServices(uiVfxService, _services.RemoteTextureService);
			var configProvider = _services.ConfigsProvider;

			MainInstaller.Bind<IMainMenuServices>(mainMenuServices);
			
			_assetAdderService.AddConfigs(configProvider.GetConfig<MainMenuAssetConfigs>());
			_uiService.GetUi<LoadingScreenPresenter>().SetLoadingPercentage(0.5f);

			await _services.AudioFxService.LoadAudioClips(configProvider.GetConfig<AudioMainMenuAssetConfigs>().ConfigsDictionary);
			await _services.AssetResolverService.LoadScene(SceneId.MainMenu, LoadSceneMode.Additive);
			
			_uiService.GetUi<LoadingScreenPresenter>().SetLoadingPercentage(0.8f);

			await _uiService.LoadGameUiSet(UiSetId.MainMenuUi, 0.9f);

			uiVfxService.Init(_uiService);
			
			_statechartTrigger(MainMenuLoadedEvent);
		}
		
		private async void UnloadMainMenu()
		{
			var configProvider = _services.ConfigsProvider;

			Camera.main.gameObject.SetActive(false);
			_uiService.UnloadUiSet((int) UiSetId.MainMenuUi);
			_services.AudioFxService.DetachAudioListener();

			await Task.Delay(1000); // Delays 1 sec to play the loading screen animation
			await _services.AssetResolverService.UnloadScene(SceneId.MainMenu);

			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMainMenuAssetConfigs>().ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MainMenuAssetConfigs>());

			Resources.UnloadUnusedAssets();
			MainInstaller.CleanDispose<IMainMenuServices>();

			_statechartTrigger(MainMenuUnloadedEvent);
		}
	}
}