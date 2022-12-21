using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using Sirenix.OdinInspector;
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

		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private GenericGridView _gridView;
		[SerializeField, Required] private TextMeshProUGUI _descriptionText;
		[SerializeField, Required] private TextMeshProUGUI _itemTitleText;
		[SerializeField, Required] private Image _avatarImage;
		[SerializeField, Required] private Button _selectButton;
		[SerializeField, Required] private Button _selectedButton;
		[SerializeField, Required] private GameObject _selectedGameHolder;
		[SerializeField, Required] private Button _blockerButton;
		
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private GameId _selectedId;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_closeButton.onClick.AddListener(() => Data.OnCloseClicked());
			_selectButton.onClick.AddListener(OnSelectedPressed);
			
			_services.MessageBrokerService.Subscribe<PlayerSkinUpdatedMessage>(OnUpdatePlayerSkinMessage);
			_blockerButton.onClick.AddListener(OnBlockerButtonPressed);
		}

		private void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		// We override the OnClosed because we want to show the Loot menu before the close animation completes
		protected override async Task OnClosed()
		{
			await base.OnClosed();
			Data.OnCloseClicked();
		}

		/// <summary>
		/// Called when this screen is Initialised and Opened.
		/// </summary>
		protected override async void OnOpened()
		{
			_selectedId = _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin;
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
			var data = GameIdGroup.PlayerSkin.GetIds();
			var list = new List<PlayerSkinGridItemView.PlayerSkinGridItemData>(data.Count);
			
			foreach (var id in data)
			{
				var viewData = new PlayerSkinGridItemView.PlayerSkinGridItemData
				{
					Skin = id,
					IsSelected = id == _selectedId,
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
			var skinValue = _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin;
			
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
		
		private void OnBlockerButtonPressed()
		{
			Data.OnCloseClicked();
		}
	}
}