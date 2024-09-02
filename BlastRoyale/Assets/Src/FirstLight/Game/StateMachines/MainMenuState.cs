using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs.AssetConfigs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters;
using FirstLight.Game.Presenters.News;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Statechart;
using I2.Loc;
using Quantum;
using Unity.Services.Lobbies.Models;
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

		private readonly IStatechartEvent _customGameJoined = new StatechartEvent("Custom Game Joined");
		private readonly IStatechartEvent _backButtonClicked = new StatechartEvent("Back Button Clicked");
		private readonly IStatechartEvent _customGameButtonClicked = new StatechartEvent("Room Join Create Button Clicked Event");
		private readonly IStatechartEvent _nameChangeClickedEvent = new StatechartEvent("Name Change Clicked Event");
		private readonly IStatechartEvent _chooseGameModeClickedEvent = new StatechartEvent("Game Mode Clicked Event");
		private readonly IStatechartEvent _equipmentClickedEvent = new StatechartEvent("Equipment Clicked Event");
		private readonly IStatechartEvent _newsClickedEvent = new StatechartEvent("News Clicked");
		private readonly IStatechartEvent _collectionClickedEvent = new StatechartEvent("Collection Clicked Event");
		private readonly IStatechartEvent _gameModeSelectedFinishedEvent = new StatechartEvent("Game Mode Selected Finished Event");
		private readonly IStatechartEvent _leaderboardClickedEvent = new StatechartEvent("Leaderboard Clicked Event");
		private readonly IStatechartEvent _storeClickedEvent = new StatechartEvent("Store Clicked Event");
		private readonly IStatechartEvent _roomJoinCreateBackClickedEvent = new StatechartEvent("Room Join Create Back Button Clicked Event");
		private readonly IStatechartEvent _friendsClickedEvent = new StatechartEvent("Friends Button Clicked Event");

		private readonly IStatechartEvent _gameCompletedCheatEvent = new StatechartEvent("Game Completed Cheat Event");
		private readonly IStatechartEvent _brokenItemsCloseEvent = new StatechartEvent("Broken Items Close Event");
		private readonly IStatechartEvent _brokenItemsRepairEvent = new StatechartEvent("Broken Items Repair Event");

		private readonly IGameServices _services;
		private readonly IGameDataProvider _gameDataProvider;
		private readonly IAssetAdderService _assetAdderService;
		private readonly Action<IStatechartEvent> _statechartTrigger;
		private readonly SettingsMenuState _settingsMenuState;
		private readonly EnterNameState _enterNameState;
		private readonly CollectionMenuState _collectionMenuState;

		private int _unclaimedCountCheck;

		public MainMenuState(IGameServices services, IGameDataProvider gameLogic, IAssetAdderService assetAdderService,
							 Action<IStatechartEvent> statechartTrigger)
		{
			_services = services;
			_gameDataProvider = gameLogic;
			_assetAdderService = assetAdderService;
			_statechartTrigger = statechartTrigger;
			_collectionMenuState = new CollectionMenuState(services, gameLogic, statechartTrigger);
			_enterNameState = new EnterNameState(services, gameLogic, statechartTrigger);
			_settingsMenuState = new SettingsMenuState(gameLogic, services, statechartTrigger);
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
			var mainMenu = stateFactory.Nest("Main Menu Screen");
			var mainMenuTransition = stateFactory.Transition("Main Transition");
			var disconnected = stateFactory.State("Disconnected");
			var disconnectedCheck = stateFactory.Choice("Disconnected Final Choice");

			initial.Transition().Target(mainMenuLoading);
			initial.OnExit(SubscribeEvents);

			mainMenuLoading.OnEnter(BeforeLoadingMenu);
			mainMenuLoading.WaitingFor(LoadMainMenu).Target(mainMenu);
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
			_services.MessageBrokerService.Publish(new MainMenuLoadedMessage());
		}

		private void MainMenuSetup(IStateFactory stateFactory)
		{
			var initial = stateFactory.Initial("Initial");
			var final = stateFactory.Final("Final");
			var homeCheck = stateFactory.Choice("Main Screen Check");
			var homeMenu = stateFactory.State("Home Menu");
			var news = stateFactory.State("News");
			var collectionMenu = stateFactory.Nest("Collection Menu");
			var settingsMenu = stateFactory.Nest("Settings Menu");
			var matchmakingChecks = stateFactory.Choice("Play Button Clicked Check");
			var waitMatchmaking = stateFactory.State("Matchmaking Waiting");
			var chooseGameMode = stateFactory.State("Enter Choose Game Mode");
			var leaderboard = stateFactory.Wait("Leaderboard");
			var battlePass = stateFactory.Wait("BattlePass");
			var store = stateFactory.Wait("Store");
			var friends = stateFactory.Wait("Friends");
			var enterNameDialog = stateFactory.Nest("Enter Name Dialog");
			var customGamesList = stateFactory.State("Custom Games List");
			var customGameLobby = stateFactory.State("Custom Game Lobby");

			void AddMatchmakingHooks(params IStateEvent[] states)
			{
				foreach (var state in states)
				{
					state.Event(NetworkState.JoinedPlayfabMatchmaking)
						.OnTransition(() => OpenHomeScreen().Forget())
						.Target(waitMatchmaking);

					state.Event(NetworkState.JoinedRoomEvent)
						.Target(final);

					state.Event(_customGameJoined)
						.Target(customGameLobby);
				}
			}

			initial.Transition().Target(homeCheck);
			initial.OnExit(() => FLGCamera.Instance.PhysicsRaycaster.enabled = true);

			news.OnEnter(OnEnterNews);
			news.Event(_backButtonClicked).Target(homeCheck);

			homeCheck.Transition().Condition(MetaTutorialConditionsCheck).Target(enterNameDialog);
			homeCheck.Transition().Condition(RequiresToSeeStore).Target(store);
			homeCheck.Transition().Condition(IsInRoom)
				.OnTransition(() => _services.RoomService.LeaveRoom())
				.Target(homeMenu);
			homeCheck.Transition().Target(homeMenu);

			homeMenu.OnEnter(() => OpenHomeScreen().Forget());
			homeMenu.OnEnter(RequestStartMetaMatchTutorial);
			homeMenu.Event(PlayClickedEvent).Target(matchmakingChecks);
			homeMenu.Event(_settingsMenuClickedEvent).Target(settingsMenu);
			homeMenu.Event(_gameCompletedCheatEvent).Target(homeCheck);
			homeMenu.Event(_nameChangeClickedEvent).Target(enterNameDialog);
			homeMenu.Event(_chooseGameModeClickedEvent).Target(chooseGameMode);
			homeMenu.Event(_leaderboardClickedEvent).Target(leaderboard);
			homeMenu.Event(BattlePassClickedEvent).Target(battlePass);
			homeMenu.Event(_newsClickedEvent).Target(news);
			homeMenu.Event(_storeClickedEvent).Target(store);
			homeMenu.Event(_collectionClickedEvent).Target(collectionMenu);
			homeMenu.Event(_friendsClickedEvent).Target(friends);

			settingsMenu.Nest(_settingsMenuState.Setup).Target(homeCheck);
			collectionMenu.Nest(_collectionMenuState.Setup).Target(homeCheck);
			battlePass.WaitingFor(OpenBattlePassUI).Target(homeCheck);
			leaderboard.WaitingFor(OpenLeaderboardUI).Target(homeCheck);
			store.WaitingFor(OpenStore).Target(homeCheck);
			friends.WaitingFor(wait => OpenFriends(wait).Forget()).Target(homeCheck);
			AddMatchmakingHooks(
				settingsMenu,
				customGamesList,
				homeMenu,
				chooseGameMode,
				collectionMenu,
				battlePass,
				leaderboard,
				store,
				news,
				friends);

			matchmakingChecks.Transition().Condition(CheckPartyNotReady).Target(homeCheck);
			matchmakingChecks.Transition().Condition(CheckInvalidTeamSize)
				.OnTransition(() => _services.GenericDialogService.OpenSimpleMessage("Error", "Invalid party size!")).Target(homeCheck);
			matchmakingChecks.Transition().Condition(IsInRoom).Target(homeCheck);
			matchmakingChecks.Transition().Condition(CheckIsNotPartyLeader).OnTransition(() => TogglePartyReadyStatus().Forget())
				.Target(homeCheck);
			matchmakingChecks.Transition().OnTransition(SendPlayReadyMessage)
				.Target(waitMatchmaking);

			// Matchmaking
			waitMatchmaking.OnEnter(ShowMatchmaking);
			waitMatchmaking.Event(NetworkState.JoinedRoomEvent).Target(final);
			waitMatchmaking.Event(NetworkState.PhotonDisconnectedEvent).OnTransition(HideMatchmaking).Target(homeCheck);
			waitMatchmaking.Event(NetworkState.JoinRoomFailedEvent).OnTransition(HideMatchmaking).Target(homeCheck);
			waitMatchmaking.Event(NetworkState.CreateRoomFailedEvent).OnTransition(HideMatchmaking).Target(homeCheck);
			waitMatchmaking.Event(NetworkState.CanceledMatchmakingEvent)
				.OnTransition(HideMatchmaking)
				.Target(homeCheck);

			chooseGameMode.OnEnter(OpenGameModeSelectionUI);
			chooseGameMode.Event(_gameModeSelectedFinishedEvent).Target(homeCheck);
			chooseGameMode.Event(_customGameButtonClicked).Target(customGamesList);

			enterNameDialog.OnEnter(RequestStartMetaMatchTutorial);
			enterNameDialog.Nest(_enterNameState.Setup).Target(homeMenu);

			customGameLobby.OnEnter(OpenCustomGameLobby);
			customGameLobby.Event(_roomJoinCreateBackClickedEvent).Target(customGamesList);
			customGameLobby.Event(NetworkState.JoinedRoomEvent).Target(final);
			customGameLobby.Event(NetworkState.JoinRoomFailedEvent).Target(chooseGameMode);
			customGameLobby.Event(NetworkState.CreateRoomFailedEvent).Target(chooseGameMode);
			customGamesList.OnExit(() => _services.UIService.CloseScreen<MatchLobbyScreenPresenter>(false).Forget());

			customGamesList.OnEnter(OpenCustomGameList);
			customGamesList.Event(_roomJoinCreateBackClickedEvent).Target(chooseGameMode);
			customGamesList.Event(NetworkState.JoinRoomFailedEvent).Target(chooseGameMode);
			customGamesList.Event(NetworkState.CreateRoomFailedEvent).Target(chooseGameMode);
			customGamesList.OnExit(() => _services.UIService.CloseScreen<MatchListScreenPresenter>(false).Forget());
		}

		private async UniTaskVoid OpenFriends(IWaitActivity wait)
		{
			var data = new FriendsScreenPresenter.StateData
			{
				OnBackClicked = () => wait.Complete()
			};

			await _services.UIService.OpenScreen<FriendsScreenPresenter>(data);
		}

		private void OnEnterNews()
		{
			_services.UIService.OpenScreen<NewsScreenPresenter>(new NewsScreenPresenter.NewsScreenData()
			{
				OnBack = OnBackClicked
			}).Forget();
		}

		private bool RequiresToSeeStore()
		{
			return _services.IAPService.RequiredToViewStore;
		}

		private void HideMatchmaking()
		{
			// TODO mihak: Move matchmaking into it's own screen
			_services.UIService.GetScreen<HomeScreenPresenter>().ShowMatchmaking(false);
		}

		private void OnBackClicked()
		{
			_statechartTrigger(_backButtonClicked);
		}

		private void ShowMatchmaking()
		{
			// TODO mihak: Move matchmaking into it's own screen
			_services.UIService.GetScreen<HomeScreenPresenter>().ShowMatchmaking(true);
		}

		private void SubscribeEvents()
		{
			_services.FLLobbyService.CurrentMatchCallbacks.LocalLobbyJoined += OnJoinedCustomGame;
			_services.MessageBrokerService.Subscribe<ItemConvertedToBlastBuckMessage>(OnItemConvertedToBlastBucks);
			_services.MessageBrokerService.Subscribe<GameCompletedRewardsMessage>(OnGameCompletedRewardsMessage);
			_services.MessageBrokerService.Subscribe<NewBattlePassSeasonMessage>(OnBattlePassNewSeason);
			_services.MessageBrokerService.Subscribe<MainMenuShouldReloadMessage>(MainMenuShouldReloadMessage);
		}

		private void OnJoinedCustomGame(Lobby l)
		{
			_statechartTrigger(_customGameJoined);
		}

		private void MainMenuShouldReloadMessage(MainMenuShouldReloadMessage msg)
		{
			// TODO mihak wtf is this method
			OpenHomeScreen().Forget();
		}

		private void OnItemConvertedToBlastBucks(ItemConvertedToBlastBuckMessage m)
		{
			if (_services.RoomService.InRoom) return;

			var itemView = ItemFactory.Currency(GameId.BlastBuck, m.BlastBucks).GetViewModel();
			_services.GenericDialogService.OpenSimpleMessage(
				ScriptLocalization.UITGeneric.items_converted,
				string.Format(ScriptLocalization.UITGeneric.items_converted_msg, $"{m.BlastBucks} {itemView}", m.Items.Count)
			);
		}

		private void UnsubscribeEvents()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
			_services.FLLobbyService.CurrentMatchCallbacks.LocalLobbyJoined -= OnJoinedCustomGame;
		}

		private async UniTask PreloadQuantumSettings()
		{
			var assets = UnityDB.CollectAddressableAssets();
			foreach (var asset in assets)
			{
				if (!asset.Item1.StartsWith("Settings"))
				{
					continue;
				}

				await _assetAdderService.LoadAssetAsync<AssetBase>(asset.Item1);
			}
		}

		private bool MetaTutorialConditionsCheck()
		{
			// If first guide match tutorial not completed, and tutorial not running
			var b = !_services.TutorialService.HasCompletedTutorialSection(TutorialSection.ENTER_NAME_PROMPT);
			
			Debug.Log($"MetaTutorialConditionsCheck HasCompletedTutorialSection-ENTER_NAME_PROMPT {!b} HasCompletedTutorial {_services.TutorialService.HasCompletedTutorial()}");
			return b &&
				!_services.TutorialService.HasCompletedTutorial();
		}

		private void OnGameCompletedRewardsMessage(GameCompletedRewardsMessage message)
		{
			_statechartTrigger(_gameCompletedCheatEvent);
		}

		private void TryClaimUncollectedRewards()
		{
			_unclaimedCountCheck = 0;
			if (FeatureFlags.GetLocalConfiguration().OfflineMode || !FeatureFlags.WAIT_REWARD_SYNC)
			{
				OnCheckIfServerRewardsMatch(true).Forget();
				return;
			}

			_services.GameBackendService.CheckIfRewardsMatch(b => OnCheckIfServerRewardsMatch(b).Forget(), null);
		}

		private async UniTaskVoid OnCheckIfServerRewardsMatch(bool serverRewardsMatch)
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
				_services.GenericDialogService.OpenButtonDialog("Server Error", "Desync", false, confirmButton).Forget();
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
					false, new GenericDialogButton()).Forget();
			}

			_unclaimedCountCheck++;
			await Task.Delay(TimeSpan.FromMilliseconds(500)); // space check calls a bit
			_services?.GameBackendService?.CheckIfRewardsMatch(b => OnCheckIfServerRewardsMatch(b).Forget(), null);
		}

		private void SendPlayReadyMessage()
		{
			_services.MessageBrokerService.Publish(new LocalPlayerClickedPlayMessage());
		}

		private void SendCancelMatchmakingMessage()
		{
			_services.MessageBrokerService.Publish(new MatchmakingCancelMessage());
		}

		private bool CheckIsNotPartyLeader()
		{
			if (_services.FLLobbyService.CurrentPartyLobby == null) return false;

			return !(_services.FLLobbyService.CurrentPartyLobby.IsLocalPlayerHost() && _services.FLLobbyService.CurrentPartyLobby.IsEveryoneReady());
		}

		private bool CheckPartyNotReady()
		{
			return _services.FLLobbyService.CurrentPartyLobby != null && _services.FLLobbyService.CurrentPartyLobby.IsLocalPlayerHost() &&
				!_services.FLLobbyService.CurrentPartyLobby.IsEveryoneReady();
		}

		private bool CheckInvalidTeamSize()
		{
			return (_services.FLLobbyService.CurrentPartyLobby?.Players?.Count ?? 1) > _services.GameModeService.SelectedGameMode.Value.Entry.TeamSize;
		}

		private async UniTaskVoid TogglePartyReadyStatus()
		{
			await _services.FLLobbyService.TogglePartyReady();
		}

		private void OpenGameModeSelectionUI()
		{
			var data = new GameModeScreenPresenter.StateData
			{
				GameModeChosen = _ => _statechartTrigger(_gameModeSelectedFinishedEvent),
				CustomGameChosen = () => _statechartTrigger(_customGameButtonClicked),
				OnBackClicked = () => _statechartTrigger(_gameModeSelectedFinishedEvent),
			};

			_services.UIService.OpenScreen<GameModeScreenPresenter>(data).Forget();
		}

		private void OpenLeaderboardUI(IWaitActivity activity)
		{
			var data = new GlobalLeaderboardScreenPresenter.StateData
			{
				OnBackClicked = () => { activity.Complete(); }
			};

			_services.UIService.OpenScreen<GlobalLeaderboardScreenPresenter>(data).Forget();
		}

		private void OnBattlePassNewSeason(NewBattlePassSeasonMessage msg)
		{
			_statechartTrigger(BattlePassClickedEvent);
		}

		private void OpenBattlePassUI(IWaitActivity activity)
		{
			var cacheActivity = activity;

			var data = new BattlePassScreenPresenter.StateData
			{
				BackClicked = () =>
				{
					cacheActivity.Complete();
				},
				DisableScrollAnimation = true
			};

			_services.UIService.OpenScreen<BattlePassScreenPresenter>(data).Forget();
		}

		private void OpenStore(IWaitActivity activity)
		{
			OpenStoreAsync(activity).Forget();
		}

		private async UniTaskVoid OpenStoreAsync(IWaitActivity activity)
		{
			var data = new StoreScreenPresenter.StateData
			{
				OnBackClicked = () => { activity.Complete(); },
				OnHomeClicked = () => { activity.Complete(); },
				OnPurchaseItem = PurchaseItem,
			};
			await _services.UIService.OpenScreen<StoreScreenPresenter>(data);
			_services.MessageBrokerService.Publish(new ShopScreenOpenedMessage());
		}

		private void PurchaseItem(GameProduct product)
		{
			_services.IAPService.BuyProduct(product);
		}

		private void OpenCustomGameLobby()
		{
			_services.UIService.OpenScreen<MatchLobbyScreenPresenter>(new MatchLobbyScreenPresenter.StateData
			{
				BackClicked = () => _statechartTrigger(_roomJoinCreateBackClickedEvent),
			}).Forget();
		}

		private void OpenCustomGameList()
		{
			// Leave party if player has one
			if (_services.FLLobbyService.IsInPartyLobby())
				_services.FLLobbyService.LeaveParty().Forget();
			
			_services.UIService.OpenScreen<MatchListScreenPresenter>(new MatchListScreenPresenter.StateData
			{
				BackClicked = () => _statechartTrigger(_roomJoinCreateBackClickedEvent),
			}).Forget();
		}

		private async UniTaskVoid OpenHomeScreen()
		{
			if (!_services.UIService.IsScreenOpen<HomeScreenPresenter>())
			{
				LoadingScreenPresenter.Destroy();

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
					OnMatchmakingCancelClicked = SendCancelMatchmakingMessage,
					OnLevelUp = OpenLevelUpScreen,
					OnRewardsReceived = OnRewardsReceived,
					NewsClicked = () => _statechartTrigger(_newsClickedEvent),
					FriendsClicked = () => _statechartTrigger(_friendsClickedEvent)
				};

				await _services.UIService.OpenScreen<HomeScreenPresenter>(data);
			}

			TryClaimUncollectedRewards();

			_services.MessageBrokerService.Publish(new MainMenuOpenedMessage());
		}

		private void FinishRewardSequence()
		{
			if (_services.RoomService.InRoom) return;

			OpenHomeScreen().Forget();
			_services.MessageBrokerService.Publish(new OnViewingRewardsFinished());
		}

		private void OnRewardsReceived(List<ItemData> items)
		{
			if (_services.RoomService.InRoom) return;

			FLog.Verbose("Main Menu State", "Opening reward sequence");
			var rewardsCopy = items
				.Where(item => !item.Id.IsInGroup(GameIdGroup.Currency) && item.Id is not (GameId.XP or GameId.BPP or GameId.Trophies)).ToList();
			if (rewardsCopy.Count > 0)
			{
				_services.UIService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData
				{
					Items = rewardsCopy,
					OnFinish = FinishRewardSequence
				}).Forget();
			}
		}

		private void OpenLevelUpScreen()
		{
			if (_services.RoomService.InRoom) return;

			var levelRewards = _gameDataProvider.PlayerDataProvider.GetRewardsForFameLevel(
				_gameDataProvider.PlayerDataProvider.Level.Value - 1
			);
			_services.UIService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData
			{
				FameRewards = true,
				Items = levelRewards,
				OnFinish = FinishRewardSequence
			}).Forget();
		}

		private void OpenDisconnectedScreen()
		{
			var data = new DisconnectedScreenPresenter.StateData
			{
				ReconnectClicked = () => _services.MessageBrokerService.Publish(new AttemptManualReconnectionMessage())
			};

			_services.UIService.OpenScreen<DisconnectedScreenPresenter>(data).Forget();
		}

		private void LoadingComplete()
		{
			_services.UIService.CloseScreen<SwipeTransitionScreenPresenter>(false).Forget();
		}

		private void PlayButtonClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			_statechartTrigger(PlayClickedEvent);
		}

		private void RequestStartMetaMatchTutorial()
		{
			RequestDelayed().Forget();
		}

		private async UniTask RequestDelayed()
		{
			await UniTask.NextFrame(); // TODO: Hack because we get into a loop and Unity freezes without it
			_services.MessageBrokerService.Publish(new RequestStartMetaMatchTutorialMessage());
		}

		private void BeforeLoadingMenu()
		{
			// Removing invalid skins
			var skin = _gameDataProvider.CollectionDataProvider.GetEquipped(CollectionCategories.PLAYER_SKINS);
			if (skin != null && !skin.Id.IsInGroup(GameIdGroup.PlayerSkin))
			{
				FLog.Info("Updating skin as skin was not in PlayerSkin group");
				_services.CommandService.ExecuteCommand(new EquipCollectionItemCommand()
				{
					Item = _gameDataProvider.CollectionDataProvider.DefaultCollectionItems[CollectionCategories.PLAYER_SKINS].First()
				});
			}

			// Removing non-nft equipments
			var nonNfts = _gameDataProvider.EquipmentDataProvider.GetInventoryEquipmentCount(EquipmentFilter.NoNftOnly);
			if (nonNfts > 0)
			{
				FLog.Info("Converting non-nfts to blast bucks");
				_services.CommandService.ExecuteCommand(new NonNftConvertCommand());
			}
		}

		private async UniTask LoadMainMenu()
		{
			var mainMenuServices = new MainMenuServices(_services.RemoteTextureService);
			var configProvider = _services.ConfigsProvider;

			MainInstaller.Bind<IMainMenuServices>(mainMenuServices);

			_assetAdderService.AddConfigs(configProvider.GetConfig<MainMenuAssetConfigs>());

			await _services.AudioFxService.LoadAudioClips(configProvider.GetConfig<AudioMainMenuAssetConfigs>()
				.ConfigsDictionary);

			await SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
			SceneManager.SetActiveScene(SceneManager.GetSceneByName("MainMenu"));

			_statechartTrigger(MainMenuLoadedEvent);

			await PreloadQuantumSettings();
		}

		private async UniTask UnloadMenuTask()
		{
			await _services.UIService.OpenScreen<SwipeTransitionScreenPresenter>();
			FLGCamera.Instance.PhysicsRaycaster.enabled = false;

			var configProvider = _services.ConfigsProvider;

			_services.AudioFxService.DetachAudioListener();

			_services.VfxService.DespawnAll();
			_services.AudioFxService.UnloadAudioClips(configProvider.GetConfig<AudioMainMenuAssetConfigs>()
				.ConfigsDictionary);
			_services.AssetResolverService.UnloadAssets(true, configProvider.GetConfig<MainMenuAssetConfigs>());

			await SceneManager.UnloadSceneAsync("MainMenu");

			await Resources.UnloadUnusedAssets();
			MainInstaller.CleanDispose<IMainMenuServices>();
		}

		private bool IsInRoom()
		{
			return _services.RoomService.InRoom;
		}
	}
}