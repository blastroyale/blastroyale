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
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using TMPro;

namespace FirstLight.Game.Views.BattlePassViews
{
	// There are 2 important callbacks you need to implement, apart from Start(): CreateViewsHolder() and UpdateViewsHolder()
	// See explanations below
	public class BattlePassSegmentListView : OSA<BaseParamsWithPrefab, BattlePassSegmentView>
	{
		private IGameServices _services;
		
		// Helper that stores data and notifies the adapter when items count changes
		// Can be iterated and can also have its elements accessed by the [] operator
		public SimpleDataHelper<BattlePassSegmentView.DataModel> Data { get; private set; }

		protected override void Awake()
		{
			base.Awake();

			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void Start()
		{
			Data = new SimpleDataHelper<BattlePassSegmentView.DataModel>(this);

			// Calling this initializes internal data and prepares the adapter to handle item count changes
			base.Start();
			
			// Retrieve the models from your data source and set the items count
			RetrieveDataAndUpdate(_services.ConfigsProvider.GetConfig<BattlePassConfig>());
		}

		// This is called initially, as many times as needed to fill the viewport, 
		// and anytime the viewport's size grows, thus allowing more items to be displayed
		// Here you create the "ViewsHolder" instance whose views will be re-used
		// *For the method's full description check the base implementation
		protected override BattlePassSegmentView CreateViewsHolder(int itemIndex)
		{
			var instance = new BattlePassSegmentView();
			
			instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);

			return instance;
		}

		// This is called anytime a previously invisible item become visible, or after it's created, 
		// or when anything that requires a refresh happens
		// Here you bind the data from the model to the item's views
		// *For the method's full description check the base implementation
		protected override void UpdateViewsHolder(BattlePassSegmentView newOrRecycled)
		{
			BattlePassSegmentView.DataModel model = Data[newOrRecycled.ItemIndex];
	
			//newOrRecycled.RewardText.text = model.RewardName;
			//newOrRecycled.RewardImage.sprite = model.RewardSprite;
		}
		
		public void AddItemsAt(int index, IList<BattlePassSegmentView.DataModel> items)
		{
			Data.InsertItems(index, items);
		}

		public void RemoveItemsFrom(int index, int count)
		{
			Data.RemoveItems(index, count);
		}

		public void SetItems(IList<BattlePassSegmentView.DataModel> items)
		{
			Data.ResetItems(items);
		}

		private void RetrieveDataAndUpdate(BattlePassConfig battlePassConfig)
		{
			var newItems = new BattlePassSegmentView.DataModel[battlePassConfig.Levels.Count];

			for (int i = 0; i < newItems.Length; ++i)
			{
				var model = new BattlePassSegmentView.DataModel
				{
					RewardName = battlePassConfig.Levels[i].RewardId.ToString()
				};
				
				newItems[i] = model;
			}
			
			OnDataRetrieved(newItems);
		}

		private void OnDataRetrieved(BattlePassSegmentView.DataModel[] newItems)
		{
			Data.InsertItemsAtEnd(newItems);
		}
	}
}