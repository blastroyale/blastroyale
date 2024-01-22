using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Presenters.BattlePass;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.Game.Views.UITK;
using FirstLight.Services;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using Button = UnityEngine.UIElements.Button;


namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles the BattlePass screen - displays the current / next level, the progress, and
	/// shows reward popups when you receive them.
	/// </summary>
	public class BattlePassScreenPresenter : UiToolkitPresenterData<BattlePassScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action BackClicked;
			public IGameUiService UiService;
			public bool DisableInitialScrollAnimation;
		}

		private const string UssBpSegmentFiller = "bp-segment-filler";
		private const float BpSegmentWidth = 450f;

		[SerializeField] private int _scrollToDurationMs = 1500;
		[SerializeField] private Sprite _battlepassLevelSprite;
		[SerializeField] private Sprite _battlepassPremiumSprite;

		private ScrollView _rewardsScroll;
		private VisualElement _leftBar;
		private VisualElement _rightContent;
		private VisualElement _seasonHeader;
		private VisualElement _columnHolder;
		private VisualElement _root;
		private VisualElement _bppProgressBackground;
		private VisualElement _bppProgressFill;
		private VisualElement _nextLevelRoot;
		private LocalizedButton _claimButton;
		private VisualElement _premiumLock;
		private Button _activateButton;
		private ImageButton _fullScreenClaimButton;
		private Label _bppProgressLabel;
		private Label _seasonNumber;
		private Label _nextLevelValueLabel;
		private Label _timeLeftLabel;
		private Label _premiumTitle;
		private Label _freeTitle;
		private LocalizedLabel _seasonEndsLabel;
		private VisualElement _lastRewardBaloon;
		private VisualElement _lastRewardSprite;
		private ScreenHeaderElement _screenHeader;
		private ImageButton _currentReward;
		private VisualElement _endGraphic;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private Dictionary<PassType, List<BattlePassSegmentData>> _segmentData;
		private Dictionary<int, BattlepassLevelColumnElement> _levelElements;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_levelElements = new Dictionary<int, BattlepassLevelColumnElement>();
			_leftBar = root.Q<VisualElement>("LeftBar").Required();
			_rightContent = root.Q<VisualElement>("RightContent").Required();
			_currentReward = root.Q<ImageButton>("CurrentReward").Required();
			_seasonHeader = root.Q<VisualElement>("SeasonHeader").Required();
			_rewardsScroll = root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = root.Q<ScreenHeaderElement>("Header").Required();
			_claimButton = root.Q<LocalizedButton>("ClaimButton").Required();
			_fullScreenClaimButton = root.Q<ImageButton>("FullScreenClaim").Required();
			_nextLevelValueLabel = root.Q<Label>("NextLevelLabel").Required();
			_seasonNumber = root.Q<Label>("SeasonNumber").Required();
			_bppProgressLabel = root.Q<Label>("BppProgressLabel").Required();
			_bppProgressBackground = root.Q("BppBackground").Required();
			_bppProgressFill = root.Q("BppProgress").Required();
			_nextLevelRoot = root.Q("NextLevel").Required();
			_columnHolder = root.Q("ColumnHolder").Required();
			_premiumLock = root.Q("PremiumLock").Required();
			_activateButton = root.Q<Button>("ActivateButton").Required();
			_timeLeftLabel = root.Q<Label>("TimeLeftLabel").Required();
			_premiumTitle = root.Q<Label>("PremiumTitle").Required();
			_freeTitle = root.Q<Label>("FreeTitle").Required();
			_seasonEndsLabel = root.Q<LocalizedLabel>("SeasonEndsLabel").Required();
			_lastRewardBaloon = root.Q("LastRewardBalloon");
			_lastRewardBaloon.RegisterCallback<PointerDownEvent>(e => OnClickLastRewardIcon());
			_lastRewardSprite = root.Q("LastRewardSprite");
			_endGraphic = root.Q("LastReward").Required();
			root.Q<CurrencyDisplayElement>("BBCurrency").AttachView(this, out CurrencyDisplayView _);

			_rewardsScroll.horizontalScroller.valueChanged += OnScroll;
			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;
			//_fullScreenClaimButton.clicked += OnClaimClicked;
			//_claimButton.clicked += OnClaimClicked;
			_activateButton.clicked += ActivateClicked;
			_currentReward.clicked += GoToCurrentReward;

			_fullScreenClaimButton.SetDisplay(false);
			root.Q("RewardShineBlue").Required().AddRotatingEffect(3, 10);
			root.Q("RewardShineYellow").Required().AddRotatingEffect(5, 10);
			_services.MessageBrokerService.Subscribe<BattlePassPurchasedMessage>(OnBpPurchase);
			_services.MessageBrokerService.Subscribe<BattlePassLevelPurchasedMessage>(OnBoughtBpLevel);
		}

		private void OnBoughtBpLevel(BattlePassLevelPurchasedMessage obj)
		{
			InitScreen(true);
		}

		private void OnBpPurchase(BattlePassPurchasedMessage msg)
		{
			ShowRewards(new[] {ItemFactory.Unlock(UnlockSystem.PaidBattlePass)});
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			InitScreenAndSegments();
			FixSafeZone();
		}

		private void OnClickLastRewardIcon()
		{
			ScrollToBpLevel((int) _dataProvider.BattlePassDataProvider.MaxLevel, 1000);
		}

		private void FixSafeZone()
		{
			var safeArea = Screen.safeArea;
			var leftTop =
				RuntimePanelUtils.ScreenToPanel(_leftBar.panel, new Vector2(safeArea.xMin, Screen.height - safeArea.yMax));
			var rightBottom =
				RuntimePanelUtils.ScreenToPanel(_leftBar.panel, new Vector2(Screen.width - safeArea.xMax, safeArea.yMin));

			_leftBar.style.marginLeft = leftTop.x;
			_seasonHeader.style.paddingRight = rightBottom.x;
			_rightContent.style.left = leftTop.x;
		}

		private void InitScreenAndSegments()
		{
			if (_dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig() == null)
			{
				// No season present
				return;
			}

			InitScreen();
			UpdateTimeLeft();
		}

		private void ActivateClicked()
		{
			var price = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig().Season.Price;

			_services.GenericDialogService.OpenPurchaseOrNotEnough(
				new GenericPurchaseDialogPresenter.GenericPurchaseOptions()
				{
					ItemSprite = _battlepassPremiumSprite,
					OverwriteTitle = ScriptLocalization.UITBattlePass.buy_premium_batttlepass_popup_title,
					OverwriteItemName = ScriptLocalization.UITBattlePass.buy_premium_batttlepass_popup_item_name,
					Value = price,
					OnConfirm = () =>
					{
						_services.CommandService.ExecuteCommand(new ActivateBattlepassCommand());
					},
					OnExit = () =>
					{
						if (_services.IAPService.RequiredToViewStore)
						{
							Data.BackClicked.Invoke();
						}
					}
				});
		}

		private void BuyLevelClicked()
		{
			var price = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig().Season.BuyLevelPrice;

			_services.GenericDialogService.OpenPurchaseOrNotEnough(
				new GenericPurchaseDialogPresenter.GenericPurchaseOptions()
				{
					Value = price,
					ItemSprite = _battlepassLevelSprite,
					OverwriteTitle = ScriptLocalization.UITBattlePass.buy_level_popup_title,
					OverwriteItemName = ScriptLocalization.UITBattlePass.buy_level_popup_item_name,
					OnConfirm = () =>
					{
						_services.CommandService.ExecuteCommand(new BuyBattlepassLevelCommand());
					},
					OnExit = () =>
					{
						if (_services.IAPService.RequiredToViewStore)
						{
							Data.BackClicked.Invoke();
						}
					}
				});
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);
			_dataProvider.BattlePassDataProvider.CurrentPoints.Observe(OnBpPointsChanged);
		}

		protected override void UnsubscribeFromEvents()
		{
			base.UnsubscribeFromEvents();

			_services.MessageBrokerService.UnsubscribeAll(this);
			_dataProvider.BattlePassDataProvider.CurrentPoints.StopObservingAll(this);
		}

		private void UpdateTimeLeft()
		{
			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();

			var now = DateTime.UtcNow;
			var endsAt = battlePassConfig.Season.GetEndsAtDateTime();
			if (now > endsAt)
			{
				_seasonEndsLabel.text = ScriptLocalization.UITBattlePass.season_ended;
				_timeLeftLabel.SetVisibility(false);
			}
			else
			{
				var duration = endsAt - now;
				_seasonEndsLabel.text = ScriptLocalization.UITBattlePass.season_ends_in;
				_timeLeftLabel.text = duration.ToDayAndHours(true);
				_seasonEndsLabel.SetVisibility(true);
				_timeLeftLabel.SetVisibility(true);
			}
		}

		public void EnableFullScreenClaim(bool enableFullScreenClaim)
		{
			_fullScreenClaimButton.SetDisplay(enableFullScreenClaim);
		}

		private void OnSegmentRewardClicked(BattlepassSegmentButtonElement view)
		{
			FLog.Verbose("Claiming BP Rewards");
			OnClaimClicked(view.SegmentData.PassType);
		}

		private void OnClaimClicked(PassType type)
		{
			EnableFullScreenClaim(false);
			if (_dataProvider.BattlePassDataProvider.HasUnclaimedRewards())
			{
				_services.CommandService.ExecuteCommand(new RedeemBPPCommand()
				{
					PassType = type
				});
			}
		}

		private void OnBpPointsChanged(uint previous, uint next)
		{
			//TODO: DO WE NEED ?
		}

		private bool IsDisablePremium()
		{
			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();
			return battlePassConfig.Season.RemovePaid;
		}

		private bool IsDisableEndGraphic()
		{
			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();
			return battlePassConfig.Season.RemoveEndGraphic;
		}

		private void InitScreen(bool update = false)
		{
			if (IsDisablePremium())
			{
				Root.AddToClassList("screen-root--no-paid");
			}

			_segmentData = new ()
			{
				{PassType.Free, new List<BattlePassSegmentData>()},
				{PassType.Paid, new List<BattlePassSegmentData>()}
			};

			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();


			_activateButton.text = ScriptLocalization.UITBattlePass.activate_premium_button_text;
			_premiumTitle.text = ScriptLocalization.UITBattlePass.left_bar_premium_title;
			_freeTitle.text = ScriptLocalization.UITBattlePass.left_bar_free_title;
			_activateButton.SetDisplay(!_dataProvider.BattlePassDataProvider.HasPurchasedSeason());
			_premiumLock.SetDisplay(!_dataProvider.BattlePassDataProvider.HasPurchasedSeason());
			_claimButton.SetDisplay(_dataProvider.BattlePassDataProvider.HasUnclaimedRewards());
			_nextLevelRoot.SetDisplay(predictedProgress.Item1 < _dataProvider.BattlePassDataProvider.MaxLevel);

			var predictedMaxProgress =
				_dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int) predictedProgress.Item1);
			_bppProgressLabel.text = predictedProgress.Item2 + "/" + predictedMaxProgress;
			_nextLevelValueLabel.text = (predictedProgress.Item1 + 1).ToString();
			float pctCurrentLevel = (float) predictedProgress.Item2 / predictedMaxProgress;
			_bppProgressFill.style.width = Length.Percent(pctCurrentLevel * 100f);

			_seasonNumber.text = string.Format(ScriptLocalization.UITBattlePass.season_number,
				battlePassConfig.Season.Number);


			for (var i = 0; i < battlePassConfig.Levels.Count; ++i)
			{
				var freeSegmentData = new BattlePassSegmentData
				{
					SegmentLevel = (uint) i,
					LevelAfterClaiming = predictedProgress.Item1,
					PointsAfterClaiming = predictedProgress.Item2,
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId],
					PassType = PassType.Free
				};

				var paidSegmentData = new BattlePassSegmentData
				{
					SegmentLevel = (uint) i,
					LevelAfterClaiming = predictedProgress.Item1,
					PointsAfterClaiming = predictedProgress.Item2,
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].PremiumRewardId],
					PassType = PassType.Paid
				};

				_segmentData[PassType.Free].Add(freeSegmentData);
				_segmentData[PassType.Paid].Add(paidSegmentData);

				var completed = freeSegmentData.LevelAfterClaiming > i;
				var currentLevel = freeSegmentData.LevelAfterClaiming == i;
				var column = update ? _levelElements[i] : new BattlepassLevelColumnElement();
				ConfigureSegment(column.FreeReward, freeSegmentData, update);
				ConfigureSegment(column.PaidReward, paidSegmentData, update);
				if (IsDisablePremium())
				{
					column.DisablePaid();
				}
				
				if(IsDisableEndGraphic())
				{
					_endGraphic.SetDisplay(false);
				}

				column.SetBarData((uint) i + 1, completed, currentLevel, battlePassConfig.Season.BuyLevelPrice);
				if (!update)
				{
					column.OnBuyLevelClicked += BuyLevelClicked;
					_columnHolder.Insert(0, column);
				}

				_levelElements[i] = column;
			}

			SpawnScrollFiller();
			UpdateLastRewardBubbleSprite().Forget();
			
			if (predictedProgress.Item1 > 1)
			{
				ScrollToBpLevel((int) predictedProgress.Item1, _scrollToDurationMs, Data.DisableInitialScrollAnimation && !update);
			}

			// Disable current reward bubble
			_currentReward.SetDisplay(false);
		}

		public async UniTaskVoid UpdateLastRewardBubbleSprite()
		{
			// Go ahead miha, you can yell at me
			var currentSeason = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();
			var type = currentSeason.Season.RemovePaid ? PassType.Free : PassType.Paid;
			var rewards = _dataProvider.BattlePassDataProvider.GetRewardConfigs(currentSeason.Levels.Select((_, e) => (uint)e+1), type);
			rewards.Reverse();
			var bestReward = rewards.First();
			var itemData = _dataProvider.RewardDataProvider.CreateItemFromConfig(bestReward);
			var loadTask = _services.CollectionService.LoadCollectionItemSprite(itemData);
			await UIUtils.SetSprite(loadTask, _lastRewardSprite);
		}

		public RewardState GetRewardState(BattlePassSegmentData segment)
		{
			if (_dataProvider.BattlePassDataProvider.IsRewardClaimed(
					segment.LevelNeededToClaim, segment.PassType))
				return RewardState.Claimed;

			if (segment.RewardConfig.GameId != GameId.Random && _dataProvider.BattlePassDataProvider.IsRewardClaimable(
					segment.LevelAfterClaiming, segment.LevelNeededToClaim, segment.PassType))
				return RewardState.Claimable;

			return RewardState.NotReached;
		}

		private void ConfigureSegment(BattlepassSegmentButtonElement element, BattlePassSegmentData segment, bool update)
		{
			var state = GetRewardState(segment);
			if (update && element.RewardState == state)
			{
				return;
			}

			element.SetData(segment, GetRewardState(segment), _dataProvider.BattlePassDataProvider.HasPurchasedSeason(), segment.PassType == PassType.Free && !IsDisablePremium());
			if (update) return;
			element.Clicked += OnSegmentRewardClicked;
		}

		private void GoToCurrentReward()
		{
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			ScrollToBpLevel((int) predictedProgress.Item1, 1000);
		}

		private void UpdateGoToCurrentRewardButton()
		{
			var level = Math.Min((int) _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints().Item1, (int)_dataProvider.BattlePassDataProvider.MaxLevel-1);
			var isCurrentLevelOnScreen = _levelElements[level].IsInScreen(_rightContent);
			if (isCurrentLevelOnScreen)
			{
				_currentReward.SetDisplay(false);
				return;
			}

			_currentReward.SetDisplay(true);
			var scrollTarget = GetScrollTargetForElement(level);
			var scroll = _rewardsScroll.scrollOffset.x;
			_currentReward.RemoveModifiers();
			if (scroll > scrollTarget)
			{
				_currentReward.AddToClassList("current-reward-cloud--left");
			}
		}

		private void OnScroll(float x)
		{
			var lastElement = _levelElements.Last().Value;
			_lastRewardBaloon.SetDisplay(!lastElement.IsInScreen(Root));
			UpdateGoToCurrentRewardButton();
		}

		private void ScrollToBpLevel(int index, int durationMs, bool instant = false)
		{
			var targetX = GetScrollTargetForElement(index);
			if (instant)
			{
				_rewardsScroll.scrollOffset = new Vector2(targetX, _rewardsScroll.scrollOffset.y);
				return;
			}

			var startX = _rewardsScroll.scrollOffset.x;
			var offset = targetX - startX;
			_rewardsScroll.experimental.animation.Start(0, 1f, durationMs, (element, percent) =>
			{
				var scrollView = (ScrollView) element;
				var currentScroll = scrollView.scrollOffset;
				scrollView.scrollOffset = new Vector2(startX + (offset * percent), currentScroll.y);
			}).Ease(Easing.OutCubic);
		}

		private float GetScrollTargetForElement(int index)
		{
			if (index >= _dataProvider.BattlePassDataProvider.MaxLevel) index = (int) _dataProvider.BattlePassDataProvider.MaxLevel - 1;
			var targetX = ((index + 1) * BpSegmentWidth) - (BpSegmentWidth * 3);
			return Math.Max(0, targetX);
		}

		private void SpawnScrollFiller()
		{
			var filler = new VisualElement {name = "background"};
			_rewardsScroll.Add(filler);
			filler.pickingMode = PickingMode.Ignore;
			filler.AddToClassList(UssBpSegmentFiller);
		}

		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
			ShowRewards(message.Rewards);
		}

		private void ShowRewards(IEnumerable<ItemData> rewards)
		{
			var battlePassData = Data;
			battlePassData.DisableInitialScrollAnimation = true;
			Data.UiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
				SkipSummary = true,
				Items = rewards,
				OnFinish = () =>
				{
					_services.MessageBrokerService.Publish(new FinishedClaimingBpRewardsMessage());
					_uiService.OpenScreen<BattlePassScreenPresenter, StateData>(battlePassData);
				}
			});
		}
	}
}