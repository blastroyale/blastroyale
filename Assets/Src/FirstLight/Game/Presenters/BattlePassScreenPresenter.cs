using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
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
		private VisualElement _root;
		private VisualElement _bppProgressBackground;
		private VisualElement _bppProgressFill;
		private VisualElement _nextLevelRoot;
		private LocalizedButton _claimButton;
		private Label _bppProgressLabel;
		private Label _currentLevelLabel;
		private Label _nextLevelValueLabel;
		private ScreenHeaderElement _screenHeader;
		
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private List<BattlePassSegmentData> _segmentData;
		private List<KeyValuePair<BattlePassSegmentView, VisualElement>> _segmentViewsAndElements;

		private Queue<KeyValuePair<UniqueId,Equipment>> _pendingRewards;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_segmentViewsAndElements = new List<KeyValuePair<BattlePassSegmentView, VisualElement>>();
			_segmentData = new List<BattlePassSegmentData>();
			_pendingRewards = new Queue<KeyValuePair<UniqueId,Equipment>>();
			
		}
		
		protected override async void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_rewardsScroll = root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = root.Q<ScreenHeaderElement>("Header").Required();
			_claimButton = root.Q<LocalizedButton>("ClaimButton").Required();
			_currentLevelLabel = root.Q<Label>("CurrentLevelValue").Required();
			_nextLevelValueLabel = root.Q<Label>("NextLevelValue").Required();
			_bppProgressLabel = root.Q<Label>("BppProgressLabel").Required();
			_bppProgressBackground = root.Q("BppBackground").Required();
			_bppProgressFill = root.Q("BppProgress").Required();
			_nextLevelRoot = root.Q("NextLevel").Required();
			
			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;
			_claimButton.clicked += OnClaimClicked;
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			InitScreen();
			SpawnSegments();
			
			// Has to be done 1 frame after the segments are spawned, otherwise they don't init correctly
			InitSegments();
			
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();

			if (predictedProgress.Item1 > 1)
			{
				ScrollToBpLevel((int) predictedProgress.Item1,_scrollToDurationMs);
			}
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
		
		private void OnSegmentRewardClicked(BattlePassSegmentView view)
		{
			OnClaimClicked();
		}

		private void OnClaimClicked()
		{
			if (_dataProvider.BattlePassDataProvider.IsRedeemable())
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
			_segmentData.Clear();
			
			var battlePassConfig = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			var currentLevel = _dataProvider.BattlePassDataProvider.CurrentLevel.Value;
			var currentProgress = _dataProvider.BattlePassDataProvider.CurrentPoints.Value;
			
			var predictedMaxProgress = _dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int)predictedProgress.Item1);
			_bppProgressLabel.text = predictedProgress.Item2 + "/" + predictedMaxProgress;
			_currentLevelLabel.text = (predictedProgress.Item1 + 1).ToString();
			_nextLevelValueLabel.text = (predictedProgress.Item1 + 2).ToString();
			
			_bppProgressFill.style.flexGrow = (float) predictedProgress.Item2 / predictedMaxProgress;

			_screenHeader.SetTitle(string.Format(ScriptLocalization.UITBattlePass.season_number, "1"));
			_claimButton.SetDisplay(_dataProvider.BattlePassDataProvider.IsRedeemable());
			_nextLevelRoot.SetDisplay(predictedProgress.Item1 < _dataProvider.BattlePassDataProvider.MaxLevel);
			
			if (predictedProgress.Item1 >= _dataProvider.BattlePassDataProvider.MaxLevel)
			{
				_currentLevelLabel.text = _bppProgressLabel.text = ScriptLocalization.UITBattlePass.max;
			}

			for (int i = 0; i < battlePassConfig.Levels.Count; ++i)
			{
				var data = new BattlePassSegmentData
				{
					SegmentLevel = (uint) i,
					CurrentLevel = currentLevel,
					CurrentProgress = currentProgress,
					PredictedCurrentLevel = predictedProgress.Item1,
					PredictedCurrentProgress = predictedProgress.Item2,
					MaxProgress = _dataProvider.BattlePassDataProvider.GetRequiredPointsForLevel(i),
					MaxLevel = _dataProvider.BattlePassDataProvider.MaxLevel,
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId]
				};

				_segmentData.Add(data);
			}
		}

		private void SpawnSegments()
		{
			// Add filler to start of BP so it looks nicer
			SpawnScrollFiller();
			
			foreach (var segment in _segmentData)
			{
				var segmentInstance = _battlePassSegmentAsset.Instantiate();
				segmentInstance.AttachView(this, out BattlePassSegmentView view);
				view.Clicked += OnSegmentRewardClicked;
				_segmentViewsAndElements.Add(new KeyValuePair<BattlePassSegmentView, VisualElement>(view, segmentInstance));
				_rewardsScroll.Add(segmentInstance);
			}

			// Shuffle all the items to front so they are arranged properly
			// This is done as the elements overlay on top of each other, and they need to be flexed/arranged 
			// in a specific way to keep correct render order
			for (int i = _segmentViewsAndElements.Count-1; i >= 0; i--)
			{
				_segmentViewsAndElements[i].Value.BringToFront();
			}

			// Add filler to end of BP so it looks nicer
			SpawnScrollFiller();
		}

		private void InitSegments()
		{
			for (int i = 0; i < _segmentViewsAndElements.Count; i++)
			{
				_segmentViewsAndElements[i].Key.InitWithData(_segmentData[i]);
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

		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
			var predictedProgress = _dataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			ScrollToBpLevel((int) predictedProgress.Item1, 0);
			
			_pendingRewards.Clear();
			
			foreach (var config in message.Rewards)
			{
				_pendingRewards.Enqueue(config);
			}

			TryShowNextReward();
		}

		private async void TryShowNextReward()
		{
			// Keep showing/dismissing reward dialogs recursively, until all have been shown
			if (Data.UiService.HasUiPresenter<EquipmentRewardDialogPresenter>())
			{
				Data.UiService.CloseUi<EquipmentRewardDialogPresenter>();

				await Task.Delay(GameConstants.Visuals.REWARD_POPUP_CLOSE_MS);
			}

			if (!_pendingRewards.TryDequeue(out var reward))
			{
				return;
			}

			var data = new EquipmentRewardDialogPresenter.StateData()
			{
				ConfirmClicked = TryShowNextReward,
				Equipment = reward.Value,
				EquipmentId = reward.Key
			};

			var popup = await Data.UiService.OpenUiAsync<EquipmentRewardDialogPresenter, EquipmentRewardDialogPresenter.StateData>(data);
			popup.InitEquipment();
		}
	}
}