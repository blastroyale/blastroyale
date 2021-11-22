using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles the Change Player Skin Menu.
	/// </summary>
	public class PlayerSkinScreenPresenter : AnimatedUiPresenterData<PlayerSkinScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnSkinSelected;
			public Action OnCloseClicked;
		}

		[SerializeField] private Button _closeButton;
		[SerializeField] private GenericGridView _gridView;
		[SerializeField] private TextMeshProUGUI _descriptionText;
		[SerializeField] private TextMeshProUGUI _itemTitleText;
		[SerializeField] private Image _avatarImage;
		[SerializeField] private Button _selectButton;
		[SerializeField] private Button _selectedButton;
		[SerializeField] private GameObject _selectedGameHolder;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private GameId _selectedId;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_closeButton.onClick.AddListener(Close);
			_selectButton.onClick.AddListener(OnSelectedPressed);
			
			_services.MessageBrokerService.Subscribe<PlayerSkinUpdatedMessage>(OnUpdatePlayerSkinMessage);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		// We override the OnClosed because we want to show the Loot menu before the close animation completes
		protected override void OnClosed()
		{
			base.OnClosed();
			Data.OnCloseClicked();
		}

		/// <summary>
		/// Called when this screen is Initialised and Opened.
		/// </summary>
		protected override async void OnOpened()
		{
			_selectedId = _gameDataProvider.PlayerDataProvider.CurrentSkin.Value;
			base.OnOpened();
			
			// Used to fix OSA order of execution issue.
			await Task.Yield(); 
			
			UpdatePlayerSkinMenu();
			UpdateSelectedButtonImage(_selectedId);
		}

		/// <summary>
		/// Update the data in this menu. Sometimes we may want to update data without opening the screen. 
		/// </summary>
		private async void UpdatePlayerSkinMenu()
		{
			var data = _services.ConfigsProvider.GetConfigsList<PlayerSkinConfig>();
			var list = new List<PlayerSkinGridItemView.PlayerSkinGridItemData>(data.Count);
			
			for (var i = 0; i < data.Count; i++)
			{
				var info = data[i];
				var viewData = new PlayerSkinGridItemView.PlayerSkinGridItemData
				{
					Config = info,
					IsSelected = info.Id == _selectedId,
					OnAvatarClicked = OnAvatarClicked
				};
				
				list.Add(viewData);
			}
			
			_gridView.UpdateData(list);
			_itemTitleText.text = _selectedId.GetTranslation();
			_avatarImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(_selectedId);
		}

		private void UpdateSelectedButtonImage(GameId selectedId)
		{
			var skinValue = _gameDataProvider.PlayerDataProvider.CurrentSkin.Value;
			
			_selectedGameHolder.SetActive(skinValue == selectedId);
			_selectButton.gameObject.SetActive(skinValue != selectedId);
			_selectedButton.gameObject.SetActive(skinValue == selectedId);
		}
		

		private void OnUpdatePlayerSkinMessage(PlayerSkinUpdatedMessage updatedMessage)
		{
			UpdatePlayerSkinMenu();
			UpdateSelectedButtonImage(updatedMessage.SkinId);
		}

		private void OnAvatarClicked(GameId skin)
		{
			_selectedId = skin;
			
			UpdatePlayerSkinMenu();
			UpdateSelectedButtonImage(skin);
		}

		private void OnSelectedPressed()
		{
			_services.CommandService.ExecuteCommand(new UpdatePlayerSkinCommand { SkinId = _selectedId });
		}
	}
}