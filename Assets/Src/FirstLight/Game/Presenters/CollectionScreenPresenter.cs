using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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
		[SerializeField] private Vector3 _collectionSpawnPosition;

		private static readonly int PAGE_SIZE = 3;

		public struct StateData
		{
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		private ListView _collectionList;

		private LocalizedLabel _comingSoonLabel;
		private Label _selectedItemLabel;
		private Label _selectedItemDescription;
		private Button _equipButton;
		private PriceButton _buyButton;
		private VisualElement _renderTexture;
		private VisualElement _categoriesRoot;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private int _selectedIndex;
		private CollectionCategory _selectedCategory;
		private GameObject _collectionObject;


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

			_collectionList.selectionType = SelectionType.Single;
			_collectionList.makeItem = MakeCollectionListItem;
			_collectionList.bindItem = BindCollectionListItem;
			_collectionList.ClearSelection();

			_renderTexture = root.Q<VisualElement>("RenderTexture");

			_comingSoonLabel = root.Q<LocalizedLabel>("ComingSoon").Required();
			_comingSoonLabel.visible = false;

			_selectedItemLabel = root.Q<Label>("ItemName").Required();
			_selectedItemDescription = root.Q<Label>("ItemDescription").Required();

			_equipButton = root.Q<Button>("EquipButton").Required();
			_equipButton.clicked += OnEquipClicked;

			_buyButton = root.Q<PriceButton>("BuyButton").Required();
			_buyButton.clicked += OnBuyClicked;
			_buyButton.visible = false;

			_categoriesRoot = root.Q<VisualElement>("CategoryHolder").Required();
			_categoriesRoot.Clear();
			SetupCategories();
			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			ViewOwnedItemsFromCategory(_selectedCategory);
			SelectEquipped(_selectedCategory);
			UpdateCollectionDetails(_selectedCategory);

			Update3DObject();
		}

		protected override async Task OnClosed()
		{
			base.OnClosed();

			if (_collectionObject != null)
			{
				Destroy(_collectionObject);
				_collectionObject = null;
			}
		}

		private void SetupCategories()
		{
			var categories = _gameDataProvider.CollectionDataProvider.GetCollectionsCategories();
			foreach (var category in categories)
			{
				var catElement = new CollectionCategoryElement();
				catElement.clicked += OnCategoryClicked;
				catElement.SetupCategoryButton(category);
				_categoriesRoot.Add(catElement);
			}

			OnCategoryClicked(categories.First());
		}

		private void OnCategoryClicked(CollectionCategory group)
		{
			if (_selectedCategory == group) return;

			_selectedCategory = group;

			foreach (var category in _categoriesRoot.Children().Cast<CollectionCategoryElement>())
			{
				category.SetSelected(category.Category == group);
			}

			var hasItems = GetViewCollection().Any();
			if (hasItems)
			{
				_comingSoonLabel.visible = false;
				_collectionList.visible = true;

				ViewOwnedItemsFromCategory(group);
				SelectEquipped(group);
				UpdateCollectionDetails(group);
				Update3DObject();
			}
			else
			{
				_comingSoonLabel.visible = true;
				_collectionList.visible = false;
			}

			_renderTexture.visible = hasItems;
			_equipButton.visible = hasItems;
			_selectedItemLabel.visible = hasItems;
			_selectedItemDescription.visible = hasItems;
		}

		private void SelectEquipped(CollectionCategory category)
		{
			var collection = GetViewCollection();
			var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
			var previousIndex = _selectedIndex;
			if (equipped.IsValid())
			{
				_selectedIndex = collection.IndexOf(equipped);
			}

			var row = _selectedIndex / PAGE_SIZE;
			var previousRow = previousIndex / PAGE_SIZE;
			_collectionList.RefreshItem(row);
			if (previousRow != row)
			{
				_collectionList.RefreshItem(previousRow);
			}
		}

		/// <summary>
		/// Update the data in this menu. Sometimes we may want to update data without opening the screen. 
		/// </summary>
		private void ViewOwnedItemsFromCategory(CollectionCategory category)
		{
			var collection = GetViewCollection();
			_selectedCategory = category;
			_collectionList.itemsSource = collection.ChunksOf(PAGE_SIZE).ToList();
			_collectionList.RefreshItems();
		}

		private CollectionItem GetSelectedItem()
		{
			return GetViewCollection()[_selectedIndex];
		}

		public List<CollectionItem> GetViewCollection()
		{
			return _gameDataProvider.CollectionDataProvider.GetOwnedCollection(_selectedCategory);
		}

		private void OnEquipClicked()
		{
			_services.CommandService.ExecuteCommand(new EquipCollectionItemCommand() {Item = GetSelectedItem()});
			UpdateCollectionDetails(_selectedCategory);
			SelectEquipped(_selectedCategory);
		}

		/// <summary>
		/// TODO: Enable players to buy new items here.
		/// </summary>
		private void OnBuyClicked()
		{
		}

		private async void Update3DObject()
		{
			var selectedItem = GetSelectedItem();
			if (!selectedItem.IsValid())
			{
				return;
			}

			if (_collectionObject != null)
			{
				// Unload requires the asset reference with we don't have here so we cant unload. :L
				// _services.AssetResolverService.UnloadAsset(_collectionObject);
				Destroy(_collectionObject);
				_collectionObject = null;
			}

			_collectionObject =
				await _services.AssetResolverService.RequestAsset<GameId, GameObject>(selectedItem.Id, true,
					true);
			_collectionObject.transform.SetPositionAndRotation(_collectionSpawnPosition, new Quaternion(0, 0, 0, 0));
		}

		/// Updated cost of Collection items / has it been equipped, etc. 
		private void UpdateCollectionDetails(CollectionCategory category)
		{
			var selectedId = GetSelectedItem().Id;
			var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
			// If an item is already equipped, show SELECTED instead of Equip
			_equipButton.text = equipped.IsValid() && selectedId == equipped.Id
				? ScriptLocalization.General.Selected.ToUpper()
				: ScriptLocalization.General.Equip;

			_selectedItemLabel.text = selectedId.GetLocalization();
			_selectedItemDescription.text = selectedId.GetDescriptionLocalization();
		}

		private VisualElement MakeCollectionListItem()
		{
			// maybe move this USS to a .uss sheet ?
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
			for (var i = 0; i < PAGE_SIZE; i++)
			{
				var card = new CollectionCardElement {name = "item-" + (i + 1)};
				card.clicked += OnCollectionItemSelected;
				row.Add(card);
			}

			return row;
		}


		private void BindCollectionListItem(VisualElement visualElement, int rowNumber)
		{
			var rowCards = visualElement.Children().Cast<CollectionCardElement>().ToArray();
			var rowItems = _collectionList.itemsSource[rowNumber] as IList<CollectionItem>;
			for (var x = 0; x < PAGE_SIZE; x++)
			{
				var card = rowCards[x];
				if (x >= rowItems.Count)
				{
					card.SetDisplay(false);
					continue;
				}

				var selectedItem = rowItems[x];
				var itemIndex = rowNumber * PAGE_SIZE + x;
				var category = _gameDataProvider.CollectionDataProvider.GetCollectionType(selectedItem);
				var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
				card.SetCollectionElement(selectedItem.Id, itemIndex,
					equipped.IsValid() && equipped.Equals(selectedItem));
				card.SetSelected(itemIndex == _selectedIndex);
			}
		}

		private void OnCollectionItemSelected(int newIndex)
		{
			var oldRow = _selectedIndex / PAGE_SIZE;
			var newRow = newIndex / PAGE_SIZE;
			_selectedIndex = newIndex;

			Update3DObject();
			UpdateCollectionDetails(_selectedCategory);

			if (oldRow != newRow)
			{
				_collectionList.RefreshItem(oldRow);
			}

			_collectionList.RefreshItem(newRow);
		}
	}
}