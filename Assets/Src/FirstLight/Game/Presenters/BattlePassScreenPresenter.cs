using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using DG.Tweening;
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
		}

		private const string USS_SEGMENT_FILLER = "bp-segment-filler";
		private const float BP_SEGMENT_WIDTH = 475f;

		[SerializeField] private VisualTreeAsset _battlePassSegmentAsset;
		[SerializeField] private DG.Tweening.Ease _scrollEaseMode;
		[SerializeField] private float _scrollToDuration;
		
		private ScrollView _rewardsScroll;
		private VisualElement _bppProgressBackground;
		private VisualElement _bppProgressFill;
		private Label _bppProgressLabel;
		private Label _currentLevelLabel;
		private Label _nextLevelLabel;
		private ScreenHeaderElement _screenHeader;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private List<BattlePassSegmentData> _segmentData;
		private List<BattlePassSegmentView> _segmentViews;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_segmentViews = new List<BattlePassSegmentView>();
			_segmentData = new List<BattlePassSegmentData>();
		}

		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_rewardsScroll = root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = root.Q<ScreenHeaderElement>("Header").Required();
			_currentLevelLabel = root.Q<Label>("CurrentLevelValue").Required();
			_nextLevelLabel = root.Q<Label>("NextLevelValue").Required();
			_bppProgressLabel = root.Q<Label>("BppProgressLabel").Required();
			_bppProgressBackground = root.Q("BppBackground").Required();
			_bppProgressFill = root.Q("BppProgress").Required();
			
			_screenHeader.SetTitle(string.Format(ScriptLocalization.UITBattlePass.season_number, "1"));
			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;

			InitScreen();
		}

		private void InitScreen()
		{
			var battlePassConfig = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var predictedProgress = _gameDataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			var currentLevel = _gameDataProvider.BattlePassDataProvider.CurrentLevel.Value;
			var currentProgress = _gameDataProvider.BattlePassDataProvider.CurrentPoints.Value;
			
			var predictedMaxProgress = _gameDataProvider.BattlePassDataProvider.GetRequiredPointsForLevel((int)predictedProgress.Item1);
			_bppProgressLabel.text = predictedProgress.Item2 + "/" + predictedMaxProgress;
			_currentLevelLabel.text = (predictedProgress.Item1 + 1).ToString();
			_nextLevelLabel.text = (predictedProgress.Item1 + 2).ToString();
			
			for (int i = 0; i < battlePassConfig.Levels.Count; ++i)
			{
				var data = new BattlePassSegmentData
				{
					SegmentLevel = (uint) i,
					CurrentLevel = currentLevel,
					CurrentProgress = currentProgress,
					PredictedCurrentLevel = predictedProgress.Item1,
					PredictedCurrentProgress = predictedProgress.Item2,
					MaxProgress = _gameDataProvider.BattlePassDataProvider.GetRequiredPointsForLevel(i),
					MaxLevel = _gameDataProvider.BattlePassDataProvider.MaxLevel,
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId]
				};

				_segmentData.Add(data);
			}

			// Add filler to start and end of BP so it looks nicer on the ends
			AddFillerToBp();

			// Add level 1 of battle pass, for aesthetics/UX
			var segment1 = _battlePassSegmentAsset.Instantiate();
			segment1.AttachView(this, out BattlePassSegmentView viewOne);
			viewOne.InitMinimalWithData(0, predictedProgress.Item1, predictedProgress.Item2,
				_gameDataProvider.BattlePassDataProvider.MaxLevel);
			_segmentViews.Add(viewOne);
			_rewardsScroll.Add(segment1);

			foreach (var segment in _segmentData)
			{
				var segmentInstance = _battlePassSegmentAsset.Instantiate();
				segmentInstance.AttachView(this, out BattlePassSegmentView view);
				view.InitWithData(segment);
				view.Clicked += OnSegmentRewardClicked;
				_segmentViews.Add(view);
				_rewardsScroll.Add(segmentInstance);
			}

			AddFillerToBp();

			// SmoothScrollTo(10);
		}

		private void SmoothScrollTo(int index)
		{
			// TODO TEST IF CORRECT REWARD IS SCROLLED
			var targetX = ((index + 1) * BP_SEGMENT_WIDTH) - (_rewardsScroll.contentRect.width / 2);

			DOVirtual.Float(0, 1f, _scrollToDuration, percent =>
			{
				var currentScroll = _rewardsScroll.scrollOffset;

				_rewardsScroll.scrollOffset = new Vector2(targetX * percent, currentScroll.y);
			}).SetEase(_scrollEaseMode);
		}

		private void AddFillerToBp()
		{
			var filler = new VisualElement {name = "background"};
			_rewardsScroll.Add(filler);
			filler.pickingMode = PickingMode.Ignore;
			filler.AddToClassList(USS_SEGMENT_FILLER);
		}

		private void OnSegmentRewardClicked(BattlePassSegmentView view)
		{
		}
	}
}