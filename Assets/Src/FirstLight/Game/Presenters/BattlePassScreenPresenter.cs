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
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		private List<BattlePassSegmentData> _segmentData;
		private List<BattlePassSegmentView> _segmentViews;
		private ScrollView _rewardsScroll;
		private ScreenHeaderElement _screenHeader;

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
			
			_screenHeader.SetTitle(string.Format(ScriptLocalization.UITBattlePass.season_number, "1"));
			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.homeClicked += Data.BackClicked;

			SpawnAllSegments();
		}

		private void SpawnAllSegments()
		{
			var battlePassConfig = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<EquipmentRewardConfig>();
			var redeemedProgress = _gameDataProvider.BattlePassDataProvider.GetPredictedLevelAndPoints();
			
			for (int i = 0; i < battlePassConfig.Levels.Count; ++i)
			{
				var data = new BattlePassSegmentData
				{
					SegmentLevel = (uint) i,
					CurrentLevel = _gameDataProvider.BattlePassDataProvider.CurrentLevel.Value,
					CurrentProgress = _gameDataProvider.BattlePassDataProvider.CurrentPoints.Value,
					PredictedCurrentLevel = redeemedProgress.Item1,
					PredictedCurrentProgress = redeemedProgress.Item2,
					MaxProgress = _gameDataProvider.BattlePassDataProvider.GetRequiredPointsForLevel(i),
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId]
				};
				
				_segmentData.Add(data);
			}
			
			// Add filler to start and end of BP so it looks nicer on the ends
			AddFillerToBp();
			foreach (var segment in _segmentData)
			{
				var segmentInstance = _battlePassSegmentAsset.Instantiate();
				segmentInstance.AttachView(this, out BattlePassSegmentView view);
				view.SetData(segment);
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
			var targetX = ((index + 1) * BP_SEGMENT_WIDTH) - (_rewardsScroll.contentRect.width/2);

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