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
			
			// Calling this initializes internal data and prepares the adapter to handle item count changes
			base.Start();
			
			// Retrieve the models from your data source and set the items count
			LoadBattlePassData(_services.ConfigsProvider.GetConfig<BattlePassConfig>());
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

		private void LoadBattlePassData(BattlePassConfig battlePassConfig)
		{
			var newItems = new BattlePassSegmentData[battlePassConfig.Levels.Count];

			for (uint i = 0; i < newItems.Length; ++i)
			{
				var model = new BattlePassSegmentData
				{
					Level = i,
					CurrentLevel = _gameDataProvider.BattlePassDataProvider.CurrentLevel.Value
				};
				
				newItems[i] = model;
			}
			
			OnDataRetrieved(newItems);
		}

		private void OnDataRetrieved(BattlePassSegmentData[] newItems)
		{
			Data.InsertItemsAtEnd(newItems);
		}
	}
}