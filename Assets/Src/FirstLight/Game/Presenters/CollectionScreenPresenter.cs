using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Collection Screen, where players can equip skins, death markers, gliders, etc.
	/// </summary>
	[LoadSynchronously]
	public class CollectionScreenPresenter : UiToolkitPresenterData<CollectionScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<GameIdGroup> OnSlotButtonClicked;
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		private List<EquipmentSlotElement> _categories;
		private List<CollectionMenuSlotElement> _collectionCategories;
		private MightElement _might;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		private GameId _selectedId;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_categories = root.Query<EquipmentSlotElement>().Build().ToList();

			foreach (var cat in _categories)
			{
				cat.clicked += () => Data.OnSlotButtonClicked(cat.Category);
			}

			var header = root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked += Data.OnBackClicked;
			header.homeClicked += Data.OnHomeClicked;

			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			UpdatePlayerSkinMenu();
			
			RefreshCategories();
			RefreshSpecials();

			_services.MessageBrokerService.Publish(new EquipmentScreenOpenedMessage());
		}
		
		/// <summary>
		/// Update the data in this menu. Sometimes we may want to update data without opening the screen. 
		/// </summary>
		private async void UpdatePlayerSkinMenu()
		{
			var data = GameIdGroup.PlayerSkin.GetIds();
			
			/*
			var items = _gameDataProvider.EquipmentDataProvider.Inventory.ReadOnlyDictionary
				.Where(kvp => kvp.Value.GameId.IsInGroup(Data.EquipmentSlot))
				.ToList();
			*/

			/*
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
			*/
			// _gridView.UpdateData(list);
			// _itemTitleText.text = _selectedId.GetLocalization();
			// _avatarImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(_selectedId);
		}
		
		/*
		protected override async void OnUpdateItem(PlayerSkinGridItemData data)
		{
			_frameImage.color = data.IsSelected ? _selectedColor : _regularColor;

			_data = data;
			Text.text = data.Skin.GetLocalization();

			SelectedImage.enabled = _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin == _data.Skin;
			_selectedFrameImage.SetActive(data.IsSelected);
			IconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(_data.Skin);
		}
		*/

		private void RefreshCategories()
		{
			foreach (var element in _categories)
			{
				var unseenItems = _gameDataProvider.EquipmentDataProvider.Inventory
					.Any(pair => pair.Value.GameId.IsInGroup(element.Category) &&
						_gameDataProvider.UniqueIdDataProvider.NewIds.Contains(pair.Key));

				if (_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(element.Category, out var uniqueId))
				{
					var info = _gameDataProvider.EquipmentDataProvider.GetInfo(uniqueId);

					element.SetEquipment(info, false, unseenItems);
				}
				else
				{
					element.SetEquipment(default, false, unseenItems);
				}
			}
		}

		private void RefreshSpecials()
		{

		}

		private class CollectionListRow
		{
			public Item Item1 { get; }
			public Item Item2 { get; }

			public CollectionListRow(Item item1, Item item2)
			{
				Item1 = item1;
				Item2 = item2;
			}

			internal class Item
			{
				public UniqueId UniqueId { get; }
				public Equipment Equipment { get; }

				public Item(UniqueId uniqueId, Equipment equipment)
				{
					UniqueId = uniqueId;
					Equipment = equipment;
				}
			}
		}
	}
}