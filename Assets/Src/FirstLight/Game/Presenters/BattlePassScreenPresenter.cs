using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
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

		[SerializeField] private VisualTreeAsset _battlePassSegmentAsset;
		
		private List<BattlePassSegmentData> _segmentData;
		private List<BattlePassSegmentView> _segmentViews;
		private ScrollView _rewardsScroll;
		private ScreenHeaderElement _screenHeader;

		private void Awake()
		{ 
			_segmentViews = new List<BattlePassSegmentView>();
			_segmentData = new List<BattlePassSegmentData>();
			_segmentData.Add(new BattlePassSegmentData());
			_segmentData.Add(new BattlePassSegmentData());
			_segmentData.Add(new BattlePassSegmentData());
			_segmentData.Add(new BattlePassSegmentData());
			_segmentData.Add(new BattlePassSegmentData());
			_segmentData.Add(new BattlePassSegmentData());
		}
		
		protected override void QueryElements(VisualElement root)
		{
			base.QueryElements(root);

			_rewardsScroll = root.Q<ScrollView>("RewardsScroll").Required();
			_screenHeader = root.Q<ScreenHeaderElement>("Header").Required();
			
			_screenHeader.SetTitle(string.Format(ScriptLocalization.UITBattlePass.season_number, "1"));
			_screenHeader.backClicked += Data.BackClicked;
			_screenHeader.backClicked += Data.BackClicked;

			SpawnAllSegments();
		}

		private void SpawnAllSegments()
		{
			foreach (var segment in _segmentData)
			{
				var segmentInstance = _battlePassSegmentAsset.Instantiate();
				segmentInstance.AttachView(this, out BattlePassSegmentView view);
				view.SetData(segment);
				view.Clicked += OnSegmentRewardClicked;
				_segmentViews.Add(view);
				_rewardsScroll.Add(segmentInstance);
			}
		}

		private void OnSegmentRewardClicked(BattlePassSegmentView view)
		{
			
		}
	}
	
	public struct BattlePassSegmentData
	{
	
	}
}