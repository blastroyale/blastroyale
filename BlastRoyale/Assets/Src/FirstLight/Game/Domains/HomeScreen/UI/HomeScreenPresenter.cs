using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Configs;
using FirstLight.Game.Configs.Remote;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.MonoComponent.MainMenu;
using FirstLight.Game.Presenters;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Authentication;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Utils.UCSExtensions;
using FirstLight.Game.Views;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using Unity.Services.Authentication;
using Unity.Services.Friends;
using Unity.Services.Friends.Notifications;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Domains.HomeScreen.UI
{
	/// <summary>
	/// This Presenter handles the Home Screen.
	/// </summary>
	public class HomeScreenPresenter : UIPresenterData<HomeScreenPresenter.StateData>
	{
		private const float TROPHIES_COUNT_DELAY = 0.8f;

		public class StateData
		{
			public Action OnPlayButtonClicked;
			public Action OnSettingsButtonClicked;
			public Action OnCollectionsClicked;
			public Action OnProfileClicked;
			public Action OnGameModeClicked;
			public Action OnLeaderboardClicked;
			public Action OnBattlePassClicked;
			public Action OnStoreClicked;
			public Action OnMatchmakingCancelClicked;
			public Action NewsClicked;
			public Action FriendsClicked;
		}

		private IGameDataProvider _dataProvider;
		private IGameServices _services;

		private LocalizedButton _playButton;

		private Label _playerNameLabel;
		private Label _playerTrophiesLabel;
		private PlayerAvatarElement _avatar;

		private VisualElement _collectionNotification;
		private VisualElement _storeNotification;
		private VisualElement _settingsNotification;
		private VisualElement _friendsNotification;
		private VisualElement _newsNotification;
		private VisualElement _newsNotificationShine;
		private VisualElement _onlineFriendsNotification;

		private ImageButton _starterPackBundle;
		private VisualElement _starterPackShine;
		private Label _starterPackCooldown;

		private Label _onlineFriendLabel;
		private Label _outOfSyncWarningLabel;
		private Label _betaLabel;
		private MatchmakingStatusView _matchmakingStatusView;

		//Hardcoded for while
		private const string STARTER_PACK = "com.firstlight.blastroyale.starterpack";

		[SerializeField] private HomePartyCharacterView _homePartyCharacterView = new ();

		private HashSet<GameId> _currentAnimations = new ();

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
		}

		private void OpenStats(PlayerStatisticsPopupPresenter.StateData data)
		{
			_services.UIService.OpenScreen<PlayerStatisticsPopupPresenter>(data).Forget();
		}

		protected override void QueryElements()
		{
			Root.Q<ImageButton>("ProfileButton").clicked += () =>
			{
				var data = new PlayerStatisticsPopupPresenter.StateData
				{
					PlayfabID = PlayFabSettings.staticPlayer.PlayFabId,
					OnEditNameClicked = () =>
					{
						Data.OnProfileClicked();
					},
					CanOpenLeaderboards = true
				};

				OpenStats(data);
			};

			_playerNameLabel = Root.Q<Label>("PlayerName").Required();
			_playerTrophiesLabel = Root.Q<Label>("TrophiesAmount").Required();
			_onlineFriendLabel = Root.Q<Label>("OnlineFriendsCount").Required();

			_avatar = Root.Q<PlayerAvatarElement>("Avatar").Required();

			_collectionNotification = Root.Q<VisualElement>("CollectionNotification").Required();
			_storeNotification = Root.Q<VisualElement>("StoreNotification").Required();
			_settingsNotification = Root.Q<VisualElement>("SettingsNotification").Required();
			_friendsNotification = Root.Q<VisualElement>("FriendsNotification").Required();
			_onlineFriendsNotification = Root.Q<VisualElement>("OnlineFriendsNotification").Required();
			_newsNotification = Root.Q<VisualElement>("NewsNotification").Required();
			_newsNotificationShine = Root.Q("NewsShine").Required();
			_newsNotificationShine.AddRotatingEffect(40, 10);
			_newsNotificationShine.AnimatePingOpacity(fromAmount: 0.3f, duration: 2000, repeat: true);

			Root.Q<ImageButton>("NewsButton").clicked += Data.NewsClicked;

			_playButton = Root.Q<LocalizedButton>("PlayButton");
			_playButton.clicked += OnPlayButtonClicked;

			Root.Q<VisualElement>("TopCurrenciesBar")
				.Required()
				.AttachView(this, out CurrencyTopBarView currencyTopBar);
			currencyTopBar.Configure(_playButton);

			Root.Q<VisualElement>("PartyMemberNames").Required()
				.AttachExistingView(this, _homePartyCharacterView);

			_outOfSyncWarningLabel = Root.Q<Label>("OutOfSyncWarning").Required();
			_betaLabel = Root.Q<Label>("BetaWarning").Required();

			Root.Q("BattlePassButtonHolder").AttachView(this, out BattlePassButtonView bpButtonView);
			bpButtonView.Clicked += Data.OnBattlePassClicked;
			Root.Q<ImageButton>("SettingsButton").clicked += Data.OnSettingsButtonClicked;

			var gameModeButton = Root.Q<ImageButton>("GameModeButton").Required();
			gameModeButton.LevelLock(this, Root, UnlockSystem.GameModes, Data.OnGameModeClicked);
			gameModeButton.AttachView(this, out GameModeButtonView _);

			var leaderBoardButton = Root.Q<ImageButton>("LeaderboardsButton");
			leaderBoardButton.LevelLock(this, Root, UnlockSystem.Leaderboards, Data.OnLeaderboardClicked);
			var collectionButton = Root.Q<LocalizedButton>("CollectionButton");
			collectionButton.LevelLock(this, Root, UnlockSystem.Collection, Data.OnCollectionsClicked);

			var storeButton = Root.Q<LocalizedButton>("StoreButton").Required();
			storeButton.SetDisplay(FeatureFlags.STORE_ENABLED);
			if (FeatureFlags.STORE_ENABLED)
			{
				storeButton.LevelLock(this, Root, UnlockSystem.Shop, Data.OnStoreClicked);
			}

			LoadStarterPackAndSetupButton().Forget();
			CheckStoreUpdateForNotification().Forget();

			Root.Q<VisualElement>("SocialsButtons").Required().AttachView(this, out SocialsView _);
			Root.Q<LocalizedButton>("FriendsButton").Required().LevelLock(this, Root, UnlockSystem.Friends, () => Data.FriendsClicked?.Invoke());
			Root.Q<LocalizedButton>("PartyUpButton").Required().LevelLock(this, Root, UnlockSystem.Squads, ShowPartyUpPopup);
			Root.Q("Matchmaking").AttachView(this, out _matchmakingStatusView);
			_matchmakingStatusView.CloseClicked += Data.OnMatchmakingCancelClicked;

			Root.SetupClicks(_services);
		}

		private async UniTaskVoid CheckStoreUpdateForNotification()
		{
			await UniTask.WaitUntil(() => _services.IAPService.UnityStore.Initialized);
			
			_storeNotification.SetDisplay(_services.IAPService.HasStoreItemsUpdate());
		}

		private async UniTaskVoid LoadStarterPackAndSetupButton()
		{
			_starterPackBundle = Root.Q<ImageButton>("OpenBundleButton").Required();
			_starterPackBundle.SetDisplay(false);

			_starterPackCooldown = Root.Q<Label>("BundleCooldown").Required();
			_starterPackShine = Root.Q("BundleShine").Required();

			await UniTask.WaitUntil(() => _services.IAPService.UnityStore.Initialized);

			var bundle = _services.IAPService.AvailableGameProductBundles.FirstOrDefault(b => b.Name == STARTER_PACK);
			if (bundle == null ||
				_dataProvider.PlayerDataProvider.Level.Value < 4 ||
				_dataProvider.PlayerStoreDataProvider.HasPurchasedProductsBundle(STARTER_PACK))
			{
				return;
			}

			_starterPackBundle.SetDisplay(true);
			_starterPackBundle.clicked += async () =>
			{
				var result = await _services.ProductsBundleService.OpenProductsBundleBanner(STARTER_PACK);
				if (result == IHomeScreenService.ProcessorResult.OpenOriginalScreen)
				{
					await _services.UIService.OpenScreen<HomeScreenPresenter>(Data);
				}
			};

			var bundleRemainingTime = _services.ProductsBundleService.GetBundlePurchaseTimeExpireAt(STARTER_PACK).Value;

			_starterPackShine.AnimatePingOpacity(fromAmount: 0.7f, toAmount: 1f, duration: 1000, repeat: true);
			_starterPackShine.AddRotatingEffect(40f, 10);
			_starterPackCooldown.ShowCooldown(bundleRemainingTime, false, HideProductBundleButton);
		}

		private void HideProductBundleButton()
		{
			_starterPackBundle.SetDisplay(false);
		}

		private void ShowPartyUpPopup()
		{
			PopupPresenter.OpenParty().Forget();
		}

		private void OnItemRewarded(ItemRewardedMessage msg)
		{
			if (msg.Item.Id.IsInGroup(GameIdGroup.Collection))
			{
				_collectionNotification.SetDisplay(_services.RewardService.UnseenItems(ItemMetadataType.Collection).Any());
			}
		}

		private void SetHasNewsNotification(bool hasNews)
		{
			_newsNotification.SetDisplay(hasNews);
			_newsNotificationShine.SetDisplay(hasNews);
			if (hasNews)
			{
				_newsNotification.AnimatePing();
				_newsNotificationShine.AnimatePing();
			}
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_settingsNotification.SetDisplay(_services.AuthService.SessionData.IsGuest);
			_collectionNotification.SetDisplay(_services.RewardService.UnseenItems(ItemMetadataType.Collection).Any());
			_friendsNotification.SetDisplay(FriendsService.Instance.IncomingFriendRequests.ToList().Count > 0);

			SetHasNewsNotification(false);
			_services.NewsService.HasNotSeenNews().ContinueWith(SetHasNewsNotification);
			
			
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
			_outOfSyncWarningLabel.SetDisplay(VersionUtils.IsOutOfSync());
#else
			_outOfSyncWarningLabel.SetDisplay(false);
#endif
			_betaLabel.SetDisplay(_dataProvider.RemoteConfigProvider.GetConfig<GeneralConfig>().ShowBetaLabel);

			RefreshOnlineFriends();
			UpdatePFP();
			UpdatePlayerNameColor(_services.LeaderboardService.CurrentRankedEntry.Position);

			_dataProvider.PlayerDataProvider.Trophies.InvokeObserve(OnTrophiesChanged);
			_services.MatchmakingService.IsMatchmaking.Observe(OnIsMatchmakingChanged);
			_dataProvider.PlayerDataProvider.Level.InvokeObserve(OnFameChanged);
			_services.LeaderboardService.OnRankingUpdate += OnRankingUpdateHandler;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated += OnPartyLobbyUpdate;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined += OnPartyJoined;
			_services.IAPService.PurchaseFinished += OnPurchaseFinished;
			_services.MessageBrokerService.Subscribe<ItemRewardedMessage>(OnItemRewarded);
			_services.MessageBrokerService.Subscribe<DisplayNameChangedMessage>(OnDisplayNameChanged);
			FriendsService.Instance.PresenceUpdated += OnPresenceUpdated;

			UpdatePlayButton();

			_playerNameLabel.text = _services.AuthService.GetPrettyLocalPlayerName();

			return base.OnScreenOpen(reload);
		}

		/// <summary>
		/// Handles when a deffered purchase finishes and the user is on the home screen
		/// </summary>
		/// <param name="data"></param>
		/// <param name="succeeded"></param>
		/// <param name="failurereason"></param>
		private void OnPurchaseFinished(string itemId, ItemData data, bool succeeded, IUnityStoreService.PurchaseFailureData failurereason)
		{
			if (!succeeded)
			{
				return;
			}

			if (IAPHelpers.IsUIBeingHandled(itemId)) return;

			ShowPurchaseRewardScreen(data).Forget();
		}

		private async UniTaskVoid ShowPurchaseRewardScreen(ItemData data)
		{
			// Do not show if user is matchmaking
			if (_services.MatchmakingService.IsMatchmaking.Value) return;
			var opened = await _services.RewardService.ClaimRewardsAndWaitForRewardsScreenToClose(data);
			if (!opened) return;
			await _services.UIService.OpenScreen<HomeScreenPresenter>(Data);
		}

		private void OnPartyJoined(Lobby l)
		{
			UpdatePlayButton();
		}

		private void RefreshOnlineFriends()
		{
			var onlineFriendsCount = FriendsService.Instance.Friends.Count(f => f.IsOnline());
			var hasPlayerOnline = onlineFriendsCount > 0;

			_onlineFriendsNotification.SetDisplay(onlineFriendsCount > 0);
			_onlineFriendLabel.text = onlineFriendsCount.ToString();
		}

		private void OnPartyLobbyUpdate(ILobbyChanges m)
		{
			UpdatePlayButton();
		}

		private void OnPresenceUpdated(IPresenceUpdatedEvent e)
		{
			RefreshOnlineFriends();
		}

		protected override UniTask OnScreenClose()
		{
			_dataProvider.PlayerDataProvider.Trophies.StopObserving(OnTrophiesChanged);
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.BLST);
			_services.MessageBrokerService.UnsubscribeAll(this);
			_services.MatchmakingService.IsMatchmaking.StopObserving(OnIsMatchmakingChanged);
			_services.LeaderboardService.OnRankingUpdate -= OnRankingUpdateHandler;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyUpdated -= OnPartyLobbyUpdate;
			_services.FLLobbyService.CurrentPartyCallbacks.LocalLobbyJoined -= OnPartyJoined;
			_services.IAPService.PurchaseFinished -= OnPurchaseFinished;
			_dataProvider.PlayerDataProvider.Level.StopObserving(OnFameChanged);
			FriendsService.Instance.PresenceUpdated -= OnPresenceUpdated;

			return base.OnScreenClose();
		}

		private void OnDisplayNameChanged(DisplayNameChangedMessage _)
		{
			_playerNameLabel.text = _services.AuthService.GetPrettyLocalPlayerName();
		}

		private void OnRankingUpdateHandler(PlayerLeaderboardEntry leaderboardEntry)
		{
			UpdatePlayerNameColor(leaderboardEntry.Position);
		}

		private void UpdatePlayerNameColor(int leaderboardRank)
		{
			_playerNameLabel.style.color = Color.white;
		}

		private void UpdatePFP()
		{
			_avatar.SetLocalPlayerData(_dataProvider, _services);
		}

		private void OnPlayButtonClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;
			Data.OnPlayButtonClicked();
		}

		private void OnIsMatchmakingChanged(bool previous, bool current)
		{
			UpdatePlayButton();
		}

		private void OnTrophiesChanged(uint previous, uint current)
		{
			if (current > previous && !_currentAnimations.Contains(GameId.Trophies))
			{
				StartCoroutine(AnimateCurrency(GameId.Trophies, previous, current, _playerTrophiesLabel));
			}
			else
			{
				_playerTrophiesLabel.text = current.ToString();
			}
		}

		private void OnFameChanged(uint previous, uint current)
		{
			_avatar.SetLevel(current);

			// TODO: Animate VFX when we have a progress bar: StartCoroutine(AnimateCurrency(GameId.Trophies, previous, current, _avatar));
		}

		private IEnumerator AnimateCurrency(GameId id, ulong previous, ulong current, Label label)
		{
			_currentAnimations.Add(id);
			yield return new WaitForSeconds(0.4f);

			label.text = previous.ToString();

			for (int i = 0; i < Mathf.Clamp((current - previous) / 5, 3, 10); i++)
			{
				_services.UIVFXService.PlayVfx(id,
					i * 0.05f,
					Root.GetPositionOnScreen(Root) + Random.insideUnitCircle * 100,
					label.GetPositionOnScreen(Root),
					() =>
					{
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);
					});
			}

			yield return new WaitForSeconds(TROPHIES_COUNT_DELAY);

			DOVirtual.Float(previous, current, 0.5f, val => { label.text = val.ToString("F0"); });
			_currentAnimations.Remove(id);
		}

		private void UpdatePlayButton(bool forceLoading = false)
		{
			var translationKey = ScriptTerms.UITHomeScreen.play;
			var buttonClass = string.Empty;
			var buttonEnabled = true;

			FLog.Verbose("Updating play button state");
			var partyLobby = _services.FLLobbyService.CurrentPartyLobby;

			// TODO mihak: Add operation in progress logic for parties
			if (forceLoading || _services.MatchmakingService.IsMatchmaking.Value)
			{
				buttonClass = "play-button--loading";
				buttonEnabled = false;
			}
			else if (partyLobby != null)
			{
				if (partyLobby.IsLocalPlayerHost())
				{
					if (!partyLobby.IsEveryoneReady())
					{
						translationKey = ScriptTerms.UITHomeScreen.waiting_for_members;
						buttonEnabled = false;
					}
					else
					{
						translationKey = ScriptTerms.UITHomeScreen.play;
					}
				}
				else
				{
					var isReady = partyLobby.Players.First(p => p.IsLocal()).IsReady();

					if (isReady)
					{
						buttonClass = "play-button--get-ready";
						translationKey = ScriptTerms.UITHomeScreen.youre_ready;
					}
					else
					{
						translationKey = ScriptTerms.UITHomeScreen.ready;
					}
				}
			}

			_playButton.SetEnabled(buttonEnabled);
			_playButton.RemoveModifiers();
			if (!string.IsNullOrEmpty(buttonClass)) _playButton.AddToClassList(buttonClass);
			_playButton.LocalizationKey = translationKey;
		}

		public void ShowMatchmaking(bool show)
		{
			_matchmakingStatusView.Show(show);

			// When this screen is opened we aren't officially matchmaking yet, so we force the loading state for the 
			// first few seconds - should be changed when we allow interaction on home screen during matchmaking.
			UpdatePlayButton(show);
		}
	}
}