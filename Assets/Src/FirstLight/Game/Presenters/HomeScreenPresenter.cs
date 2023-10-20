using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using FirstLight.Game.Configs;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using I2.Loc;
using PlayFab;
using PlayFab.ClientModels;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Home Screen.
	/// </summary>
	[LoadSynchronously]
	public partial class HomeScreenPresenter : UiToolkitPresenterData<HomeScreenPresenter.StateData>
	{
		private const float TROPHIES_COUNT_DELAY = 0.8f;

		private const string CS_POOL_AMOUNT_FORMAT = "<color=#FE6C07>{0}</color> / {1}";
		private const string BPP_POOL_AMOUNT_FORMAT = "<color=#49D4D4>{0}</color> / {1}";

		private const string USS_AVATAR_NFT = "player-header__avatar--nft";

		public struct StateData
		{
			public Action OnPlayButtonClicked;
			public Action OnSettingsButtonClicked;
			public Action OnLootButtonClicked;
			public Action OnCollectionsClicked;
			public Action OnProfileClicked;
			public Action OnGameModeClicked;
			public Action OnLeaderboardClicked;
			public Action OnBattlePassClicked;
			public Action OnStoreClicked;
			public Action OnDiscordClicked;
			public Action OnMatchmakingCancelClicked;
			public Action OnLevelUp;
			public Action<List<ItemData>> OnRewardsReceived;
		}

		private IGameDataProvider _dataProvider;
		private IGameServices _services;
		private IMainMenuServices _mainMenuServices;

		private IPartyService _partyService;

		private LocalizedButton _playButton;
		private VisualElement _playButtonContainer;

		private Label _playerNameLabel;
		private Label _playerTrophiesLabel;
		private PlayerAvatarElement _avatar;

		private VisualElement _equipmentNotification;
		private VisualElement _collectionNotification;

		private ImageButton _gameModeButton;
		private Label _gameModeLabel;

		private Label _csAmountLabel;
		private Label _blstAmountLabel;

		private ImageButton _battlePassButton;
		private Label _battlePassProgressLabel;
		private VisualElement _battlePassProgressElement;
		private VisualElement _battlePassRarity;

		private VisualElement _bppPoolContainer;
		private Label _bppPoolRestockTimeLabel;
		private Label _bppPoolRestockAmountLabel;
		private Label _bppPoolAmountLabel;
		private VisualElement _csPoolContainer;
		private Label _csPoolRestockTimeLabel;
		private Label _csPoolRestockAmountLabel;
		private Label _csPoolAmountLabel;
		private Label _outOfSyncWarningLabel;
		private Label _betaLabel;
		private MatchmakingStatusView _matchmakingStatusView;
		private Coroutine _updatePoolsCoroutine;
		private HashSet<GameId> _currentAnimations = new ();
		private HashSet<GameId> _initialized = new ();

		private void Awake()
		{
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_mainMenuServices = MainInstaller.Resolve<IMainMenuServices>();
			_partyService = _services.PartyService;
		}

		private async void OpenStats(PlayerStatisticsPopupPresenter.StateData data)
		{
			await _uiService.OpenUiAsync<PlayerStatisticsPopupPresenter, PlayerStatisticsPopupPresenter.StateData>(data);
		}

		protected override void QueryElements(VisualElement root)
		{
			root.Q<ImageButton>("ProfileButton").clicked += () => 
			{
				if (FeatureFlags.PLAYER_STATS_ENABLED)
				{
					var data = new PlayerStatisticsPopupPresenter.StateData
					{
						PlayerId = PlayFabSettings.staticPlayer.PlayFabId,
						OnCloseClicked = () =>
						{
							_uiService.CloseUi<PlayerStatisticsPopupPresenter>();
						},
						OnEditNameClicked = () =>
						{
							Data.OnProfileClicked();
						}
					};

					OpenStats(data);
				}
				else
				{
					Data.OnProfileClicked();
				}
			};
			root.Q<ImageButton>("LeaderboardsButton").clicked += Data.OnLeaderboardClicked;
			_playerNameLabel = root.Q<Label>("PlayerName").Required();
			_playerTrophiesLabel = root.Q<Label>("TrophiesAmount").Required();

			_avatar = root.Q<PlayerAvatarElement>("Avatar").Required();

			_gameModeLabel = root.Q<Label>("GameModeLabel").Required();
			_gameModeButton = root.Q<ImageButton>("GameModeButton").Required();

			_equipmentNotification = root.Q<VisualElement>("EquipmentNotification").Required();
			_collectionNotification = root.Q<VisualElement>("CollectionNotification").Required();

			_bppPoolContainer = root.Q<VisualElement>("BPPPoolContainer").Required();
			_bppPoolAmountLabel = _bppPoolContainer.Q<Label>("AmountLabel").Required();
			_bppPoolRestockTimeLabel = _bppPoolContainer.Q<Label>("RestockLabelTime").Required();
			_bppPoolRestockAmountLabel = _bppPoolContainer.Q<Label>("RestockLabelAmount").Required();

			_csPoolContainer = root.Q<VisualElement>("CSPoolContainer").Required();
			_csPoolAmountLabel = _csPoolContainer.Q<Label>("AmountLabel").Required();
			_csPoolRestockTimeLabel = _csPoolContainer.Q<Label>("RestockLabelTime").Required();
			_csPoolRestockAmountLabel = _csPoolContainer.Q<Label>("RestockLabelAmount").Required();

			_battlePassButton = root.Q<ImageButton>("BattlePassButton").Required();
			_battlePassProgressElement = _battlePassButton.Q<VisualElement>("BattlePassProgressElement").Required();
			_battlePassProgressLabel = _battlePassButton.Q<Label>("BPProgressText").Required();
			_battlePassRarity = _battlePassButton.Q<VisualElement>("BPRarity").Required();

			QueryElementsSquads(root);

			_playButtonContainer = root.Q("PlayButtonHolder");
			_playButton = root.Q<LocalizedButton>("PlayButton");
			_playButton.clicked += OnPlayButtonClicked;

			root.Q<CurrencyDisplayElement>("CSCurrency")
				.AttachView(this, out CurrencyDisplayView _)
				.SetAnimationOrigin(_playButton);
			root.Q<CurrencyDisplayElement>("CoinCurrency")
				.AttachView(this, out CurrencyDisplayView _)
				.SetAnimationOrigin(_playButton);
			root.Q<CurrencyDisplayElement>("FragmentsCurrency")
				.AttachView(this, out CurrencyDisplayView _)
				.SetAnimationOrigin(_playButton);

			// TODO: Uncomment when we use Fragments
			root.Q<CurrencyDisplayElement>("FragmentsCurrency").SetDisplay(false);

			_outOfSyncWarningLabel = root.Q<Label>("OutOfSyncWarning").Required();
			_betaLabel = root.Q<Label>("BetaWarning").Required();

			_gameModeButton.clicked += Data.OnGameModeClicked;
			root.Q<ImageButton>("SettingsButton").clicked += Data.OnSettingsButtonClicked;
			root.Q<ImageButton>("BattlePassButton").clicked += Data.OnBattlePassClicked;

			root.Q<Button>("EquipmentButton").clicked += Data.OnLootButtonClicked;
			root.Q<Button>("TrophiesHolder").clicked += Data.OnLeaderboardClicked;
			var collectionButton = root.Q<Button>("CollectionButton");
			collectionButton.LevelLock(this, Root, UnlockSystem.Collection, Data.OnCollectionsClicked);

			var storeButton = root.Q<Button>("StoreButton");
			storeButton.SetDisplay(FeatureFlags.STORE_ENABLED);
			if (FeatureFlags.STORE_ENABLED)
			{
				storeButton.LevelLock(this, Root, UnlockSystem.Shop, Data.OnStoreClicked);
			}

			var discordButton = root.Q<Button>("DiscordButton");
			discordButton.clicked += () =>
			{
				_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.DiscordLink);
				Data.OnDiscordClicked();
			};

			root.Q("Matchmaking").AttachView(this, out _matchmakingStatusView);
			_matchmakingStatusView.CloseClicked += Data.OnMatchmakingCancelClicked;

			root.SetupClicks(_services);
			OnAnyPartyUpdate();
			UpdateSquadsButtonVisibility();
		}

		private void OnItemRewarded(ItemRewardedMessage msg)
		{
			if (msg.Item.Id.IsInGroup(GameIdGroup.Collection))
			{
				_collectionNotification.SetDisplay(_services.RewardService.UnseenItems(ItemMetadataType.Collection).Any());
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			_equipmentNotification.SetDisplay(_dataProvider.UniqueIdDataProvider.NewIds.Count > 0);
			_collectionNotification.SetDisplay(_services.RewardService.UnseenItems(ItemMetadataType.Collection).Any());
#if !STORE_BUILD && !UNITY_EDITOR
			_outOfSyncWarningLabel.SetDisplay(VersionUtils.IsOutOfSync());
#else
			_outOfSyncWarningLabel.SetDisplay(false);
#endif
			_betaLabel.SetDisplay(FeatureFlags.BETA_VERSION);

			UpdatePFP();
			UpdatePlayerNameColor(_services.LeaderboardService.CurrentRankedEntry.Position);
		}

		private void OnRankingUpdateHandler(PlayerLeaderboardEntry leaderboardEntry)
		{
			UpdatePlayerNameColor(leaderboardEntry.Position);
		}

		private void UpdatePlayerNameColor(int leaderboardRank)
		{
			var nameColor = _services.LeaderboardService.GetRankColor(_services.LeaderboardService.Ranked, leaderboardRank);
			_playerNameLabel.style.color = nameColor;
		}

		private void UpdatePFP()
		{
			_avatar.SetLocalPlayerData(_dataProvider, _services);
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_dataProvider.AppDataProvider.DisplayName.InvokeObserve(OnDisplayNameChanged);
			_dataProvider.PlayerDataProvider.Trophies.InvokeObserve(OnTrophiesChanged);
			_dataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.CS, OnPoolChanged);
			_dataProvider.ResourceDataProvider.ResourcePools.InvokeObserve(GameId.BPP, OnPoolChanged);
			_dataProvider.BattlePassDataProvider.CurrentPoints.InvokeObserve(OnBattlePassCurrentPointsChanged);
			_services.GameModeService.SelectedGameMode.InvokeObserve(OnSelectedGameModeChanged);
			SubscribeToSquadEvents();
			_updatePoolsCoroutine = _services.CoroutineService.StartCoroutine(UpdatePoolLabels());
			_services.MatchmakingService.IsMatchmaking.Observe(OnIsMatchmakingChanged);
			_dataProvider.PlayerDataProvider.Level.InvokeObserve(OnFameChanged);
			_services.LeaderboardService.OnRankingUpdate += OnRankingUpdateHandler;
			_services.MessageBrokerService.Subscribe<ItemRewardedMessage>(OnItemRewarded);
			_services.MessageBrokerService.Subscribe<ClaimedRewardsMessage>(OnClaimedRewards);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();
			_dataProvider.AppDataProvider.DisplayName.StopObserving(OnDisplayNameChanged);
			_dataProvider.PlayerDataProvider.Trophies.StopObserving(OnTrophiesChanged);
			_services.GameModeService.SelectedGameMode.StopObserving(OnSelectedGameModeChanged);
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.CS);
			_dataProvider.CurrencyDataProvider.Currencies.StopObserving(GameId.BLST);
			_dataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.CS);
			_dataProvider.ResourceDataProvider.ResourcePools.StopObserving(GameId.BPP);
			_dataProvider.BattlePassDataProvider.CurrentPoints.StopObserving(OnBattlePassCurrentPointsChanged);
			_services.MessageBrokerService.UnsubscribeAll(this);
			_services.MatchmakingService.IsMatchmaking.StopObserving(OnIsMatchmakingChanged);
			_services.LeaderboardService.OnRankingUpdate -= OnRankingUpdateHandler;

			UnsubscribeFromSquadEvents();

			if (_updatePoolsCoroutine != null)
			{
				_services.CoroutineService.StopCoroutine(_updatePoolsCoroutine);
				_updatePoolsCoroutine = null;
			}
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
		
		private void OnClaimedRewards(ClaimedRewardsMessage msg)
		{
			Data.OnRewardsReceived(msg.Rewards);
		}

		private void OnDisplayNameChanged(string _, string current)
		{
			_playerNameLabel.text = _dataProvider.AppDataProvider.DisplayNameTrimmed;
		}

		private void OnFameChanged(uint previous, uint current)
		{
			_avatar.SetLevel(current);

			if (previous != current && previous > 0)
			{
				Data.OnLevelUp(); // TODO: This should be handled from the state machine
			}

			// TODO: Animate VFX when we have a progress bar: StartCoroutine(AnimateCurrency(GameId.Trophies, previous, current, _avatar));
		}

		private void OnSelectedGameModeChanged(GameModeInfo _, GameModeInfo current)
		{
			UpdateGameModeButton();
		}

		private IEnumerator AnimateCurrency(GameId id, ulong previous, ulong current, Label label)
		{
			_currentAnimations.Add(id);
			yield return new WaitForSeconds(0.1f);

			label.text = previous.ToString();

			for (int i = 0; i < Mathf.Clamp((current - previous) / 5, 3, 10); i++)
			{
				_mainMenuServices.UiVfxService.PlayVfx(id,
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

		private void OnPoolChanged(GameId id, ResourcePoolData previous, ResourcePoolData current,
								   ObservableUpdateType updateType)
		{
			UpdatePoolLabels();
		}

		private IEnumerator UpdatePoolLabels()
		{
			var waitForSeconds = new WaitForSeconds(GameConstants.Network.NETWORK_ATTEMPT_RECONNECT_SECONDS);

			while (true)
			{
				UpdatePool(GameId.BPP, BPP_POOL_AMOUNT_FORMAT, _bppPoolRestockTimeLabel, _bppPoolRestockAmountLabel,
					_bppPoolAmountLabel);
				UpdatePool(GameId.CS, CS_POOL_AMOUNT_FORMAT, _csPoolRestockTimeLabel, _csPoolRestockAmountLabel,
					_csPoolAmountLabel);

				yield return waitForSeconds;
			}
		}

		private void UpdatePool(GameId id, string amountStringFormat, Label timeLabel, Label restockAmountLabel,
								Label poolAmountLabel)
		{
			var poolInfo = _dataProvider.ResourceDataProvider.GetResourcePoolInfo(id);
			var timeLeft = poolInfo.NextRestockTime - DateTime.UtcNow;

			poolAmountLabel.text = string.Format(amountStringFormat, poolInfo.CurrentAmount, poolInfo.PoolCapacity);

			if (poolInfo.IsFull)
			{
				timeLabel.text = string.Empty;
				restockAmountLabel.text = string.Empty;
			}
			else
			{
				restockAmountLabel.text = $"+ {poolInfo.RestockPerInterval}";
				timeLabel.text = string.Format(
					ScriptLocalization.UITHomeScreen.resource_pool_restock_time,
					timeLeft.ToHoursMinutesSeconds());
			}
		}

		private void OnBattlePassCurrentPointsChanged(uint previous, uint current)
		{
			UpdateBattlePassReward();

			if (current > previous && _initialized.Contains(GameId.BPP) && !_currentAnimations.Contains(GameId.BPP))
			{
				StartCoroutine(AnimateBPP(GameId.BPP, previous, current));
			}
			else
			{
				_initialized.Add(GameId.BPP);
				UpdateBattlePassPoints((int) current);
			}
		}

		private IEnumerator AnimateBPP(GameId id, ulong previous, ulong current)
		{
			_currentAnimations.Add(id);
			// Apparently this initial delay is a must, otherwise "GetPositionOnScreen" starts throwing "Element out of bounds" exception OCCASIONALLY
			// I guess it depends on how long the transition to home screen take; so these errors still may appear
			yield return new WaitForSeconds(0.1f);

			var pointsDiff = (int) current - (int) previous;
			var pointsToAnimate = Mathf.Clamp((current - previous) / 10, 3, 10);
			var pointSegment = Mathf.RoundToInt(pointsDiff / pointsToAnimate);

			var pointSegments = new List<int>();

			// Split all points to animate into segments without any precision related errors due to division
			while (pointsDiff > 0)
			{
				var newSegment = pointSegment;

				if (pointSegment > pointsDiff)
				{
					newSegment = pointsDiff;
				}

				pointsDiff -= newSegment;
				pointSegments.Add(newSegment);
			}

			var totalSegmentPointsRedeemed = 0;
			var segmentIndex = 0;

			// Fire point segment VFX and update points
			foreach (var segment in pointSegments)
			{
				totalSegmentPointsRedeemed += segment;
				segmentIndex += 1;

				var points = (int) previous + totalSegmentPointsRedeemed;
				var wasRedeemable = _dataProvider.BattlePassDataProvider.IsRedeemable((int) previous);

				_mainMenuServices.UiVfxService.PlayVfx(id,
					segmentIndex * 0.05f,
					_playButton.GetPositionOnScreen(Root),
					_battlePassProgressElement.GetPositionOnScreen(Root),
					() =>
					{
						_services.AudioFxService.PlayClip2D(AudioId.CounterTick1);

						if (wasRedeemable) return;

						UpdateBattlePassPoints(points);
					});
			}

			_currentAnimations.Remove(id);
		}

		private void UpdateGameModeButton()
		{
			var current = _services.GameModeService.SelectedGameMode.Value.Entry;
			_gameModeLabel.text = LocalizationUtils.GetTranslationForGameModeId(current.GameModeId);

			var hasPool = current.AllowedRewards.Contains(GameId.CS)
				&& _dataProvider.ResourceDataProvider.GetResourcePoolInfo(GameId.CS).PoolCapacity > 0;
			_csPoolContainer.SetDisplay(hasPool);
			_playButtonContainer.EnableInClassList("button-with-pool", hasPool);

			_gameModeLabel.EnableInClassList("game-mode-button__mode--multiple-line",
				_gameModeLabel.text.Contains("\\n"));

			_gameModeButton.SetEnabled(!_partyService.HasParty.Value && !_partyService.OperationInProgress.Value);
		}

		private void UpdatePlayButton(bool forceLoading = false)
		{
			var translationKey = ScriptTerms.UITHomeScreen.play;
			var buttonClass = string.Empty;
			var buttonEnabled = true;

			if (forceLoading || _services.PartyService.OperationInProgress.Value ||
				_services.MatchmakingService.IsMatchmaking.Value)
			{
				buttonClass = "play-button--loading";
				buttonEnabled = false;
			}
			else if (_services.PartyService.HasParty.Value && _services.PartyService.GetLocalMember() != null)
			{
				if (_services.PartyService.GetLocalMember().Leader)
				{
					if (!_services.PartyService.PartyReady.Value)
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
					var isReady = _services.PartyService.GetLocalMember()!.Ready;

					if (isReady)
					{
						buttonClass = "play-button--get-ready";
						translationKey = ScriptTerms.UITHomeScreen.youre_ready;

						// TODO: Would be better to throttle requests than to block players from un-readying themselves
						buttonEnabled = false;
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
			_playButton.Localize(translationKey);
		}

		private void UpdateBattlePassReward()
		{
			var nextLevel = _dataProvider.BattlePassDataProvider.CurrentLevel.Value + 1;
			_battlePassRarity.RemoveSpriteClasses();

			if (nextLevel <= _dataProvider.BattlePassDataProvider.MaxLevel)
			{
				var reward = _dataProvider.BattlePassDataProvider.GetRewardForLevel(nextLevel);
				_battlePassRarity.AddToClassList(UIUtils.GetBPRarityStyle(reward.GameId));
			}
		}

		private void UpdateBattlePassPoints(int points)
		{
			var hasRewards = _dataProvider.BattlePassDataProvider.IsRedeemable(points);
			_battlePassButton.EnableInClassList("battle-pass-button--claimreward", hasRewards);

			if (!hasRewards)
			{
				if (!_dataProvider.BattlePassDataProvider.IsTutorial() &&
					_dataProvider.BattlePassDataProvider.CurrentLevel.Value ==
					_dataProvider.BattlePassDataProvider.MaxLevel)
				{
					_battlePassButton.EnableInClassList("battle-pass-button--completed", true);
					_bppPoolContainer.SetDisplay(false);
				}
				else
				{
					var predictedLevelAndPoints =
						_dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints(points);
					var requiredPoints =
						_dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel(
							(int) predictedLevelAndPoints.Item1);

					_battlePassProgressElement.style.flexGrow = Mathf.Clamp01((float) points / requiredPoints);
					_battlePassProgressLabel.text = $"{points}/{requiredPoints}";
				}
			}
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
