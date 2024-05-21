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
using FirstLight.UIService;
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
	public class BattlePassScreenPresenter : UIPresenterData<BattlePassScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action BackClicked;
			public Action RewardsClaimed;
			public bool DisableScrollAnimation;
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
		private VisualElement _Root;
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
		private VisualElement _endGraphicContainer;
		private VisualElement _endGraphicPicture;
		private Label _endGraphicLabel;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private Dictionary<PassType, List<BattlePassSegmentData>> _segmentData;
		private Dictionary<int, BattlepassLevelColumnElement> _levelElements;

		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements()
		{
			_levelElements = new Dictionary<int, BattlepassLevelColumnElement>();
			_leftBar = Root.Q<VisualElement>("LeftBar").Required();
			_rightContent = Root.Q<VisualElement>("RightContent").Required();
			_currentReward = Root.Q<ImageButton>("CurrentReward").Required();
			_seasonHeader = Root.Q<VisualElement>("SeasonHeader").Required();
			_rewardsScroll = Root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = Root.Q<ScreenHeaderElement>("Header").Required();
			_claimButton = Root.Q<LocalizedButton>("ClaimButton").Required();
			_fullScreenClaimButton = Root.Q<ImageButton>("FullScreenClaim").Required();
			_nextLevelValueLabel = Root.Q<Label>("NextLevelLabel").Required();
			_seasonNumber = Root.Q<Label>("SeasonNumber").Required();
			_bppProgressLabel = Root.Q<Label>("BppProgressLabel").Required();
			_bppProgressBackground = Root.Q("BppBackground").Required();
			_bppProgressFill = Root.Q("BppProgress").Required();
			_nextLevelRoot = Root.Q("NextLevel").Required();
			_columnHolder = Root.Q("ColumnHolder").Required();
			_premiumLock = Root.Q("PremiumLock").Required();
			_activateButton = Root.Q<Button>("ActivateButton").Required();
			_timeLeftLabel = Root.Q<Label>("TimeLeftLabel").Required();
			_premiumTitle = Root.Q<Label>("PremiumTitle").Required();
			_freeTitle = Root.Q<Label>("FreeTitle").Required();
			_seasonEndsLabel = Root.Q<LocalizedLabel>("SeasonEndsLabel").Required();
			_lastRewardBaloon = Root.Q("LastRewardBalloon");
			_lastRewardBaloon.RegisterCallback<PointerDownEvent>(e => OnClickLastRewardIcon());
			_lastRewardSprite = Root.Q("LastRewardSprite");
			_endGraphicContainer = Root.Q("LastReward").Required();
			_endGraphicPicture = _endGraphicContainer.Q("RewardPicture").Required();
			_endGraphicLabel = _endGraphicContainer.Q<Label>("RewardName").Required();
			Root.Q<CurrencyDisplayElement>("BBCurrency").AttachView(this, out CurrencyDisplayView _);

			_rewardsScroll.horizontalScroller.valueChanged += OnScroll;
			_screenHeader.backClicked += Data.BackClicked;
			//_fullScreenClaimButton.clicked += OnClaimClicked;
			//_claimButton.clicked += OnClaimClicked;
			_activateButton.clicked += ActivateClicked;
			_currentReward.clicked += GoToCurrentReward;

			_fullScreenClaimButton.SetDisplay(false);
			Root.Q("RewardShineBlue").Required().AddRotatingEffect(3, 10);
			Root.Q("RewardShineYellow").Required().AddRotatingEffect(5, 10);
			_services.MessageBrokerService.Subscribe<BattlePassPurchasedMessage>(OnBpPurchase);
			_services.MessageBrokerService.Subscribe<BattlePassLevelPurchasedMessage>(OnBoughtBpLevel);
		}

		private void OnBoughtBpLevel(BattlePassLevelPurchasedMessage obj)
		{
			Data.DisableScrollAnimation = false;
			InitScreen(true);
		}

		private void OnBpPurchase(BattlePassPurchasedMessage msg)
		{
			ShowRewards(new[] {ItemFactory.Unlock(UnlockSystem.PaidBattlePass)});
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			InitScreenAndSegments();
			FixSafeZone();

			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);
			_dataProvider.BattlePassDataProvider.CurrentPoints.Observe(OnBpPointsChanged);

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			_services.MessageBrokerService.UnsubscribeAll(this);
			_dataProvider.BattlePassDataProvider.CurrentPoints.StopObservingAll(this);

			return base.OnScreenClose();
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
				new GenericPurchaseDialogPresenter.StateData
				{
					ItemSprite = _battlepassPremiumSprite,
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
			var blastBucks = _dataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.BlastBuck);
			var canBuy = _dataProvider.BattlePassDataProvider.GetMaxPurchasableLevels(blastBucks);

			void OnExit()
			{
				if (_services.IAPService.RequiredToViewStore)
				{
					Data.BackClicked.Invoke();
				}
			}

			if (canBuy <= 1)
			{
				_services.GenericDialogService.OpenPurchaseOrNotEnough(
					new GenericPurchaseDialogPresenter.StateData
					{
						Value = _dataProvider.BattlePassDataProvider.GetPriceForBuying(1),
						ItemSprite = _battlepassLevelSprite,
						OverwriteItemName = ScriptLocalization.UITBattlePass.buy_level_popup_item_name,
						OnConfirm = () =>
						{
							_services.CommandService.ExecuteCommand(new BuyBattlepassLevelCommand {Levels = 1});
						},
						OnExit = OnExit
					});
				return;
			}

			_services.UIService.OpenScreen<BuyBattlepassLevelPopupPresenter>(new BuyBattlepassLevelPopupPresenter.StateData
			{
				OwnedCurrency = blastBucks,
				OnConfirm = levels =>
				{
					_services.CommandService.ExecuteCommand(new BuyBattlepassLevelCommand {Levels = (uint) levels});
				},
				OnExit = OnExit
			});
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
			Data.RewardsClaimed();
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

		private void InitScreen(bool update = false)
		{
			if (IsDisablePremium())
			{
				Root.AddToClassList("screen-Root--no-paid");
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

				if (string.IsNullOrEmpty(battlePassConfig.Season.EndGraphicImageClass))
				{
					_endGraphicContainer.SetDisplay(false);
				}
				else
				{
					_endGraphicContainer.SetDisplay(true);
					_endGraphicPicture.RemoveSpriteClasses();
					_endGraphicPicture.AddToClassList(battlePassConfig.Season.EndGraphicImageClass);
					_endGraphicLabel.text = battlePassConfig.Season.EndGraphicName;
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

			if (predictedProgress.Item1 > 1 && update)
			{
				ScrollToBpLevel((int) predictedProgress.Item1, _scrollToDurationMs, Data.DisableScrollAnimation);
			}

			// Disable current reward bubble
			_currentReward.SetDisplay(false);
			_rewardsScroll.RegisterCallback<GeometryChangedEvent>(OnFinishedRewardScroll);
		}

		private void OnFinishedRewardScroll(GeometryChangedEvent ev)
		{
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			ScrollToBpLevel((int) predictedProgress.Item1, _scrollToDurationMs, Data.DisableScrollAnimation);
			_currentReward.SetDisplay(false);
			_rewardsScroll.UnregisterCallback<GeometryChangedEvent>(OnFinishedRewardScroll);
		}

		public async UniTaskVoid UpdateLastRewardBubbleSprite()
		{
			// Go ahead miha, you can yell at me
			var currentSeason = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();
			var type = currentSeason.Season.RemovePaid ? PassType.Free : PassType.Paid;
			var rewards = _dataProvider.BattlePassDataProvider.GetRewardConfigs(currentSeason.Levels.Select((_, e) => (uint) e + 1), type);
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
			var level = Math.Min((int) _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints().Item1, (int) _dataProvider.BattlePassDataProvider.MaxLevel - 1);
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
			Data.RewardsClaimed();
		}

		private void ShowRewards(IEnumerable<ItemData> rewards)
		{
			var battlePassData = Data;
			battlePassData.DisableScrollAnimation = true;
			
			_services.UIService.OpenScreen<RewardsScreenPresenter>(new RewardsScreenPresenter.StateData()
			{
				SkipSummary = true,
				Items = rewards,
				OnFinish = () =>
				{
					_services.MessageBrokerService.Publish(new FinishedClaimingBpRewardsMessage());
					_services.UIService.OpenScreen<BattlePassScreenPresenter>(battlePassData).Forget();
				}
			}).Forget();
		}
	}
}