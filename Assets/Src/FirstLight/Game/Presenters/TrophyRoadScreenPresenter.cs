using System;
using System.Collections;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using TMPro;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using FirstLight.Services;
using I2.Loc;
using FirstLight.Game.Messages;
using FirstLight.Game.Commands;
using Quantum;
using Button = UnityEngine.UI.Button;
using FirstLight.Game.Configs.AssetConfigs;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles the Equipment / Loot Menu.
	/// </summary>
	public class TrophyRoadScreenPresenter : AnimatedUiPresenterData<TrophyRoadScreenPresenter.StateData>
	{
		private const float _scrollDuration = 2f;
		
		public struct StateData
		{
			public Action OnTrophyRoadClosedClicked;
		}

		[Header("OSA")]
		[SerializeField] private Button _closeButton;
		[SerializeField] private GenericGridView _gridView;
		[SerializeField] private TextMeshProUGUI _titleText;

		private IGameDataProvider _gameDataProvider;

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			Services.MessageBrokerService.Subscribe<TrophyRoadRewardCollectedMessage>(OnTrophyRoadRewardCollectedMessage);
			_closeButton.onClick.AddListener(OnBackButtonPressed);
		}

		protected override async void OnOpened()
		{
			base.OnOpened();
			
			// Used to fix OSA order of execution issue.
			await Task.Yield(); 
			
			UpdateMenu();			
			
			var info = _gameDataProvider.TrophyRoadDataProvider.GetAllInfos();
			
			foreach (var entry in info)
			{
				if (!entry.IsCollected && _gameDataProvider.PlayerDataProvider.Level.Value > 1)
				{
					// By default scroll to the next available reward to collect.
					_gridView.ScrollTo((int) entry.Level - 1);

					return;
				}
			}
		}
		
		
		protected void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}
		
		protected override void OnClosedCompleted()
		{
			_gridView.ScrollTo(0);
		}
		
		/// <summary>
		/// Rebuild Item if this reward is collected.
		/// </summary>
		private void OnTrophyRoadRewardCollectedMessage(TrophyRoadRewardCollectedMessage message)
		{
			UpdateMenu();
		}

		private void UpdateMenu()
		{
			var info = _gameDataProvider.TrophyRoadDataProvider.GetAllInfos();
			var list = new List<TrophyRoadGridItemView.TrophyRoadGridItemData>(info.Count);
			
			foreach (var entry in info)
			{
				var viewData = new TrophyRoadGridItemView.TrophyRoadGridItemData { Info = entry };

				list.Add(viewData);
			}
			
			_gridView.UpdateData(list);
		}
		
		private void OnBackButtonPressed()
		{
			Data.OnTrophyRoadClosedClicked();
		}
	}
}