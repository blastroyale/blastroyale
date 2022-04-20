using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.MainMenu;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Services;
using FirstLight.Statechart;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace FirstLight.Game.StateMachines
{
	/// <summary>
	/// This object contains the behaviour logic for the Main Menu State in the <seealso cref="GameStateMachine"/>
	/// </summary>
	public class MainMenuState
	{
		private readonly IStatechartEvent _tabButtonClickedEvent = new StatechartEvent("Tab Button Clicked Event");
		private readonly IStatechartEvent _currentTabButtonClickedEvent = new StatechartEvent("Current Tab Button Clicked Event");
		private readonly IStatechartEvent _playClickedEvent = new StatechartEvent("Play Clicked Event");
		private readonly IStatechartEvent _settingsMenuClickedEvent = new StatechartEvent("Settings Menu Button Clicked Event");
		private readonly IStatechartEvent _settingsCloseClickedEvent = new StatechartEvent("Settings Close Button Clicked Event");
		private readonly IStatechartEvent _roomJoinCreateClickedEvent = new StatechartEvent("Room Join Create Button Clicked Event");
		private readonly IStatechartEvent _roomJoinCreateCloseClickedEvent = new StatechartEvent("Room Join Create Close Button Clicked Event");
		private readonly IStatechartEvent _closeOverflowScreenClickedEvent = new StatechartEvent("Close Overflow Loot Screen Clicked Event");
		private readonly IStatechartEvent _speedUpOverflowCratesClickedEvent = new StatechartEvent("Speed Up Overflow Clicked Event");
		private readonly IStatechartEvent _gameCompletedCheatEvent = new StatechartEvent("Game Completed Cheat Event");
		private readonly IStatechartEvent _roomErrorDismissClicked = new StatechartEvent("Room Error Dismiss Clicked");
		
		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly LootOptionsMenuState _lootOptionsMenuState;
		private readonly LootMenuState _lootMenuState;
		private readonly CratesMenuState _cratesMenuState;
		private readonly CollectLootRewardState _collectLootRewardState;
		private readonly TrophyRoadMenuState _trophyRoadState;
		private readonly ShopMenuState _shopMenuState;
		private Type _currentScreen;

		public MainMenuState(IGameServices services, IGameUiService uiService, IGameDataProvider gameDataProvider,
		                     IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameDataProvider;
			_assetAdderService = assetAdderService;
			_statechartTrigger = statechartTrigger;
			_lootOptionsMenuState = new LootOptionsMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_lootMenuState = new LootMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_trophyRoadState = new TrophyRoadMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_cratesMenuState = new CratesMenuState(services, uiService, gameDataProvider, statechartTrigger);
			_collectLootRewardState = new CollectLootRewardState(services, statechartTrigger, _gameDataProvider);
			_shopMenuState = new ShopMenuState(services, uiService, _gameDataProvider, statechartTrigger);
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

			mainMenu.OnEnter(OpenMainMenuUi);
			mainMenu.Nest(TabsMenuSetup).Target(mainMenuUnloading);
			mainMenu.Event(_tabButtonClickedEvent).Target(mainMenuTransition);
			
			mainMenuTransition.Transition().Target(mainMenu);

			mainMenuUnloading.WaitingFor(UnloadMainMenu).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void TabsMenuSetup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var screenCheck = stateFactory.Choice("Main Screen Check");
			var overflowLootMenu = stateFactory.State("Overflow Loot Menu");
			var claimUnclaimedRewards = stateFactory.Transition("Claim Unclaimed Rewards");
			var homeMenu = stateFactory.State("Home Menu");
			var shopMenu = stateFactory.Nest("Shop Menu");
			var lootOptionsMenu = stateFactory.Nest("Loot Options Menu");
			var lootMenu = stateFactory.Nest("Loot Menu");
			var heroesMenu = stateFactory.State("Heroes Menu");
			var trophyRoadMenu = stateFactory.Nest("Trophy Road Menu");
			var collectLoot = stateFactory.Nest("Collect Loot Menu");
			var cratesMenu = stateFactory.Nest("Crates Menu");
			var socialMenu = stateFactory.State("Social Menu");
			var settingsMenu = stateFactory.State("Settings Menu");
			var playClickedCheck = stateFactory.Choice("Play Button Clicked Check");
			var roomWaitingState = stateFactory.State("Room Joined Check");
			var enterNameDialog = stateFactory.Wait("Enter Name Dialog");
			var roomJoinCreateMenu = stateFactory.State("Room Join Create Menu");
			var postNameCheck = stateFactory.Choice("Post Name Check");
			initial.Transition().Target(screenCheck);
			initial.OnExit(OpenUiVfxPresenter);
			
			screenCheck.Transition().Condition(CheckAutoLootBoxes).Target(collectLoot);
			screenCheck.Transition().Condition(CheckOverflowLootSpentHc).Target(collectLoot);
			screenCheck.Transition().Condition(CheckOverflowLoot).Target(overflowLootMenu);
			screenCheck.Transition().Condition(CheckUnclaimedRewards).Target(claimUnclaimedRewards);
			screenCheck.Transition().Condition(IsCurrentScreen<HomeScreenPresenter>).Target(homeMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<CratesScreenPresenter>).Target(cratesMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<ShopScreenPresenter>).Target(shopMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<LootOptionsScreenPresenter>).Target(lootOptionsMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<LootScreenPresenter>).Target(lootMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<PlayerSkinScreenPresenter>).Target(heroesMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<SocialScreenPresenter>).Target(socialMenu);
			screenCheck.Transition().Condition(IsCurrentScreen<TrophyRoadScreenPresenter>).Target(trophyRoadMenu);
			screenCheck.Transition().OnTransition(InvalidScreen).Target(final);
			
			overflowLootMenu.OnEnter(OpenOverflowLootScreen);
			overflowLootMenu.Event(_closeOverflowScreenClickedEvent).Target(screenCheck);
			overflowLootMenu.Event(_speedUpOverflowCratesClickedEvent).Target(collectLoot);
			overflowLootMenu.OnExit(CloseOverflowLootScreen);

			claimUnclaimedRewards.OnEnter(StartClaimRewards);
			claimUnclaimedRewards.Transition().Target(screenCheck);
			
			homeMenu.OnEnter(OpenPlayMenuUI);
			homeMenu.Event(_playClickedEvent).Target(playClickedCheck);
			homeMenu.Event(_settingsMenuClickedEvent).Target(settingsMenu);
			homeMenu.Event(_gameCompletedCheatEvent).Target(screenCheck);
			homeMenu.Event(_roomJoinCreateClickedEvent).Target(roomJoinCreateMenu);
			homeMenu.OnExit(ClosePlayMenuUI);
			
			playClickedCheck.OnEnter(SendPlayClickedEvent);
			playClickedCheck.Transition().Condition(IsNameNotSet).Target(enterNameDialog);
			playClickedCheck.Transition().Target(roomWaitingState);
			
			roomWaitingState.Event(NetworkState.JoinedRoomEvent).Target(final);
			roomWaitingState.Event(NetworkState.JoinRoomFailedEvent).Target(homeMenu);
			roomWaitingState.Event(NetworkState.CreateRoomFailedEvent).Target(homeMenu);
			
			enterNameDialog.WaitingFor(OpenEnterNameDialog).Target(postNameCheck);
			enterNameDialog.OnExit(CloseEnterNameDialog);
			
			postNameCheck.Transition().Condition(IsInRoom).Target(final);
			postNameCheck.Transition().Target(roomWaitingState);
			
			settingsMenu.OnEnter(OpenSettingsMenuUI);
			settingsMenu.Event(_settingsCloseClickedEvent).Target(homeMenu);
			settingsMenu.Event(_currentTabButtonClickedEvent).Target(homeMenu);

			shopMenu.OnEnter(OpenShopMenuUI);
			shopMenu.Nest(_shopMenuState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);
			shopMenu.OnExit(CloseShopMenuUI);

			lootOptionsMenu.OnEnter(OpenLootOptionsMenuUI);
			lootOptionsMenu.Nest(_lootOptionsMenuState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);
			lootOptionsMenu.OnExit(CloseLootOptionsMenuUI);

			lootMenu.OnEnter(OpenLootMenuUI);
			lootMenu.Nest(_lootMenuState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);
			lootMenu.OnExit(CloseLootMenuUI);
			
			heroesMenu.OnEnter(OpenHeroesMenuUI);
			heroesMenu.OnExit(CloseHeroesMenuUI);
			
			trophyRoadMenu.OnEnter(OpenTrophyRoadMenuUI);
			trophyRoadMenu.Nest(_trophyRoadState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);
			trophyRoadMenu.OnExit(CloseTrophyRoadMenuUI);

			roomJoinCreateMenu.OnEnter(OpenRoomJoinCreateMenuUI);
			roomJoinCreateMenu.Event(_playClickedEvent).Target(roomWaitingState);
			roomJoinCreateMenu.Event(_roomJoinCreateCloseClickedEvent).Target(homeMenu);
			roomJoinCreateMenu.Event(NetworkState.JoinRoomFailedEvent).Target(homeMenu);
			roomJoinCreateMenu.Event(NetworkState.CreateRoomFailedEvent).Target(homeMenu);
			roomJoinCreateMenu.OnExit(CloseRoomJoinCreateMenuUI);
			
			collectLoot.OnEnter(CloseMainMenuUI);
			collectLoot.Nest(_collectLootRewardState.Setup).Target(screenCheck);
			collectLoot.OnExit(OpenMainMenuUi);
			
			cratesMenu.Nest(_cratesMenuState.Setup).OnTransition(SetCurrentScreen<HomeScreenPresenter>).Target(screenCheck);
			cratesMenu.OnExit(CloseCratesMenuUI);
			
			socialMenu.OnEnter(OpenSocialMenuUI);
			socialMenu.OnExit(CloseSocialMenuUI);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<UnclaimedRewardsCollectedMessage>(OnRewardsCollectedMessage);
			_services.MessageBrokerService.Subscribe<MenuWorldLootBoxClickedMessage>(OnRequestOpenCratesScreenMessage);
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_gameDataProvider?.PlayerDataProvider?.Level.StopObservingAll(this);
		}

		private void OnRequestOpenCratesScreenMessage(MenuWorldLootBoxClickedMessage message)
		{
			OnTabClickedCallback<CratesScreenPresenter>();
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
				_services.MessageBrokerService.Publish(new PlayUiVfxCommandMessage
				{
					Id = reward.RewardId,
					OriginWorldPosition = position,
					Quantity = (uint) reward.Value
				});
			}
		}
		
		private void StartClaimRewards()
		{
			_services.CommandService.ExecuteCommand(new CollectUnclaimedRewardsCommand());
		}
		
		/// <summary>
		/// The player might have collected too many loot boxes and all slots are full. In that case, let them know about it so they can
		/// open up all excess Loot Boxes immediately. 
		/// </summary>
		private bool CheckOverflowLoot()
		{
			return _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo().TimedBoxExtra.Count > 0;
		}

		private bool CheckOverflowLootSpentHc()
		{
			var lootBoxExtraInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo().TimedBoxExtra;
			var time = _services.TimeService.DateTimeUtcNow;

			if (lootBoxExtraInfo.Count > 0)
			{
				var list = lootBoxExtraInfo.ConvertAll(info => info.Data.Id);
				
				_collectLootRewardState.SetLootBoxToOpen(list);
				return lootBoxExtraInfo.Count > 0 & lootBoxExtraInfo[0].GetState(time) == LootBoxState.Unlocked;
			}
			
			return false;
		}

		private bool CheckUnclaimedRewards()
		{
			return _gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0;
		}

		private bool IsConnectedAndReady()
		{
			return _services.NetworkService.QuantumClient.IsConnectedAndReady;
		}

		/// <summary>
		/// Checks to see if we have anything in one of our auto loot boxes, which are used as a temporary holding place for
		/// boxes that are acquired that do not count toward regular loot box slots. 
		/// </summary>
		private bool CheckAutoLootBoxes()
		{
			var autoLoot = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo().CoreBoxes;

			if (autoLoot.Count > 0)
			{
				var list = autoLoot.ConvertAll(info => info.Data.Id);
				
				_collectLootRewardState.SetLootBoxToOpen(list);
			}
			
			return autoLoot.Count > 0;
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

		private void OpenOverflowLootScreen()
		{
			var data = new OverflowLootDialogPresenter.StateData
			{
				CloseClicked = Close, 
				SpeedUpAllBoxes = OpenOverflowCrates
			};

			_uiService.OpenUi<OverflowLootDialogPresenter, OverflowLootDialogPresenter.StateData>(data);

			void OpenOverflowCrates()
			{
				var inventoryInfo = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
				var list = inventoryInfo.TimedBoxExtra.ConvertAll(info => info.Data.Id);
				
				_services.CommandService.ExecuteCommand(new SpeedUpAllExtraTimedBoxesCommand());
				_collectLootRewardState.SetLootBoxToOpen(list);
				_statechartTrigger(_speedUpOverflowCratesClickedEvent);
			}

			void Close()
			{
				_services.CommandService.ExecuteCommand(new CleanExtraTimedBoxesCommand());
				_statechartTrigger(_closeOverflowScreenClickedEvent);
			}
		}

		private void CloseOverflowLootScreen()
		{
			_uiService.CloseUi<OverflowLootDialogPresenter>();
		}

		private void OpenLootOptionsMenuUI()
		{
			var data = new LootOptionsScreenPresenter.StateData
			{
				OnLootBackButtonClicked = OnTabClickedCallback<LootOptionsScreenPresenter>,
			};

			_uiService.OpenUi<LootOptionsScreenPresenter, LootOptionsScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new LootScreenOpenedMessage());
		}
		
		private void CloseLootOptionsMenuUI()
		{
			_uiService.CloseUi<LootOptionsScreenPresenter>();
			_services.MessageBrokerService.Publish(new LootScreenClosedMessage());
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

		private void OpenTrophyRoadMenuUI()
		{
			var data = new TrophyRoadScreenPresenter.StateData
			{
				OnTrophyRoadClosedClicked = OnTabClickedCallback<TrophyRoadScreenPresenter>,
			};
			
			_uiService.OpenUi<TrophyRoadScreenPresenter, TrophyRoadScreenPresenter.StateData>(data);
		}
		
		private void CloseTrophyRoadMenuUI()
		{
			_uiService.CloseUi<TrophyRoadScreenPresenter>();
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
		
		private void OpenRoomErrorUI()
		{
			var title = string.Format(ScriptLocalization.MainMenu.RoomErrorCreate, "message");
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenDialog(title, false, confirmButton);
		}

		private void CloseCratesMenuUI()
		{
			_uiService.CloseUi<CratesScreenPresenter>();
			_services.MessageBrokerService.Publish(new CratesScreenClosedMessage());
		}
		
		private void OpenPlayMenuUI()
		{
			var data = new HomeScreenPresenter.StateData
			{
				OnPlayButtonClicked = PlayButtonClicked,
				OnSettingsButtonClicked = () => _statechartTrigger(_settingsMenuClickedEvent),
				OnLootButtonClicked = OnTabClickedCallback<LootScreenPresenter>,
				OnHeroesButtonClicked = OnTabClickedCallback<PlayerSkinScreenPresenter>,
				OnCratesButtonClicked = OnTabClickedCallback<CratesScreenPresenter>,
				OnSocialButtonClicked = OnTabClickedCallback<SocialScreenPresenter>,
				OnShopButtonClicked = OnTabClickedCallback<ShopScreenPresenter>,
				OnTrophyRoadClicked = OnTabClickedCallback<TrophyRoadScreenPresenter>,
				OnRoomJoinCreateClicked = () => _statechartTrigger(_roomJoinCreateClickedEvent),
			};
			
			_uiService.OpenUi<HomeScreenPresenter, HomeScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new PlayScreenOpenedMessage());
		}

		private void ClosePlayMenuUI()
		{
			_uiService.CloseUi<HomeScreenPresenter>();
		}

		private void OpenShopMenuUI()
		{
			var data = new ShopScreenPresenter.StateData
			{
				OnShopBackButtonClicked = OnTabClickedCallback<HomeScreenPresenter>,
			};

			_uiService.OpenUi<ShopScreenPresenter, ShopScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new ShopScreenOpenedMessage());
		}

		private void CloseShopMenuUI()
		{
			_uiService.CloseUi<ShopScreenPresenter>();
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

		private void OpenEnterNameDialog(IWaitActivity activity)
		{
			var closureActivity = activity;
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.Yes,
				ButtonOnClick = OnNameSet
			};
			
			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.NameHeroTitle, 
			                                                    _gameDataProvider.AppDataProvider.Nickname, 
			                                                    confirmButton, false, OnNameSet);

			void OnNameSet(string newName)
			{
				_services.PlayfabService.UpdateNickname(newName);
				closureActivity.Complete();
			}
		}

		private void CloseEnterNameDialog()
		{
			_services.GenericDialogService.CloseDialog();
		}

		private void OpenSettingsMenuUI()
		{
			_uiService.OpenUi<SettingsScreenPresenter, ActionStruct>(new ActionStruct(CloseScreen));

			void CloseScreen()
			{
				_statechartTrigger(_settingsCloseClickedEvent);
			}
		}

		private void OpenMatchmakingLoadingScreen()
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

		private void LoadingComplete()
		{
			_uiService.CloseUi<LoadingScreenPresenter>();
			SetCurrentScreen<HomeScreenPresenter>();
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
			var mainMenuServices = new MainMenuServices(_services.AssetResolverService, uiVfxService, _services.MessageBrokerService);
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

			await _uiService.LoadUiAsync<MatchmakingLoadingScreenPresenter>();
			
			Camera.main.gameObject.SetActive(false);
			_uiService.UnloadUiSet((int)UiSetId.MainMenuUi);
			_services.AudioFxService.DetachAudioListener();
			OpenMatchmakingLoadingScreen();
			
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

		private void SendPlayClickedEvent()
		{
			var config = _gameDataProvider.AppDataProvider.CurrentMapConfig;
			
			var dictionary = new Dictionary<string, object> 
			{
				{"player_level", _gameDataProvider.PlayerDataProvider.Level.Value},
				{"map_id", config.Id},
				{"map_name", config.Map},
			};
			
			_services.AnalyticsService.LogEvent("play_clicked", dictionary);
		}
	}
}