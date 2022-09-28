using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using frame8.Logic.Misc.Other.Extensions;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;

namespace FirstLight.Game.Views.BattlePassViews
{
	/// <summary>
	/// This class is an OSA implementation of a view holder. It handles spawning and controlling battle pass segment
	/// view holders.
	/// </summary>
	public class BattlePassSegmentListView : OSA<BaseParamsWithPrefab, BattlePassSegmentViewHolder>
	{
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		public SimpleDataHelper<BattlePassSegmentData> Data { get; private set; }
		
		protected override void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			Data = new SimpleDataHelper<BattlePassSegmentData>(this);
			
			// (OSA) Calling this initializes internal data and prepares the adapter to handle item count changes
			base.Start();
			
			LoadBattlePassData();
		}

		/// <summary>
		/// Updates all BP segments with the most recent BP data
		/// </summary>
		public void UpdateAllSegments()
		{
			var battlePassConfig = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<BattlePassRewardConfig>();
			var redeemedProgress = _gameDataProvider.BattlePassDataProvider.GetLevelAndPointsIfReedemed();

			for (int i = 0; i < Data.List.Count; i++)
			{
				Data.List[i].SegmentLevel = (uint) i;
				Data.List[i].CurrentLevel = _gameDataProvider.BattlePassDataProvider.CurrentLevel.Value;
				Data.List[i].CurrentProgress = _gameDataProvider.BattlePassDataProvider.CurrentPoints.Value;
				Data.List[i].RedeemableLevel = redeemedProgress.Item1;
				Data.List[i].RedeemableProgress = redeemedProgress.Item2;
				Data.List[i].MaxProgress = battlePassConfig.PointsPerLevel;
				Data.List[i].RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId];
				
				var viewsHolder = GetItemViewsHolderIfVisible(i);
				if (viewsHolder != null)
				{
					viewsHolder.View.Init(Data.List[i]);
				}
			}

			ScrollToBattlePassLevel();
		}

		/// <summary>
		/// Scrolls OSA to current redeemable BP level
		/// </summary>
		public void ScrollToBattlePassLevel()
		{
			var index = Math.Clamp((int) _gameDataProvider.BattlePassDataProvider.GetLevelAndPointsIfReedemed().Item1, 0, Data.Count - 1);
			SmoothScrollTo(index, 0.3f, 0, -1f);
		}

		protected override BattlePassSegmentViewHolder CreateViewsHolder(int itemIndex)
		{
			var instance = new BattlePassSegmentViewHolder();
			
			instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);

			return instance;
		}

		protected override void UpdateViewsHolder(BattlePassSegmentViewHolder newOrRecycled)
		{
			BattlePassSegmentData model = Data[newOrRecycled.ItemIndex];
			newOrRecycled.View.Init(model);
		}

		private void LoadBattlePassData()
		{
			var battlePassConfig = _services.ConfigsProvider.GetConfig<BattlePassConfig>();
			var rewardConfig = _services.ConfigsProvider.GetConfigsList<BattlePassRewardConfig>();
			var newSegments = new BattlePassSegmentData[battlePassConfig.Levels.Count];
			var redeemedProgress = _gameDataProvider.BattlePassDataProvider.GetLevelAndPointsIfReedemed();
			
			for (int i = 0; i < newSegments.Length; ++i)
			{
				var model = new BattlePassSegmentData
				{
					SegmentLevel = (uint)i,
					CurrentLevel = _gameDataProvider.BattlePassDataProvider.CurrentLevel.Value,
					CurrentProgress = _gameDataProvider.BattlePassDataProvider.CurrentPoints.Value,
					RedeemableLevel = redeemedProgress.Item1,
					RedeemableProgress = redeemedProgress.Item2,
					MaxProgress = battlePassConfig.PointsPerLevel,
					RewardConfig = rewardConfig[battlePassConfig.Levels[i].RewardId]
				};
				
				newSegments[i] = model;
			}
			
			OnDataRetrieved(newSegments);
		}

		private void OnDataRetrieved(BattlePassSegmentData[] newItems)
		{
			Data.InsertItemsAtEnd(newItems);
		}
	}
}