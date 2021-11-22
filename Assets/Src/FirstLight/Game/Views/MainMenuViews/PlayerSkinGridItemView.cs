using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using Quantum;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script controls a specific Equipment Item held within the List of Items in teh Equipment screen.
	/// Tapping on an item brings up it's information.
	/// </summary>
	public class PlayerSkinGridItemView : GridItemBase<PlayerSkinGridItemView.PlayerSkinGridItemData>
	{
		public struct PlayerSkinGridItemData
		{
			public PlayerSkinConfig Config;
			public bool IsSelected;
			public Action<GameId> OnAvatarClicked;
		}
		
		[SerializeField] private TextMeshProUGUI Text;
		[SerializeField] private Image IconImage;
		[SerializeField] private Button Button;
		[SerializeField] private Image SelectedImage;
		[SerializeField] private Image _frameImage;
		[SerializeField] private Color _regularColor;
		[SerializeField] private Color _selectedColor;
		[SerializeField] private GameObject _selectedFrameImage;
		
		private PlayerSkinGridItemData _data;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			Button.onClick.AddListener(OnButtonClick);
		}

		protected override async void OnUpdateItem(PlayerSkinGridItemData data)
		{
			_frameImage.color = data.IsSelected ? _selectedColor : _regularColor;

			_data = data;
			Text.text = data.Config.Id.GetTranslation();

			SelectedImage.enabled = _gameDataProvider.PlayerDataProvider.CurrentSkin.Value == _data.Config.Id;
			_selectedFrameImage.SetActive(data.IsSelected);
			IconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(_data.Config.Id);
		}

		private void OnButtonClick()
		{
			Data.OnAvatarClicked(_data.Config.Id);
		}
	}
}

