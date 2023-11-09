using System;
using System.Collections.Generic;
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
		}

		private const string UssBpSegmentFiller = "bp-segment-filler";
		private const float BpSegmentWidth = 475f;

		[SerializeField] private VisualTreeAsset _battlePassSegmentBarAsset;
		[SerializeField] private VisualTreeAsset _battlePassSegmentAsset;
		[SerializeField] private int _scrollToDurationMs = 1500;

		private ScrollView _rewardsScroll;
		private VisualElement _leftBar;
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
		private LocalizedLabel _seasonEndsLabel;
		private ScreenHeaderElement _screenHeader;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private Dictionary<PassType, List<BattlePassSegmentData>> _segmentData;
		private bool _finishedTutorialBpThisCycle = false;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);
			_leftBar = root.Q<VisualElement>("LeftBar").Required();
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
			_seasonEndsLabel = root.Q<LocalizedLabel>("SeasonEndsLabel").Required();
			root.Q("LastRewardBalloon").RegisterCallback<PointerDownEvent>(e => OnClickLastRewardIcon());
			root.Q<CurrencyDisplayElement>("BBCurrency").AttachView(this, out CurrencyDisplayView _);

			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;
			//_fullScreenClaimButton.clicked += OnClaimClicked;
			//_claimButton.clicked += OnClaimClicked;
			_activateButton.clicked += ActivateClicked;

			_fullScreenClaimButton.SetDisplay(false);
			root.Q("RewardShineBlue").Required().AddRotatingEffect(3, 10);
			root.Q("RewardShineYellow").Required().AddRotatingEffect(5, 10);
			_services.MessageBrokerService.Subscribe<BattlePassPurchasedMessage>(OnBpPurchase);
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
			this.ScrollToBpLevel((int)_dataProvider.BattlePassDataProvider.MaxLevel, 1000);
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
			_columnHolder.style.paddingLeft = leftTop.x;
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
			// TODO: Remove, should display/disable in the button
			if (!_dataProvider.BattlePassDataProvider.HasCurrencyForPurchase())
			{
				_services.GenericDialogService.OpenSimpleMessage("[Debug]", "Not enough BBs go buy some");
				return;
			}

			_services.CommandService.ExecuteCommand(new ActivateBattlepassCommand());
		}

		protected override void SubscribeToEvents()
		{
			base.SubscribeToEvents();
			_services.MessageBrokerService.Subscribe<TutorialBattlePassCompleted>(OnTutorialBattlePassCompleted);
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
				_seasonEndsLabel.text = "SEASON ENDED!";
				_timeLeftLabel.SetVisibility(false);
			}
			else
			{
				var duration = endsAt - now;
				_seasonEndsLabel.text = "SEASON ENDS IN ";
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

		private void InitScreen()
		{
			_segmentData = new ()
			{
				{PassType.Free, new List<BattlePassSegmentData>()},
				{PassType.Paid, new List<BattlePassSegmentData>()}
			};

			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetCurrentSeasonConfig();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();

			_activateButton.SetDisplay(!_dataProvider.BattlePassDataProvider.HasPurchasedSeason());
			_premiumLock.SetDisplay(!_dataProvider.BattlePassDataProvider.HasPurchasedSeason());
			_claimButton.SetDisplay(_dataProvider.BattlePassDataProvider.HasUnclaimedRewards());
			_nextLevelRoot.SetDisplay(predictedProgress.Item1 < _dataProvider.BattlePassDataProvider.MaxLevel);

			var predictedMaxProgress =
				_dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int) predictedProgress.Item1);
			_bppProgressLabel.text = predictedProgress.Item2 + "/" + predictedMaxProgress;
			_nextLevelValueLabel.text = (predictedProgress.Item1 + 1).ToString();
			float pctCurrentLevel = (float) predictedProgress.Item2 / predictedMaxProgress;
			_bppProgressFill.style.flexGrow = pctCurrentLevel;

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

				var levelBarPct = 1f;
				if (paidSegmentData.LevelAfterClaiming == i) levelBarPct = pctCurrentLevel;
				else if (paidSegmentData.LevelAfterClaiming < i) levelBarPct = 0;

				var column = new BattlepassLevelColumnElement();
				ConfigureSegment(column.FreeReward, freeSegmentData);
				ConfigureSegment(column.PaidReward, paidSegmentData);
				column.SetBarData((uint)i+1, levelBarPct);
				_columnHolder.Insert(0, column);
			}

			SpawnScrollFiller();

			if (predictedProgress.Item1 > 1)
			{
				ScrollToBpLevel((int) predictedProgress.Item1, _scrollToDurationMs);
			}
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


		private void ConfigureSegment(BattlepassSegmentButtonElement element, BattlePassSegmentData segment)
		{
			element.SetData(segment, GetRewardState(segment), _dataProvider.BattlePassDataProvider.HasPurchasedSeason());
			element.Clicked += OnSegmentRewardClicked;
		}

		private void ScrollToBpLevel(int index, int durationMs)
		{
			if (index >= _dataProvider.BattlePassDataProvider.MaxLevel) index = (int)_dataProvider.BattlePassDataProvider.MaxLevel - 1;
			var targetX = ((index + 1) * BpSegmentWidth) - (BpSegmentWidth * 3);
			_rewardsScroll.experimental.animation.Start(0, 1f, durationMs, (element, percent) =>
			{
				var scrollView = (ScrollView) element;
				var currentScroll = scrollView.scrollOffset;
				scrollView.scrollOffset = new Vector2(targetX * percent, currentScroll.y);
			}).Ease(Easing.OutCubic);
		}

		private void SpawnScrollFiller()
		{
			var filler = new VisualElement {name = "background"};
			_rewardsScroll.Add(filler);
			filler.pickingMode = PickingMode.Ignore;
			filler.AddToClassList(UssBpSegmentFiller);
		}

		private void OnTutorialBattlePassCompleted(TutorialBattlePassCompleted message)
		{
			_finishedTutorialBpThisCycle = true;
		}

		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			ScrollToBpLevel((int) predictedProgress.Item1, 0);
			ShowRewards(message.Rewards);
		}

		private void ShowRewards(IEnumerable<ItemData> rewards)
		{
			var battlePassData = Data;
			Data.UiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
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