using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Commands;
using FirstLight.Game.Infos;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Ids;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Collection Screen, where players can equip skins, death markers, gliders, etc.
	/// </summary>
	[LoadSynchronously]
	public class CollectionScreenPresenter : UiToolkitPresenterData<CollectionScreenPresenter.StateData>
	{
		private const string COMING_SOON_LOC_KEY = "UITCollectionScreen/comingsoon";
		
		public struct StateData
		{
			public Action<GameIdGroup> OnSlotButtonClicked;
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		private ListView _collectionList;
		private List<CollectionListRow> _collectionListRows;
		private Dictionary<GameId, int> _itemRowMap;
		private Label _comingSoonLabel;
		private Label _selectedItemLabel;
		private Label _selectedItemDescription;
		private Button _equipButton;
		private PriceButton _buyButton;
		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		
		private GameId _selectedId;

		private CollectionCategoryElement [] _collectionCategories;
		private CollectionCategoryElement _categoryCharacters;
		private CollectionCategoryElement _categoryBanners;
		private CollectionCategoryElement _categoryGliders;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_collectionList = root.Q<ListView>("CollectionList").Required();
			_collectionList.DisableScrollbars();
			
			var header = root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked += Data.OnBackClicked;
			header.homeClicked += Data.OnHomeClicked;
			
			_collectionList.makeItem = MakeCollectionListItem;
			_collectionList.bindItem = BindCollectionListItem;

			root.SetupClicks(_services);
			
			_comingSoonLabel = root.Q<Label>("ComingSoon").Required();
			_comingSoonLabel.text = COMING_SOON_LOC_KEY.LocalizeKey();
			_comingSoonLabel.visible = false; // TO DO: Show Coming soon for other categories.
			
			_selectedItemLabel = root.Q<Label>("ItemName").Required();
			_selectedItemDescription = root.Q<Label>("ItemDescription").Required();
			
			_equipButton = root.Q<Button>("EquipButton").Required();
			_equipButton.clicked += OnEquipClicked;
			
			_buyButton = root.Q<PriceButton>("BuyButton").Required();
			_buyButton.clicked += OnBuyClicked;
			_buyButton.visible = false;

			_categoryCharacters = root.Q<CollectionCategoryElement>("CollectionCategoryElementCharacters").Required();
			_categoryBanners = root.Q<CollectionCategoryElement>("CollectionCategoryElementBanners").Required();
			_categoryGliders = root.Q<CollectionCategoryElement>("CollectionCategoryElementGliders").Required();
			
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			CreateCategories();
			UpdatePlayerSkinMenu();
			UpdateCollectionDetails();
		}

		private void CreateCategories()
		{
			/*
			_collectionCategories = new CollectionCategoryElement[3];
			_collectionCategories[0] = new CollectionCategoryElement();
			_collectionCategories[0].Category = GameIdGroup.PlayerSkin;
			_collectionCategories[1] = new CollectionCategoryElement();
			_collectionCategories[1].Category = GameIdGroup.Banner;
			_collectionCategories[2] = new CollectionCategoryElement();
			_collectionCategories[2].Category = GameIdGroup.Glider;
			*/
			
			_categoryCharacters.Category = GameIdGroup.PlayerSkin;
			_categoryCharacters.clicked += OnCategoryClicked;
			
			_categoryGliders.Category = GameIdGroup.Glider;
			_categoryGliders.clicked += OnCategoryClicked;
			
			_categoryBanners.Category = GameIdGroup.Banner;
			_categoryBanners.clicked += OnCategoryClicked;
		}

		private void OnCategoryClicked(GameIdGroup group)
		{
			Debug.Log("Category Clicked: " + group);
		}

		private void UpdateCategories()
		{
			
		}

		/// <summary>
		/// Update the data in this menu. Sometimes we may want to update data without opening the screen. 
		/// </summary>
		private async void UpdatePlayerSkinMenu()
		{
			var data = GameIdGroup.PlayerSkin.GetIds();
			var listCount = data.Count;
			
			_selectedId = _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin;
			_collectionListRows = new List<CollectionListRow>(listCount / 3);
			_itemRowMap = new Dictionary<GameId, int>(listCount/ 3);

			for (var i = 0; i < listCount; i += 3)
			{
				var item1 = data[i];
				
				if (i + 1 >= data.Count)
				{
					_collectionListRows.Add(
						new CollectionListRow(new CollectionListRow.Item(item1), null, null));
					_itemRowMap[item1] = _collectionListRows.Count - 1;
				}
				else
				{
					var item2 = data[i + 1];
					var item3 = data[i + 2];
					
					_collectionListRows.Add(new CollectionListRow(
						new CollectionListRow.Item(item1),
						new CollectionListRow.Item(item2),
						new CollectionListRow.Item(item3)
						));
					_itemRowMap[item1] = _collectionListRows.Count - 1;
					_itemRowMap[item2] = _collectionListRows.Count - 1;
					_itemRowMap[item3] = _collectionListRows.Count - 1;
				}
			}
			
			_collectionList.itemsSource = _collectionListRows;
			_collectionList.RefreshItems();
		}
		
		private void OnCollectionItemClicked(GameId id)
		{
			if (id == _selectedId) return;

			var previousItem = _selectedId;

			_selectedId = id;
			UpdateCollectionDetails();

			_collectionList.RefreshItem(_itemRowMap[previousItem]);
			_collectionList.RefreshItem(_itemRowMap[_selectedId]);
		}

		private void OnEquipClicked()
		{
			_services.CommandService.ExecuteCommand(new UpdatePlayerSkinCommand { SkinId = _selectedId });
			UpdateCollectionDetails();
			UpdatePlayerSkinMenu();
		}

		private void OnBuyClicked()
		{
			
		}


		/// Updated cost of Collection items / has it been equipped, etc. 
		private void UpdateCollectionDetails()
		{
			// If an item is already equipped, show SELECTED instead of Equip
			_equipButton.text = _selectedId == _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin ? ScriptLocalization.General.Selected.ToUpper() : ScriptLocalization.General.Equip;

			_selectedItemLabel.text = _selectedId.GetLocalization();
			_selectedItemDescription.text = _selectedId.GetDescriptionLocalization();
		}
		

		private VisualElement MakeCollectionListItem()
		{
			var row = new VisualElement
			{
				style =
				{
					flexDirection = FlexDirection.Row,
					flexGrow = 1f,
					justifyContent = Justify.SpaceBetween,
					alignItems = Align.Center,
					paddingLeft = new Length(50, LengthUnit.Pixel),
					paddingRight = new Length(50, LengthUnit.Pixel)
				}
			};

			var item1 = new CollectionCardElement {name = "item-1"};
			var item2 = new CollectionCardElement {name = "item-2"};
			var item3 = new CollectionCardElement {name = "item-3"};

			item1.clicked += OnCollectionItemClicked;
			item2.clicked += OnCollectionItemClicked;
			item3.clicked += OnCollectionItemClicked;

			row.Add(item1);
			row.Add(item2);
			row.Add(item3);

			return row;
		}
		
		private void BindCollectionListItem(VisualElement visualElement, int index)
		{
			var row = _collectionListRows[index];

			var card1 = visualElement.Q<CollectionCardElement>("item-1");
			var card2 = visualElement.Q<CollectionCardElement>("item-2");
			var card3 = visualElement.Q<CollectionCardElement>("item-3");
			
			var currentSkin = _gameDataProvider.PlayerDataProvider.PlayerInfo.Skin;
			
			card1.SetCollectionElement(row.Item1.GameId, row.Item1.GameId == currentSkin);
			card2.SetDisplay(false);
			card3.SetDisplay(false);
			
			if (row.Item2 != null)
			{
				card2.SetDisplay(true);
				card2.SetCollectionElement(row.Item2.GameId, row.Item2.GameId == currentSkin);
			}
			if (row.Item3 != null)
			{
				card3.SetDisplay(true);
				card3.SetCollectionElement(row.Item3.GameId, row.Item3.GameId == currentSkin);
			}

			card1.SetSelected(card1.MenuGameId == _selectedId);
			card2.SetSelected(card2.MenuGameId == _selectedId);
			card3.SetSelected(card3.MenuGameId == _selectedId);
		}
		
		private class CollectionListRow
		{
			public Item Item1 { get; } 
			public Item Item2 { get; }
			public Item Item3 { get; }

			public CollectionListRow(Item item1, Item item2, Item item3)
			{
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
			}

			internal class Item
			{
				public GameId GameId { get; }

				public Item(GameId gameId)
				{
					GameId = gameId;
				}
			}
		}
	}
}