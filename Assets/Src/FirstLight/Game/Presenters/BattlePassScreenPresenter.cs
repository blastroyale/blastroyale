using System;
using System.Collections.Generic;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.Game.Views.UITK;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;


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

		[SerializeField] private VisualTreeAsset _battlePassSegmentAsset;
		[SerializeField] private int _scrollToDurationMs = 1500;

		private ScrollView _rewardsScroll;
		private VisualElement _upperRow;
		private VisualElement _bottomRow;
		private VisualElement _root;
		private VisualElement _bppProgressBackground;
		private VisualElement _bppProgressFill;
		private VisualElement _nextLevelRoot;
		private LocalizedButton _claimButton;
		private ImageButton _fullScreenClaimButton;
		private Label _bppProgressLabel;
		private Label _currentLevelLabel;
		private Label _nextLevelValueLabel;
		private Label _timeLeftLabel;
		private LocalizedLabel _seasonEndsLabel;
		private ScreenHeaderElement _screenHeader;

		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private Dictionary<PassType, List<BattlePassSegmentData>> _segmentData;
		private Dictionary<PassType, List<BattlePassSegmentView>> _segmentViews;
		
		private bool _finishedTutorialBpThisCycle = false;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_segmentViews = new Dictionary<PassType, List<BattlePassSegmentView>>();
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_rewardsScroll = root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = root.Q<ScreenHeaderElement>("Header").Required();
			_claimButton = root.Q<LocalizedButton>("ClaimButton").Required();
			_fullScreenClaimButton = root.Q<ImageButton>("FullScreenClaim").Required();
			_currentLevelLabel = root.Q<Label>("CurrentLevelValue").Required();
			_nextLevelValueLabel = root.Q<Label>("NextLevelValue").Required();
			_bppProgressLabel = root.Q<Label>("BppProgressLabel").Required();
			_bppProgressBackground = root.Q("BppBackground").Required();
			_bppProgressFill = root.Q("BppProgress").Required();
			_nextLevelRoot = root.Q("NextLevel").Required();
			_upperRow = root.Q("UpperRow").Required();
			_bottomRow = root.Q("BottomRow").Required();
			_timeLeftLabel = root.Q<Label>("TimeLeftLabel").Required();
			_seasonEndsLabel = root.Q<LocalizedLabel>("SeasonEndsLabel").Required();
			root.Q<CurrencyDisplayElement>("CSCurrency").AttachView(this, out CurrencyDisplayView _);
			root.Q<CurrencyDisplayElement>("CoinCurrency").AttachView(this, out CurrencyDisplayView _);

			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;
			_fullScreenClaimButton.clicked += OnClaimClicked;
			_claimButton.clicked += OnClaimClicked;

			_fullScreenClaimButton.SetDisplay(false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			InitScreenAndSegments();
		}

		public void CloseManual()
		{
			Data.BackClicked();
		}

		private void InitScreenAndSegments()
		{
			InitScreen();
			RemoveAllSegments();
			SpawnSegments();
			InitSegments();
			UpdateTimeLeft();

			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();

			if (predictedProgress.Item1 > 1)
			{
				ScrollToBpLevel((int) predictedProgress.Item1, _scrollToDurationMs);
			}
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
			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetBattlePassConfig();
			if (battlePassConfig.TryGetEndsAt(out var endsAt))
			{
				var now = DateTime.UtcNow;
				
				if (now > endsAt)
				{
					_seasonEndsLabel.text = "SEASON ENDED!";
					_timeLeftLabel.SetVisibility(false);
				}
				else
				{
					var duration = endsAt - now;
					_seasonEndsLabel.text = "SEASON ENDS IN";
					_timeLeftLabel.text = duration.ToDayAndHours().ToUpperInvariant();
				}
			
				return;
			}
			_seasonEndsLabel.SetVisibility(false);
			_timeLeftLabel.SetVisibility(false);
		}
		
		public void EnableFullScreenClaim(bool enableFullScreenClaim)
		{
			_fullScreenClaimButton.SetDisplay(enableFullScreenClaim);
		}

		private void OnSegmentRewardClicked(BattlePassSegmentView view)
		{
			OnClaimClicked();
		}

		private void OnClaimClicked()
		{
			EnableFullScreenClaim(false);
			if (_dataProvider.BattlePassDataProvider.HasUnclaimedRewards())
			{
				_services.CommandService.ExecuteCommand(new RedeemBPPCommand());
			}
		}

		private void OnBpPointsChanged(uint previous, uint next)
		{
			InitScreen();
			InitSegments();
		}

		private void InitScreen()
		{
			_segmentData = new ()
			{
				{ PassType.Free, new List<BattlePassSegmentData>() },
				{ PassType.Paid, new List<BattlePassSegmentData>() }
			};
			_segmentViews = new ()
			{
				{ PassType.Free, new List<BattlePassSegmentView>() },
				{ PassType.Paid, new List<BattlePassSegmentView>() }
			};

			var battlePassConfig = _dataProvider.BattlePassDataProvider.GetBattlePassConfig();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();

			_claimButton.SetDisplay(_dataProvider.BattlePassDataProvider.HasUnclaimedRewards());
			_nextLevelRoot.SetDisplay(predictedProgress.Item1 < _dataProvider.BattlePassDataProvider.MaxLevel);

			var predictedMaxProgress =
				_dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int) predictedProgress.Item1);
			_bppProgressLabel.text = predictedProgress.Item2 + "/" + predictedMaxProgress;
			_currentLevelLabel.text = (predictedProgress.Item1 + 1).ToString();
			_nextLevelValueLabel.text = (predictedProgress.Item1 + 2).ToString();

			_bppProgressFill.style.flexGrow = (float) predictedProgress.Item2 / predictedMaxProgress;
			if (predictedProgress.Item1 >= _dataProvider.BattlePassDataProvider.MaxLevel)
			{
				_currentLevelLabel.text = _bppProgressLabel.text = ScriptLocalization.UITBattlePass.max;
			}
			
			_screenHeader.SetTitle(string.Format(ScriptLocalization.UITBattlePass.season_number,
				battlePassConfig.CurrentSeason));
			
			
			for (int i = 0; i < battlePassConfig.Levels.Count; ++i)
			{
				var data = new BattlePassSegmentData
				{
					SegmentLevel = (uint) i,
					LevelAfterClaiming = predictedProgress.Item1,
					PointsAfterClaiming = predictedProgress.Item2,
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId],
					PassType = PassType.Free
				};
				
				// Copy the struct, since its value type this is a copy and not a reference
				var premiumSegment = data;
				premiumSegment.PassType = PassType.Paid;
				var premiumRewardId = battlePassConfig.Levels[i].PremiumRewardId;
				if (premiumRewardId >= 0) premiumSegment.RewardConfig = rewardConfig[premiumRewardId];
				else premiumSegment.RewardConfig = default;

				_segmentData[PassType.Free].Add(data);
				_segmentData[PassType.Paid].Add(premiumSegment);
			}
		}

		private BattlePassSegmentView CreateNewSegmentView(BattlePassSegmentData segment)
		{
			var segmentInstance = _battlePassSegmentAsset.Instantiate();
			segmentInstance.AttachView(this, out BattlePassSegmentView view);
			view.Clicked += OnSegmentRewardClicked;
			segmentInstance.userData = view;
			_segmentViews[segment.PassType].Add(view);
			view.InitWithData(segment);
			return view;
		}

		private void SpawnSegments()
		{
			SpawnScrollFiller();

			foreach (var segment in _segmentData[PassType.Free])
			{
				_bottomRow.Add(CreateNewSegmentView(segment).Element);
			}
			
			foreach (var segment in _segmentData[PassType.Paid])
			{
				_upperRow.Add(CreateNewSegmentView(segment).Element);
			}

			// Shuffle all the items to front so they are arranged properly
			// This is done as the elements overlay on top of each other, and they need to be flexed/arranged 
			// in a specific way to keep correct render order
			foreach (var (type, views) in _segmentViews)
			{
				foreach(var view in views) view.Element.BringToFront();
			}

			// Add filler to end of BP so it looks nicer
			SpawnScrollFiller();
		}

		private void RemoveAllSegments()
		{
			foreach (var (type, views) in _segmentViews)
			{
				foreach (var view in views)
				{
					view.Element.RemoveFromHierarchy();
				}
			}
		}

		private void InitSegments()
		{
			foreach (var (type, views) in _segmentViews)
			{
				foreach (var view in views)
				{
					var updatedData = _segmentData[type][(int) view.SegmentData.SegmentLevel];
					view.InitWithData(updatedData);
				}
			}
		}

		private void ScrollToBpLevel(int index, int durationMs)
		{
			var targetX = ((index + 1) * BpSegmentWidth) - BpSegmentWidth;

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
			var battlePassData = Data;
			Data.UiService.OpenScreen<RewardsScreenPresenter, RewardsScreenPresenter.StateData>(new RewardsScreenPresenter.StateData()
			{
				Items = message.Rewards,
				OnFinish = () =>
				{
					if (_finishedTutorialBpThisCycle)
					{
						CompleteTutorialPass();
					}

					_services.MessageBrokerService.Publish(new FinishedClaimingBpRewardsMessage());
					_uiService.OpenScreen<BattlePassScreenPresenter, StateData>(battlePassData);
				}
			});
		}

		// TODO: Add some faff jazz & wiggs
		private void CompleteTutorialPass()
		{
			_finishedTutorialBpThisCycle = false;
			_services.GenericDialogService.OpenButtonDialog("", ScriptLocalization.FTUE.BPComplete, false,
				new GenericDialogButton()
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = InitScreenAndSegments
				});
		}
	}
}