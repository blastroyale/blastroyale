using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Statechart;
using I2.Loc;
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
		public static readonly IStatechartEvent MainMenuLoadedEvent = new StatechartEvent("Main Menu Loaded Event");
		public static readonly IStatechartEvent PlayClickedEvent = new StatechartEvent("Play Clicked Event");
		public static readonly IStatechartEvent BattlePassClickedEvent = new StatechartEvent("BattlePass Clicked Event");

		private readonly IStatechartEvent _settingsMenuClickedEvent = new StatechartEvent("Settings Menu Button Clicked Event");

		private readonly IStatechartEvent _roomJoinCreateClickedEvent = new StatechartEvent("Room Join Create Button Clicked Event");
		private readonly IStatechartEvent _nameChangeClickedEvent = new StatechartEvent("Name Change Clicked Event");
		private readonly IStatechartEvent _chooseGameModeClickedEvent = new StatechartEvent("Game Mode Clicked Event");
		private readonly IStatechartEvent _equipmentClickedEvent = new StatechartEvent("Equipment Clicked Event");
		private readonly IStatechartEvent _collectionClickedEvent = new StatechartEvent("Collection Clicked Event");
		private readonly IStatechartEvent _gameModeSelectedFinishedEvent = new StatechartEvent("Game Mode Selected Finished Event");
		private readonly IStatechartEvent _leaderboardClickedEvent = new StatechartEvent("Leaderboard Clicked Event");
		private readonly IStatechartEvent _storeClickedEvent = new StatechartEvent("Store Clicked Event");
		private readonly IStatechartEvent _roomJoinCreateBackClickedEvent = new StatechartEvent("Room Join Create Back Button Clicked Event");
		private readonly IStatechartEvent _closeClickedEvent = new StatechartEvent("Close Button Clicked Event");

		private readonly IStatechartEvent _gameCompletedCheatEvent = new StatechartEvent("Game Completed Cheat Event");
		private readonly IStatechartEvent _brokenItemsCloseEvent = new StatechartEvent("Broken Items Close Event");
		private readonly IStatechartEvent _brokenItemsRepairEvent = new StatechartEvent("Broken Items Repair Event");

		private readonly IGameUiService _uiService;
		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly EquipmentMenuState _equipmentMenuState;
		private readonly SettingsMenuState _settingsMenuState;
		private readonly EnterNameState _enterNameState;
		private readonly CollectionMenuState _collectionMenuState;


		private int _unclaimedCountCheck;

		public MainMenuState(IGameServices services, IGameUiService uiService, IGameLogic gameLogic,
							 IAssetAdderService assetAdderService, Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_uiService = uiService;
			_gameDataProvider = gameLogic;
			_assetAdderService = assetAdderService;
			_statechartTrigger = statechartTrigger;
			_equipmentMenuState = new EquipmentMenuState(services, uiService, gameLogic, statechartTrigger);
			_collectionMenuState = new CollectionMenuState(services, uiService, gameLogic, statechartTrigger);
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
			var mainMenuUnloading = stateFactory.TaskWait("Main Menu Unloading");
			var mainMenu = stateFactory.Nest("Main Menu");
			var mainMenuTransition = stateFactory.Transition("Main Transition");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCheck = stateFactory.Choice("Disconnected Final Choice");

			initial.Transition().Target(mainMenuLoading);
			initial.OnExit(SubscribeEvents);

			mainMenuLoading.OnEnter(LoadMainMenu);
			mainMenuLoading.Event(MainMenuLoadedEvent).Target(mainMenu);
			mainMenuLoading.OnExit(LoadingComplete);

			mainMenu.OnEnter(OnMainMenuLoaded);
			mainMenu.Nest(MainMenuSetup).Target(disconnectedCheck);
			mainMenu.Event(NetworkState.PhotonCriticalDisconnectedEvent).Target(disconnected);

			mainMenuTransition.Transition().Target(mainMenu);

			disconnectedCheck.Transition().Condition(NetworkUtils.IsOfflineOrDisconnected).Target(disconnected);
			disconnectedCheck.Transition().Target(mainMenuUnloading);

			disconnected.OnEnter(OpenDisconnectedScreen);
			disconnected.Event(NetworkState.PhotonMasterConnectedEvent).Target(mainMenu);

			mainMenuUnloading.WaitingFor(UnloadMenuTask).Target(final);

			final.OnEnter(UnsubscribeEvents);
		}

		private void OnMainMenuLoaded()
		{
			_services.MessageBrokerService.Publish(new MainMenuOpenedMessage());
		}

		private void MainMenuSetup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var homeCheck = stateFactory.Choice("Main Screen Check");
			var homeMenu = stateFactory.State("Home Menu");
			var equipmentMenu = stateFactory.Nest("Equipment Menu");
			var collectionMenu = stateFactory.Nest("Collection Menu");
			var settingsMenu = stateFactory.Nest("Settings Menu");
			var playClickedCheck = stateFactory.Choice("Play Button Clicked Check");
			var waitMatchmaking = stateFactory.State("Matchmaking Waiting");
			var chooseGameMode = stateFactory.State("Enter Choose Game Mode");
			var leaderboard = stateFactory.Wait("Leaderboard");
			var battlePass = stateFactory.Wait("BattlePass");
			var store = stateFactory.Wait("Store");
			var enterNameDialog = stateFactory.Nest("Enter Name Dialog");
			var roomJoinCreateMenu = stateFactory.State("Room Join Create Menu");
			var loadoutRestricted = stateFactory.Wait("Loadout Restriction Pop Up");
			var brokenItems = stateFactory.State("Broken Items Pop Up");
			
			void AddGoToMatchmakingHook(params IStateEvent[] states)
			{
				foreach (var state in states)
				{
					state.Event(NetworkState.JoinedPlayfabMatchmaking)
						.OnTransition(OpenHomeScreen)
						.Target(waitMatchmaking);
				}
			}

			initial.Transition().Target(homeCheck);
			initial.OnExit(OpenUiVfxPresenter);
			initial.OnExit(() => FLGCamera.Instance.PhysicsRaycaster.enabled = true);

			homeCheck.Transition().Condition(CheckItemsBroken).Target(brokenItems);
			homeCheck.Transition().Condition(HasDefaultName).Target(enterNameDialog);
			homeCheck.Transition().Condition(MetaTutorialConditionsCheck).Target(enterNameDialog);
			homeCheck.Transition().Target(homeMenu);
			homeCheck.OnExit(OpenHomeScreen);
			
			homeMenu.OnEnter(OpenHomeScreen);
			homeMenu.OnEnter(TryClaimUncollectedRewards);
			homeMenu.Event(PlayClickedEvent).Target(playClickedCheck);
			homeMenu.Event(_settingsMenuClickedEvent).Target(settingsMenu);
			homeMenu.Event(_gameCompletedCheatEvent).Target(homeCheck);
			homeMenu.Event(_nameChangeClickedEvent).Target(enterNameDialog);
			homeMenu.Event(_chooseGameModeClickedEvent).Target(chooseGameMode);
			homeMenu.Event(_leaderboardClickedEvent).Target(leaderboard);
			homeMenu.Event(BattlePassClickedEvent).Target(battlePass);
			homeMenu.Event(_storeClickedEvent).Target(store);
			homeMenu.Event(_equipmentClickedEvent).Target(equipmentMenu);
			homeMenu.Event(_collectionClickedEvent).Target(collectionMenu);
			homeMenu.Event(NetworkState.JoinedPlayfabMatchmaking).Target(waitMatchmaking);
			
			settingsMenu.Nest(_settingsMenuState.Setup).Target(homeCheck);
			equipmentMenu.Nest(_equipmentMenuState.Setup).Target(homeCheck);
			collectionMenu.Nest(_collectionMenuState.Setup).Target(homeCheck);
			battlePass.WaitingFor(OpenBattlePassUI).Target(homeCheck);
			leaderboard.WaitingFor(OpenLeaderboardUI).Target(homeCheck);
			store.WaitingFor(OpenStore).Target(homeCheck);
			AddGoToMatchmakingHook(settingsMenu, equipmentMenu, collectionMenu, battlePass, leaderboard, store);

			playClickedCheck.Transition().Condition(LoadoutCountCheckToPlay).Target(loadoutRestricted);
			playClickedCheck.Transition().Condition(CheckItemsBroken).Target(brokenItems);
			playClickedCheck.Transition().Condition(CheckPartyNotReady).Target(homeCheck);
			playClickedCheck.Transition().Condition(CheckIsNotPartyLeader).OnTransition(TogglePartyReadyStatus)
				.Target(homeCheck);
			playClickedCheck.Transition().OnTransition(SendPlayReadyMessage)
				.Target(waitMatchmaking);

			// Matchmaking
			waitMatchmaking.OnEnter(ShowMatchmaking);
			waitMatchmaking.Event(NetworkState.JoinedRoomEvent).Target(final);
			waitMatchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(HideMatchmaking).Target(homeCheck);
			waitMatchmaking.Event(NetworkState.JoinRoomFailedEvent).OnTransition(HideMatchmaking).Target(homeCheck);
			waitMatchmaking.Event(NetworkState.CreateRoomFailedEvent).Target(homeCheck);
			waitMatchmaking.Event(NetworkState.CanceledMatchmakingEvent)
				.OnTransition(HideMatchmaking)
				.Target(homeCheck);

			chooseGameMode.OnEnter(OpenGameModeSelectionUI);
			chooseGameMode.Event(_gameModeSelectedFinishedEvent).Target(homeCheck);
			chooseGameMode.Event(_roomJoinCreateClickedEvent).Target(roomJoinCreateMenu);

			enterNameDialog.OnEnter(RequestStartMetaMatchTutorial);
			enterNameDialog.Nest(_enterNameState.Setup).Target(homeMenu);

			brokenItems.OnEnter(OpenBrokenItemsPopUp);
			brokenItems.Event(_brokenItemsCloseEvent).Target(homeCheck);
			brokenItems.Event(_brokenItemsRepairEvent).Target(equipmentMenu);
			brokenItems.OnExit(CloseBrokenItemsPopUp);

			loadoutRestricted.WaitingFor(OpenItemsAmountInvalidDialog).Target(homeCheck);

			roomJoinCreateMenu.OnEnter(OpenRoomJoinCreateMenuUI);
			roomJoinCreateMenu.Event(PlayClickedEvent).OnTransition(OpenHomeScreen).Target(waitMatchmaking);
			roomJoinCreateMenu.Event(_roomJoinCreateBackClickedEvent).Target(chooseGameMode);
			roomJoinCreateMenu.Event(_closeClickedEvent).Target(homeCheck);
			roomJoinCreateMenu.Event(NetworkState.JoinRoomFailedEvent).Target(chooseGameMode);
			roomJoinCreateMenu.Event(NetworkState.CreateRoomFailedEvent).Target(chooseGameMode);
		}

		private void HideMatchmaking()
		{
			_uiService.GetUi<HomeScreenPresenter>().ShowMatchmaking(false);
		}

		private void ShowMatchmaking()
		{
			_uiService.GetUi<HomeScreenPresenter>().ShowMatchmaking(true);
		}

		private void SubscribeEvents()
		{
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private async Task PreloadQuantumSettings()
		{
			var assets = UnityDB.CollectAddressableAssets();
			foreach (var asset in assets)
			{
				if (!asset.Item1.StartsWith("Settings"))
				{
					continue;
				}
				_ = _assetAdderService.LoadAssetAsync<AssetBase>(asset.Item1);
			}
		}

		private bool HasDefaultName()
		{
			return _gameDataProvider.AppDataProvider.DisplayNameTrimmed ==
				GameConstants.PlayerName.DEFAULT_PLAYER_NAME ||
				string.IsNullOrEmpty(_gameDataProvider.AppDataProvider.DisplayNameTrimmed);
		}

		private bool MetaTutorialConditionsCheck()
		{
			// If meta/match tutorial not completed, and tutorial not running
			return FeatureFlags.TUTORIAL && 
				!_services.TutorialService.HasCompletedTutorialSection(TutorialSection.META_GUIDE_AND_MATCH) &&
				!_services.TutorialService.IsTutorialRunning;
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_statechartTrigger(_gameCompletedCheatEvent);
		}

		private void TryClaimUncollectedRewards()
		{
			_unclaimedCountCheck = 0;

			_services.GameBackendService.CheckIfRewardsMatch(OnCheckIfServerRewardsMatch, null);
		}

		private async void OnCheckIfServerRewardsMatch(bool serverRewardsMatch)
		{
			if (serverRewardsMatch)
			{
				if (_unclaimedCountCheck > 0)
				{
					_services.GenericDialogService.CloseDialog();
				}

				if (_gameDataProvider.RewardDataProvider.UnclaimedRewards.Count > 0)
				{
					_services.CommandService.ExecuteCommand(new CollectUnclaimedRewardsCommand());
				}

				return;
			}

			// We try 10 times to check reward claiming to timeout and show error pop up
			if (_unclaimedCountCheck == 10)
			{
#if UNITY_EDITOR
				var confirmButton = new GenericDialogButton
				{
					ButtonText = "OK",
					ButtonOnClick = () => _services.QuitGame("Desync")
				};
				_services.GenericDialogService.OpenButtonDialog("Server Error", "Desync", false, confirmButton);
#else
				FirstLight.NativeUi.NativeUiService.ShowAlertPopUp(false, "Error", "Desync",
					new FirstLight.NativeUi.AlertButton
					{
						Callback = () => _services.QuitGame("Server desynch"),
						Style = FirstLight.NativeUi.AlertButtonStyle.Negative,
						Text = "Quit Game"
					});
#endif
				return;
			}

			if (_unclaimedCountCheck == 0)
			{
				_services.GenericDialogService.OpenButtonDialog(
					ScriptLocalization.UITHomeScreen.waitforrewards_popup_title,
					ScriptLocalization.UITHomeScreen.waitforrewards_popup_description,
					false, new GenericDialogButton());
			}

			_unclaimedCountCheck++;
			await Task.Delay(TimeSpan.FromMilliseconds(500)); // space check calls a bit
			_services?.GameBackendService?.CheckIfRewardsMatch(OnCheckIfServerRewardsMatch, null);
		}

		private void SendPlayReadyMessage()
		{
			_services.MessageBrokerService.Publish(new PlayMatchmakingReadyMessage());
		}

		private void SendCancelMatchmakingMessage()
		{
			_services.MessageBrokerService.Publish(new MatchmakingCancelMessage());
		}


		private bool LoadoutCountCheckToPlay()
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD

			if (FeatureFlags.GetLocalConfiguration().IgnoreEquipmentRequirementForRanked)
			{
				return false;
			}
#endif
			return _services.GameModeService.SelectedGameMode.Value.Entry.MatchType != MatchType.Casual
				&& !_gameDataProvider.EquipmentDataProvider.EnoughLoadoutEquippedToPlay();
		}

		private bool CheckItemsBroken()
		{
			var infos = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.Unbroken);

			return infos.Count != _gameDataProvider.EquipmentDataProvider.Loadout.Count;
		}

		private bool CheckIsNotPartyLeader()
		{
			if (!_services.PartyService.HasParty.Value) return false;

			return !(_services.PartyService.GetLocalMember().Leader && _services.PartyService.PartyReady.Value);
		}

		private bool CheckPartyNotReady()
		{
			return _services.PartyService.HasParty.Value && _services.PartyService.GetLocalMember().Leader &&
				!_services.PartyService.PartyReady.Value;
		}

		private async void TogglePartyReadyStatus()
		{
			var local = _services.PartyService.GetLocalMember();
			await _services.PartyService.Ready(!local?.Ready ?? false);
		}

		private async void OpenBrokenItemsPopUp()
		{
			var infos = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.All);
			var loadout = new Dictionary<GameIdGroup, UniqueId>();
			var rusted = new Dictionary<GameIdGroup, UniqueId>();

			foreach (var info in infos)
			{
				if (!info.IsBroken)
				{
					loadout.Add(info.Equipment.GameId.GetSlot(), info.Id);
				}
				else
				{
					rusted.Add(info.Equipment.GameId.GetSlot(), info.Id);
				}
			}

			if (rusted.Count == 0) return;

			_services.CommandService.ExecuteCommand(new UpdateLoadoutCommand {SlotsToUpdate = loadout});

			var data = new EquipmentPopupPresenter.StateData
			{
				OnCloseClicked = () => _statechartTrigger(_brokenItemsCloseEvent),
				OnActionConfirmed = (_, _) => _statechartTrigger(_brokenItemsRepairEvent),
				EquipmentIds = rusted.Values.Where(val => val.IsValid).ToArray(),
				PopupMode = EquipmentPopupPresenter.Mode.Rusted
			};

			await _uiService.OpenUiAsync<EquipmentPopupPresenter, EquipmentPopupPresenter.StateData>(data);
		}

		private void CloseBrokenItemsPopUp()
		{
			_uiService.CloseUi<EquipmentPopupPresenter>();
		}

		private void OpenItemsAmountInvalidDialog(IWaitActivity activity)
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
				GameModeChosen = _ =>
				{
					_services.MessageBrokerService.Publish(new SelectedGameModeMessage());
					_statechartTrigger(_gameModeSelectedFinishedEvent);
				},
				CustomGameChosen = () => _statechartTrigger(_roomJoinCreateClickedEvent),
				OnBackClicked = () => _statechartTrigger(_gameModeSelectedFinishedEvent),
				OnHomeClicked = () => _statechartTrigger(_gameModeSelectedFinishedEvent)
			};

			_uiService.OpenScreen<GameModeSelectionPresenter, GameModeSelectionPresenter.StateData>(data);
		}

		private void OpenLeaderboardUI(IWaitActivity activity)
		{
			var data = new GlobalLeaderboardScreenPresenter.StateData
			{
				OnBackClicked = () => { activity.Complete(); }
			};

			_uiService.OpenScreen<GlobalLeaderboardScreenPresenter, GlobalLeaderboardScreenPresenter.StateData>(data);
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
				OnBackClicked = () => { activity.Complete(); },
				OnHomeClicked = () => { activity.Complete(); },
				OnPurchaseItem = PurchaseItem,
				UiService = _uiService,
				IapProcessingFinished = OnIapProcessingFinished
			};

			_uiService.OpenScreen<StoreScreenPresenter, StoreScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new ShopScreenOpenedMessage());
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

		private void OpenRoomJoinCreateMenuUI()
		{
			var data = new RoomJoinCreateScreenPresenter.StateData
			{
				CloseClicked = () => _statechartTrigger(_closeClickedEvent),
				BackClicked = () => _statechartTrigger(_roomJoinCreateBackClickedEvent),
				PlayClicked = PlayButtonClicked
			};

			_uiService.OpenScreen<RoomJoinCreateScreenPresenter, RoomJoinCreateScreenPresenter.StateData>(data);
		}

		private void OpenHomeScreen()
		{
			var data = new HomeScreenPresenter.StateData
			{
				OnPlayButtonClicked = PlayButtonClicked,
				OnSettingsButtonClicked = () => _statechartTrigger(_settingsMenuClickedEvent),
				OnLootButtonClicked = () => _statechartTrigger(_equipmentClickedEvent),
				OnCollectionsClicked = () => _statechartTrigger(_collectionClickedEvent),
				OnProfileClicked = () => _statechartTrigger(_nameChangeClickedEvent),
				OnGameModeClicked = () => _statechartTrigger(_chooseGameModeClickedEvent),
				OnLeaderboardClicked = () => _statechartTrigger(_leaderboardClickedEvent),
				OnBattlePassClicked = () => _statechartTrigger(BattlePassClickedEvent),
				OnStoreClicked = () => _statechartTrigger(_storeClickedEvent),
				OnDiscordClicked = DiscordButtonClicked,
				OnMatchmakingCancelClicked = SendCancelMatchmakingMessage,
				OnLevelUp = OpenLevelUpScreen
			};

			_uiService.OpenScreen<HomeScreenPresenter, HomeScreenPresenter.StateData>(data);
			_services.MessageBrokerService.Publish(new PlayScreenOpenedMessage());
		}

		private void OpenLevelUpScreen()
		{
			var config = _services.ConfigsProvider.GetConfig<PlayerLevelConfig>((int) _gameDataProvider.PlayerDataProvider.Level.Value);
			var rewards = new List<IReward>();

			foreach (var (id, amount) in config.Rewards)
			{
				rewards.Add(new CurrencyReward(id, (uint) amount));
			}

			foreach (var unlockSystem in config.Systems)
			{
				rewards.Add(new UnlockReward(unlockSystem));
			}

			_uiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
				FameRewards = true,
				Rewards = rewards,
				OnFinish = OpenHomeScreen
			});
		}

		private void OpenDisconnectedScreen()
		{
			var data = new DisconnectedScreenPresenter.StateData
			{
				ReconnectClicked = () => _services.MessageBrokerService.Publish(new AttemptManualReconnectionMessage())
			};

			_uiService.OpenScreen<DisconnectedScreenPresenter, DisconnectedScreenPresenter.StateData>(data);
		}

		private void LoadingComplete()
		{
			CloseTransitions();
			
			// Giving new skins to old players
			if(!_gameDataProvider.CollectionDataProvider.IsItemOwned(new (GameId.MaleAssassin)))
			{
				_services.CommandService.ExecuteCommand(new GetNewSkinsCommand());
			}
		}

		private void CloseTransitions()
		{
			_ = SwipeScreenPresenter.Finish();

			if (_uiService.HasUiPresenter<LoadingScreenPresenter>())
			{
				_uiService.CloseUi<LoadingScreenPresenter>(true);
			}
		}

		private void OpenUiVfxPresenter()
		{
			_uiService.OpenUi<UiVfxPresenter>();
		}

		private void PlayButtonClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			_statechartTrigger(PlayClickedEvent);
		}

		private void RequestStartMetaMatchTutorial()
		{
			if (FeatureFlags.TUTORIAL)
			{
				_services.MessageBrokerService.Publish(new RequestStartMetaMatchTutorialMessage());
			}
		}

		private async void LoadMainMenu()
		{
			var uiVfxService = new UiVfxService(_services.AssetResolverService);
			var mainMenuServices = new MainMenuServices(uiVfxService, _services.RemoteTextureService);
			var configProvider = _services.ConfigsProvider;

			MainInstaller.Bind<IMainMenuServices>(mainMenuServices);

			_assetAdderService.AddConfigs(configProvider.GetConfig<MainMenuAssetConfigs>());
			
			await _services.AudioFxService.LoadAudioClips(configProvider.GetConfig<AudioMainMenuAssetConfigs>()
				.ConfigsDictionary);
			await _services.AssetResolverService.LoadScene(SceneId.MainMenu, LoadSceneMode.Additive);

			await _uiService.LoadGameUiSet(UiSetId.MainMenuUi, 0.9f);

			uiVfxService.Init(_uiService);

			_statechartTrigger(MainMenuLoadedEvent);

			_ = PreloadQuantumSettings();
		}

		private async Task UnloadMenuTask()
		{
			await SwipeScreenPresenter.StartSwipe();
			FLGCamera.Instance.PhysicsRaycaster.enabled = false;

			var configProvider = _services.ConfigsProvider;

			_uiService.UnloadUiSet((int) UiSetId.MainMenuUi);
			_services.AudioFxService.DetachAudioListener();

			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMainMenuAssetConfigs>()
				.ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MainMenuAssetConfigs>());
			
			await _services.AssetResolverService.UnloadScene(SceneId.MainMenu);
			
			Resources.UnloadUnusedAssets();
			MainInstaller.CleanDispose<IMainMenuServices>();
		}

		private void DiscordButtonClicked()
		{
			Application.OpenURL(GameConstants.Links.DISCORD_SERVER);
		}
	}
}