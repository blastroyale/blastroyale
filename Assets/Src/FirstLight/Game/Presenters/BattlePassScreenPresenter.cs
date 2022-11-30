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
		[SerializeField] private Ease _scrollEaseMode;
		[SerializeField] private float _scrollToDuration;
		
		private ScrollView _rewardsScroll;
		private VisualElement _root;
		private VisualElement _bppProgressBackground;
		private VisualElement _bppProgressFill;
		private LocalizedButton _claimButton;
		private Label _bppProgressLabel;
		private Label _currentLevelLabel;
		private Label _nextLevelLabel;
		private ScreenHeaderElement _screenHeader;
		
		private IGameServices _services;
		private IGameDataProvider _dataProvider;
		private List<BattlePassSegmentData> _segmentData;
		private List<KeyValuePair<BattlePassSegmentView, VisualElement>> _segmentViewsAndElements;
		private bool _initialized = false;

		private Queue<KeyValuePair<UniqueId,Equipment>> _pendingRewards;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_dataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_segmentViewsAndElements = new List<KeyValuePair<BattlePassSegmentView, VisualElement>>();
			_segmentData = new List<BattlePassSegmentData>();
			_services.MessageBrokerService.Subscribe<BattlePassLevelUpMessage>(OnBattlePassLevelUp);
			_pendingRewards = new Queue<KeyValuePair<UniqueId,Equipment>>();
			_dataProvider.BattlePassDataProvider.CurrentPoints.Observe(OnBpPointsChanged);
		}
		
		protected override async void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_rewardsScroll = root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = root.Q<ScreenHeaderElement>("Header").Required();
			_claimButton = root.Q<LocalizedButton>("ClaimButton").Required();
			_currentLevelLabel = root.Q<Label>("CurrentLevelValue").Required();
			_nextLevelLabel = root.Q<Label>("NextLevelValue").Required();
			_bppProgressLabel = root.Q<Label>("BppProgressLabel").Required();
			_bppProgressBackground = root.Q("BppBackground").Required();
			_bppProgressFill = root.Q("BppProgress").Required();
			
			_screenHeader.SetTitle(string.Format(ScriptLocalization.UITBattlePass.season_number, "1"));
			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;
			_claimButton.clicked += OnClaimClicked;

			await Task.Yield();
			
			InitScreen();
			SpawnInitSegments();

			_initialized = true; 
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
			if (!_initialized) return;
			
			InitScreen();
			UpdateSegments();
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
			_nextLevelLabel.text = (predictedProgress.Item1 + 2).ToString();
			
			var barMaxWidth = _bppProgressBackground.contentRect.width;
			var predictedProgressPercent = (float) predictedProgress.Item2 / predictedMaxProgress;
			_bppProgressFill.style.width = barMaxWidth * predictedProgressPercent;

			_claimButton.SetDisplay(_dataProvider.BattlePassDataProvider.IsRedeemable());
			
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

		private void SpawnInitSegments()
		{
			// Add filler to start of BP so it looks nicer
			SpawnScrollFiller();
			
			foreach (var segment in _segmentData)
			{
				var segmentInstance = _battlePassSegmentAsset.Instantiate();
				segmentInstance.AttachView(this, out BattlePassSegmentView view);
				view.InitWithData(segment);
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

		private void UpdateSegments()
		{
			for (int i = 0; i < _segmentViewsAndElements.Count; i++)
			{
				_segmentViewsAndElements[i].Key.InitWithData(_segmentData[i]);
			}
		}

		private void ScrollToBpLevel(int index)
		{
			// TODO TEST IF CORRECT REWARD IS SCROLLED
			var targetX = ((index + 1) * BpSegmentWidth) - (_rewardsScroll.contentRect.width / 2);

			DOVirtual.Float(0, 1f, _scrollToDuration, percent =>
			{
				var currentScroll = _rewardsScroll.scrollOffset;

				_rewardsScroll.scrollOffset = new Vector2(targetX * percent, currentScroll.y);
			}).SetEase(_scrollEaseMode);
		}

		private void SpawnScrollFiller()
		{
			var filler = new VisualElement {name = "background"};
			_rewardsScroll.Add(filler);
			filler.pickingMode = PickingMode.Ignore;
			filler.AddToClassList(UssBpSegmentFiller);
		}

		private void OnSegmentRewardClicked(BattlePassSegmentView view)
		{
		}
		
		private void OnBattlePassLevelUp(BattlePassLevelUpMessage message)
		{
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