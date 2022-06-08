using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.NativeUi;
using FirstLight.Services;
using FirstLight.Statechart;
using FirstLight.UiService;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Main Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class MainMenuState
	{
		private readonly IStatechartEvent _tabButtonClickedEvent = new StatechartEvent("Tab Button Clicked Event");

		private readonly IStatechartEvent _currentTabButtonClickedEvent =
			new StatechartEvent("Current Tab Button Clicked Event");

		private readonly IStatechartEvent _playClickedEvent = new StatechartEvent("Play Clicked Event");

		private readonly IStatechartEvent _settingsMenuClickedEvent =
			new StatechartEvent("Settings Menu Button Clicked Event");

		private readonly IStatechartEvent _settingsCloseClickedEvent =
			new StatechartEvent("Settings Close Button Clicked Event");

		private readonly IStatechartEvent _roomJoinCreateClickedEvent =
			new StatechartEvent("Room Join Create Button Clicked Event");

		private readonly IStatechartEvent _nameChangeClickedEvent = new StatechartEvent("Name Change Clicked Event");
		private readonly IStatechartEvent _chooseGameModeClickedEvent = new StatechartEvent("Game Mode Clicked Event");

		private readonly IStatechartEvent _roomJoinCreateCloseClickedEvent =
			new StatechartEvent("Room Join Create Close Button Clicked Event");

		private readonly IStatechartEvent _gameCompletedCheatEvent = new StatechartEvent("Game Completed Cheat Event");

		private readonly IStatechartEvent _logoutConfirmClickedEvent =
			new StatechartEvent("Logout Confirm Clicked Event");

		private readonly IStatechartEvent _logoutFailedEvent = new StatechartEvent("Logout Failed Event");
		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IDataService _dataService;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly LootMenuState _lootMenuState;
		private readonly EnterNameState _enterNameState;
		private Type _currentScreen;

		public MainMenuState(IGameServices services, IDataService dataService, IGameUiService uiService,
		                     IGameDataProvider gameDataProvider,
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_dataService = dataService;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_assetAdderService = assetAdderService;
			_statechartTrigger = statechartTrigger;
			_lootMenuState = new LootMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_enterNameState = new EnterNameState(services, uiService, gameDataProvider, statechartTrigger);
		}

		/// <summary>
		/// Setups the Main Menu state
		/// </summary>
		public void Setup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var mainMenuLoading = stateFactory.TaskWait("Main Menu Loading");
			var mainMenuUnloading = stateFactory.TaskWait("Main Menu Unloading");
			var mainMenu = stateFactory.Nest("Main Menu");
			var mainMenuTransition = stateFactory.Transition("Main Transition");

			initial.Transition().Target(mainMenuLoading);
			initial.OnExit(SubscribeEvents);

			mainMenuLoading.WaitingFor(LoadMainMenu).Target(mainMenu);
			mainMenuLoading.OnExit(LoadingComplete);

			mainMenu.Nest(TabsMenuSetup).Target(mainMenuUnloading);
			mainMenu.Event(_tabButtonClickedEvent).Target(mainMenuTransition);

			mainMenuTransition.Transition().Target(mainMenu);

			mainMenuUnloading.OnEnter(OpenLoadingScreen);
			mainMenuUnloading.WaitingFor(UnloadMainMenu).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void TabsMenuSetup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var screenCheck = stateFactory.Choice("Main Screen Check");
			var claimUnclaimedRewards = stateFactory.Transition("Claim Unclaimed Rewards");
			var homeMenu = stateFactory.State("Home Menu");
			var lootMenu = stateFactory.Nest("Loot Menu");
			var heroesMenu = stateFactory.State("Heroes Menu");
			var socialMenu = stateFactory.State("Social Menu");
			var settingsMenu = stateFactory.State("Settings Menu");
			var logoutWait = stateFactory.State("Wait For Logout");
			var playClickedCheck = stateFactory.Choice("Play Button Clicked Check");
			var roomWaitingState = stateFactory.State("Room Joined Check");
			var chooseGameMode = stateFactory.Wait("Enter Choose Game Mode");
			var enterNameDialogToMenu = stateFactory.Nest("Enter Name Dialog to Menu");
			var enterNameDialogToMatch = stateFactory.Nest("Enter Name Dialog Match");
			var roomJoinCreateMenu = stateFactory.State("Room Join Create Menu");
			var postNameCheck = stateFactory.Choice("Post Name Check");

			initial.Transition().Target(screenCheck);
			initial.OnExit(OpenUiVfxPresenter);

			screenCheck.Transition().Condition(HasUncollectedRewards).Target(claimUnclaimedRewards);
			screenCheck.Transition().Condition(IsCurrentScreen<HomeScreenPresenter>).Target(homeMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<LootScreenPresenter>).Target(lootMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<PlayerSkinScreenPresenter>).Target(heroesMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<SocialScreenPresenter>).Target(socialMenu);
			screenCheck.Transition().OnTransition(InvalidScreen).Target(final);

			claimUnclaimedRewards.OnEnter(ClaimUncollectedRewards);
			claimUnclaimedRewards.Transition().Target(screenCheck);

			homeMenu.OnEnter(OpenMainMenuUi);
			homeMenu.OnEnter(OpenPlayMenuUI);
			homeMenu.Event(_playClickedEvent).Target(playClickedCheck);
			homeMenu.Event(_settingsMenuClickedEvent).Target(settingsMenu);
			homeMenu.Event(_gameCompletedCheatEvent).Target(screenCheck);
			homeMenu.Event(_roomJoinCreateClickedEvent).Target(roomJoinCreateMenu);
			homeMenu.Event(_nameChangeClickedEvent).Target(enterNameDialogToMenu);
			homeMenu.Event(_chooseGameModeClickedEvent).Target(chooseGameMode);
			homeMenu.OnExit(ClosePlayMenuUI);
			homeMenu.OnExit(CloseMainMenuUI);

			playClickedCheck.Transition().Condition(IsNameNotSet).Target(enterNameDialogToMatch);
			playClickedCheck.Transition().Target(roomWaitingState);

			roomWaitingState.Event(NetworkState.JoinedRoomEvent).Target(final);
			roomWaitingState.Event(NetworkState.JoinRoomFailedEvent).Target(homeMenu);
			roomWaitingState.Event(NetworkState.CreateRoomFailedEvent).Target(homeMenu);

			chooseGameMode.WaitingFor(OpenGameModeSelectionUI).Target(homeMenu);
			chooseGameMode.OnExit(CloseGameModeSelectionUI);

			enterNameDialogToMenu.Nest(_enterNameState.Setup).Target(homeMenu);

			enterNameDialogToMatch.Nest(_enterNameState.Setup).Target(postNameCheck);

			postNameCheck.Transition().Condition(IsInRoom).Target(final);
			postNameCheck.Transition().Target(roomWaitingState);

			settingsMenu.OnEnter(OpenSettingsMenuUI);
			settingsMenu.Event(_settingsCloseClickedEvent).Target(homeMenu);
			settingsMenu.Event(_currentTabButtonClickedEvent).Target(homeMenu);
			settingsMenu.Event(_logoutConfirmClickedEvent).Target(logoutWait);
			settingsMenu.OnExit(CloseSettingsMenuUI);

			logoutWait.OnEnter(TryLogOut);
			logoutWait.Event(_logoutFailedEvent).Target(homeMenu);

			lootMenu.OnEnter(OpenLootMenuUI);
			lootMenu.Nest(_lootMenuState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);
			lootMenu.OnExit(CloseLootMenuUI);

			heroesMenu.OnEnter(OpenHeroesMenuUI);
			heroesMenu.OnExit(CloseHeroesMenuUI);

			roomJoinCreateMenu.OnEnter(OpenRoomJoinCreateMenuUI);
			roomJoinCreateMenu.Event(_playClickedEvent).Target(roomWaitingState);
			roomJoinCreateMenu.Event(_roomJoinCreateCloseClickedEvent).Target(homeMenu);
			roomJoinCreateMenu.Event(NetworkState.JoinRoomFailedEvent).Target(homeMenu);
			roomJoinCreateMenu.Event(NetworkState.CreateRoomFailedEvent).Target(homeMenu);
			roomJoinCreateMenu.OnExit(CloseRoomJoinCreateMenuUI);

			socialMenu.OnEnter(OpenSocialMenuUI);
			socialMenu.OnExit(CloseSocialMenuUI);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<UnclaimedRewardsCollectedMessage>(OnRewardsCollectedMessage);
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.PlayerDataProvider?.Level.StopObservingAll(this);
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_statechartTrigger(_gameCompletedCheatEvent);
		}

		private void OnRewardsCollectedMessage(UnclaimedRewardsCollectedMessage message)
		{
			var position = _uiService.GetUi<GenericDialogIconPresenter>().IconPosition.position;

			foreach (var reward in message.Rewards)
			{
				_services.MessageBrokerService.Publish(new PlayUiVfxMessage
				{
					Id = reward.RewardId,
					OriginWorldPosition = position,
					Quantity = (uint) reward.Value
				});
			}
		}

		private bool HasUncollectedRewards()
		{
			return _gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0;
		}

		private void ClaimUncollectedRewards()
		{
			_services.CommandService.ExecuteCommand(new CollectUnclaimedRewardsCommand());
		}

		private bool IsInRoom()
		{
			return _services.NetworkService.QuantumClient.InRoom;
		}

		private bool IsNameNotSet()
		{
			return _gameDataProvider.AppDataProvider.Nickname == PlayerLogic.DefaultPlayerName ||
			       string.IsNullOrEmpty(_gameDataProvider.AppDataProvider.Nickname);
		}

		private bool IsCurrentScreen<T>() where T : UiPresenter
		{
			return _currentScreen == typeof(T);
		}

		private void OpenGameModeSelectionUI(IWaitActivity activity)
		{
			var cacheActivity = activity;

			var data = new GameModeSelectionPresenter.StateData
			{
				GameModeChosen = () => { cacheActivity.Complete(); }
			};

			_uiService.OpenUi<GameModeSelectionPresenter, GameModeSelectionPresenter.StateData>(data);
		}

		private void CloseGameModeSelectionUI()
		{
			_uiService.CloseUi<GameModeSelectionPresenter>();
		}

		private void OpenLootMenuUI()
		{
			var data = new LootScreenPresenter.StateData
			{
				OnLootBackButtonClicked = OnTabClickedCallback<LootScreenPresenter>,
			};

			_uiService.OpenUi<LootScreenPresenter, LootScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new LootScreenOpenedMessage());
		}

		private void CloseLootMenuUI()
		{
			_uiService.CloseUi<LootScreenPresenter>();
			_services.MessageBrokerService.Publish(new LootScreenClosedMessage());
		}

		private void OpenHeroesMenuUI()
		{
			var data = new PlayerSkinScreenPresenter.StateData
			{
				OnCloseClicked = OnTabClickedCallback<HomeScreenPresenter>,
			};

			_uiService.OpenUi<PlayerSkinScreenPresenter, PlayerSkinScreenPresenter.StateData>(data);
		}

		private void CloseHeroesMenuUI()
		{
			_uiService.CloseUi<PlayerSkinScreenPresenter>();
		}

		private void OpenRoomJoinCreateMenuUI()
		{
			var data = new RoomJoinCreateScreenPresenter.StateData
			{
				CloseClicked = RoomJoinCreateCloseClicked,
				PlayClicked = PlayButtonClicked
			};

			_uiService.OpenUi<RoomJoinCreateScreenPresenter, RoomJoinCreateScreenPresenter.StateData>(data);
		}

		private void CloseRoomJoinCreateMenuUI()
		{
			_uiService.CloseUi<RoomJoinCreateScreenPresenter>();
		}

		private void OpenPlayMenuUI()
		{
			var data = new HomeScreenPresenter.StateData
			{
				OnPlayButtonClicked = PlayButtonClicked,
				OnSettingsButtonClicked = () => _statechartTrigger(_settingsMenuClickedEvent),
				OnLootButtonClicked = OnTabClickedCallback<LootScreenPresenter>,
				OnHeroesButtonClicked = OnTabClickedCallback<PlayerSkinScreenPresenter>,
				OnSocialButtonClicked = OnTabClickedCallback<SocialScreenPresenter>,
				OnPlayRoomJoinCreateClicked = () => _statechartTrigger(_roomJoinCreateClickedEvent),
				OnNameChangeClicked = () => _statechartTrigger(_nameChangeClickedEvent),
				OnGameModeClicked = () => _statechartTrigger(_chooseGameModeClickedEvent),
			};

			_uiService.OpenUi<HomeScreenPresenter, HomeScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new PlayScreenOpenedMessage());
		}

		private void ClosePlayMenuUI()
		{
			_uiService.CloseUi<HomeScreenPresenter>();
		}

		private void OpenSocialMenuUI()
		{
			var data = new SocialScreenPresenter.StateData
			{
				OnSocialBackButtonClicked = OnTabClickedCallback<HomeScreenPresenter>,
			};

			_uiService.OpenUi<SocialScreenPresenter, SocialScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new SocialScreenOpenedMessage());
		}

		private void CloseSocialMenuUI()
		{
			_uiService.CloseUi<SocialScreenPresenter>();
		}

		private void OpenSettingsMenuUI()
		{
			var data = new SettingsScreenPresenter.StateData
			{
				LogoutClicked = TryLogOut,
				OnClose = CloseScreen
			};

			_uiService.OpenUi<SettingsScreenPresenter, SettingsScreenPresenter.StateData>(data);

			void CloseScreen()
			{
				_statechartTrigger(_settingsCloseClickedEvent);
			}
		}

		private void CloseSettingsMenuUI()
		{
			_uiService.CloseUi<SettingsScreenPresenter>();
		}

		private void LoadingComplete()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
			SetCurrentScreen<HomeScreenPresenter>();
		}

		private void OpenLoadingScreen()
		{
			_uiService.OpenUi<LoadingScreenPresenter>();
		}

		private void InvalidScreen()
		{
			throw new InvalidOperationException($"The current screen '{_currentScreen}' is invalid");
		}

		private void CloseMainMenuUI()
		{
			_uiService.CloseUi<MainMenuHudPresenter>();
		}

		private void OpenMainMenuUi()
		{
			_uiService.OpenUi<MainMenuHudPresenter>();
		}

		private void OpenUiVfxPresenter()
		{
			_uiService.OpenUi<UiVfxPresenter>();
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

		private async Task LoadMainMenu()
		{
			var uiVfxService = new UiVfxService(_services.AssetResolverService);
			var mainMenuServices = new MainMenuServices(uiVfxService, _services.RemoteTextureService);
			var configProvider = _services.ConfigsProvider;

			MainMenuInstaller.Bind<IMainMenuServices>(mainMenuServices);

			_assetAdderService.AddConfigs(configProvider.GetConfig<AudioMainMenuAssetConfigs>());
			_assetAdderService.AddConfigs(configProvider.GetConfig<MainMenuAssetConfigs>());
			_uiService.GetUi<LoadingScreenPresenter>().SetLoadingPercentage(0.5f);

			await _services.AssetResolverService.LoadScene(SceneId.MainMenu, LoadSceneMode.Additive);

			_services.AudioFxService.AudioListener.transform.SetParent(Camera.main.transform);
			_uiService.GetUi<LoadingScreenPresenter>().SetLoadingPercentage(0.8f);

			await _uiService.LoadGameUiSet(UiSetId.MainMenuUi, 0.9f);

			uiVfxService.Init(_uiService);
			_services.AudioFxService.PlayMusic(AudioId.MenuMainLoop);
		}

		private async Task UnloadMainMenu()
		{
			var mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			var configProvider = _services.ConfigsProvider;

			Camera.main.gameObject.SetActive(false);
			_uiService.UnloadUiSet((int) UiSetId.MainMenuUi);
			_services.AudioFxService.DetachAudioListener();

			await Task.Delay(1000); // Delays 1 sec to play the loading screen animation
			await _services.AssetResolverService.UnloadScene(SceneId.MainMenu);

			_services.VfxService.DespawnAll();
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<AudioMainMenuAssetConfigs>());
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MainMenuAssetConfigs>());
			mainMenuServices.Dispose();
			Resources.UnloadUnusedAssets();
			MainMenuInstaller.Clean();
		}

		private void PlayButtonClicked()
		{
			_statechartTrigger(_playClickedEvent);
		}

		private void RoomJoinCreateCloseClicked()
		{
			_statechartTrigger(_roomJoinCreateCloseClickedEvent);
		}

		private void TryLogOut()
		{
#if UNITY_EDITOR
			var unlink = new UnlinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier
			};

			PlayFabClientAPI.UnlinkCustomID(unlink, OnUnlinkSuccess, OnUnlinkFail);

			void OnUnlinkSuccess(UnlinkCustomIDResult result)
			{
				UnlinkComplete();
			}
#elif UNITY_ANDROID
			var unlink = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			
			PlayFabClientAPI.UnlinkAndroidDeviceID(unlink,OnUnlinkSuccess,OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkAndroidDeviceIDResult result)
			{
				UnlinkComplete();
			}
#elif UNITY_IOS
			var unlink = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};

			PlayFabClientAPI.UnlinkIOSDeviceID(unlink, OnUnlinkSuccess, OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkIOSDeviceIDResult result)
			{
				UnlinkComplete();
			}
#endif
			void OnUnlinkFail(PlayFabError error)
			{
				_services.AnalyticsService.CrashLog(error.ErrorMessage);

				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = () => { _statechartTrigger(_logoutFailedEvent); }
				};

				_services.GenericDialogService.OpenDialog(error.ErrorMessage, false, confirmButton);
			}

			void UnlinkComplete()
			{
				_gameDataProvider.AppDataProvider.SetLastLoginEmail("");
				_gameDataProvider.AppDataProvider.SetDeviceLinkedStatus(false);

#if UNITY_EDITOR
				var title = string.Format(ScriptLocalization.MainMenu.LogoutSuccessDesc);
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.MainMenu.QuitGameButton,
					ButtonOnClick = () => { UnityEditor.EditorApplication.isPlaying = false; }
				};

				_services.GenericDialogService.OpenDialog(title, false, confirmButton);
				return;
#else
				var button = new AlertButton
				{
					Callback = Application.Quit,
					Style = AlertButtonStyle.Positive,
					Text = ScriptLocalization.MainMenu.QuitGameButton
				};

				NativeUiService.ShowAlertPopUp(false, ScriptLocalization.MainMenu.LogoutSuccessTitle,
				                               ScriptLocalization.MainMenu.LogoutSuccessDesc, button);
#endif
			}
		}
	}
}