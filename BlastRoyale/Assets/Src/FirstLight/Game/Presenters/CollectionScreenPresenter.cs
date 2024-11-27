using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Commands;
using FirstLight.Game.Data;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Data.DataTypes.Helpers;
using FirstLight.Game.Domains.Flags.View;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.MonoComponent.Collections;
using FirstLight.Game.MonoComponent.MainMenu;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.UITK;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using SRF;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Collection Screen, where players can equip skins, death markers, gliders, etc.
	/// </summary>
	public class CollectionScreenPresenter : UIPresenterData<CollectionScreenPresenter.StateData>
	{
		[SerializeField] private Vector3 _collectionSpawnPosition;
		[SerializeField] private Vector3 _gliderSpawnPosition;
		[SerializeField] private Vector3 _collectionSpawnRotation;
		[SerializeField] private Vector3 _gliderSpawnRotation;
		[SerializeField] private Vector3 _deathMarkerSpawnRotation;

		private static readonly int PAGE_SIZE = 3;

		public class StateData
		{
			public Action OnHomeClicked;
			public Action OnBackClicked;
		}

		private ListView _collectionList;
		private Dictionary<CollectionCategory, CollectionCategoryElement> _catElements = new ();
		private LocalizedLabel _comingSoonLabel;
		private Label _selectedItemLabel;
		private Label _selectedItemDescription;
		private LocalizedButton _equipButton;
		private PriceButton _buyButton;
		private ImageButton _infoButton;
		private VisualElement _nameLockedIcon;
		private VisualElement _renderTexture;
		private VisualElement _categoriesRoot;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private int _selectedIndex;
		private int _equippedIndex;
		private CollectionCategory _selectedCategory;
		private GameObject _collectionObject;
		private GameObject _anchorObject;
		private readonly List<UniqueId> _seenItems = new ();
		private const float ITEM_ROTATE_SPEED = 40f;
		private float _degreesToRotate = 0f;
		private bool _isRotate = true;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements()
		{
			_collectionList = Root.Q<ListView>("CollectionList").Required();
			_collectionList.DisableScrollbars();

			var header = Root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked = Data.OnBackClicked;

			Root.Q<CurrencyDisplayElement>("CSCurrency").SetDisplay(false);
			Root.Q<CurrencyDisplayElement>("CoinCurrency").AttachView(this, out CurrencyDisplayView _);

			_collectionList.selectionType = SelectionType.Single;
			_collectionList.makeItem = MakeCollectionListItem;
			_collectionList.bindItem = BindCollectionListItem;
			_collectionList.ClearSelection();

			_renderTexture = Root.Q<VisualElement>("RenderTexture");

			_comingSoonLabel = Root.Q<LocalizedLabel>("ComingSoon").Required();
			_comingSoonLabel.visible = false;

			_selectedItemLabel = Root.Q<Label>("ItemName").Required();
			_selectedItemDescription = Root.Q<Label>("ItemDescription").Required();
			_nameLockedIcon = Root.Q<VisualElement>("ItemNameLocked").Required();
			_infoButton = Root.Q<ImageButton>("InfoButton").Required();
			_infoButton.clicked += OnInfoClicked;
			_equipButton = Root.Q<LocalizedButton>("EquipButton").Required();
			_equipButton.clicked += OnEquipClicked;

			_buyButton = Root.Q<PriceButton>("BuyButton").Required();
			_buyButton.clicked += OnBuyClicked;
			_buyButton.visible = false;

			_categoriesRoot = Root.Q<VisualElement>("CategoryHolder").Required();
			_categoriesRoot.Clear();
			SetupCategories();
			Root.SetupClicks(_services);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_degreesToRotate = 0f;

			ViewOwnedItemsFromCategory(_selectedCategory);
			SelectEquipped(_selectedCategory);
			UpdateCollectionDetails(_selectedCategory);

			Update3DObject().Forget();

			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			if (_seenItems.Count > 0)
			{
				_services.CommandService.ExecuteCommand(new MarkEquipmentSeenCommand {Ids = _seenItems});
				_seenItems.Clear();
			}

			if (_collectionObject != null)
			{
				Destroy(_collectionObject);
				_collectionObject = null;
			}

			if (_anchorObject != null)
			{
				Destroy(_anchorObject);
				_anchorObject = null;
			}

			return base.OnScreenClose();
		}

		private void SetupCategories()
		{
			var categories = _gameDataProvider.CollectionDataProvider.GetCollectionsCategories();
			var unseenCollectionItems = _services.RewardService.UnseenItems(ItemMetadataType.Collection);
			foreach (var category in categories)
			{
				var catElement = new CollectionCategoryElement();
				catElement.clicked += OnCategoryClicked;
				catElement.SetupCategoryButton(category);
				_catElements[category] = catElement;
				var unseenOfCategory = unseenCollectionItems.Any(i => _gameDataProvider.CollectionDataProvider.GetCollectionType(i) == category);
				catElement.SetNotification(unseenOfCategory);
				_categoriesRoot.Add(catElement);
			}

			OnCategoryClicked(categories.First());
		}

		private void OnCategoryClicked(CollectionCategory group)
		{
			_isRotate = true;
			if (_selectedCategory == group) return;

			_degreesToRotate = 0f;
			_collectionList.ScrollToItem(0);
			_selectedCategory = group;

			var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(_selectedCategory);

			if (equipped == null)
			{
				_selectedIndex = 0;
			}

			foreach (var category in _categoriesRoot.Children().Cast<CollectionCategoryElement>())
			{
				category.SetSelected(category.Category == group);
			}

			var hasItems = GetCollectionAll().Any();
			// Set visible to true after loading 3d object
			_renderTexture.visible = false;
			if (hasItems)
			{
				_comingSoonLabel.visible = false;
				_collectionList.visible = true;
				_catElements[group].SetNotification(false);
				ViewOwnedItemsFromCategory(group);
				SelectEquipped(group);
				UpdateCollectionDetails(group);
				Update3DObject().Forget();
			}
			else
			{
				_comingSoonLabel.visible = true;
				_collectionList.visible = false;
			}

			_equipButton.visible = hasItems;
			_selectedItemLabel.visible = hasItems;
			_selectedItemDescription.visible = hasItems;
		}

		private void SelectEquipped(CollectionCategory category)
		{
			var collection = GetCollectionAll();
			var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
			var equippedIndex = _equippedIndex;
			if (equipped != null)
			{
				_selectedIndex = collection.IndexOf(equipped);
				_equippedIndex = _selectedIndex;
			}

			var row = _selectedIndex / PAGE_SIZE;
			var previousEquippedRow = equippedIndex / PAGE_SIZE;
			_collectionList.RefreshItem(row);

			if (previousEquippedRow != row)
			{
				_collectionList.RefreshItem(previousEquippedRow);
			}
		}

		/// <summary>
		/// Update the data in this menu. Sometimes we may want to update data without opening the screen. 
		/// </summary>
		private void ViewOwnedItemsFromCategory(CollectionCategory category)
		{
			var collection = GetCollectionAll();
			_selectedCategory = category;
			_collectionList.itemsSource = collection.ChunksOf(PAGE_SIZE).ToList();
			_collectionList.Clear();
			_collectionList.RefreshItems();
		}

		private ItemData GetSelectedItem()
		{
			var l = GetCollectionAll();
			return l.Count == 0 ? null : l[_selectedIndex];
		}

		public List<ItemData> GetCollectionAll()
		{
			HashSet<int> hiddenGameIds = new HashSet<int>();
			// TODO: Move this to configs
			if (_services.GameAppService.AppData.TryGetValue("HIDE_COLLECTION", out var hidden) && hidden != null)
			{
				hiddenGameIds = hidden.Split(",").Select(Int32.Parse).ToHashSet();
			}

			var allVisibleItems = _gameDataProvider.CollectionDataProvider.GetFullCollection(_selectedCategory)
				.Where(c => !hiddenGameIds.Contains((int) c.Id));

			var playerItems = _gameDataProvider.CollectionDataProvider.GetOwnedCollection(_selectedCategory);

			var result = new List<ItemData>(playerItems);
			foreach (var item in allVisibleItems)
			{
				if (result.All(playerOwned => playerOwned.Id != item.Id))
				{
					result.Add(item);
				}
			}

			return result.Where(c => c.Id.IsInGroup(GameIdGroup.Collection)).ToList();
		}

		private void OnEquipClicked()
		{
			var item = GetSelectedItem();
			_services.CommandService.ExecuteCommand(new EquipCollectionItemCommand() {Item = item});
			UpdateCollectionDetails(_selectedCategory);
			SelectEquipped(_selectedCategory);
		}

		/// <summary>
		/// TODO: Enable players to buy new items here.
		/// </summary>
		private void OnBuyClicked()
		{
		}

		private async UniTaskVoid Update3DObject()
		{
			var selectedItem = GetSelectedItem();
			if (selectedItem == null)
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

			_collectionObject = await _services.CollectionService.LoadCollectionItem3DModel(selectedItem, true, true);
			if (_collectionObject == null) return;
			if (_anchorObject != null)
			{
				Destroy(_anchorObject);
				_anchorObject = null;
			}

			_anchorObject = new GameObject("Collection 3D Object Anchor");
			_anchorObject.transform.position = _selectedCategory.Id == GameIdGroup.Glider ? _gliderSpawnPosition : _collectionSpawnPosition;
			_collectionObject.transform.SetParent(_anchorObject.transform);
			_collectionObject.transform.ResetLocal();
			_renderTexture.visible = true;

			if (_selectedCategory.Id == GameIdGroup.Glider)
			{
				_degreesToRotate = _gliderSpawnRotation.y;
				_anchorObject.transform.localRotation = Quaternion.Euler(_gliderSpawnRotation);
			}
			else if (_selectedCategory.Id == GameIdGroup.DeathMarker)
			{
				_isRotate = false;
				_degreesToRotate = _deathMarkerSpawnRotation.y;
				_collectionObject.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
				_collectionObject.transform.localPosition = new Vector3(0.4f, 0, 0.15f);
				_collectionObject.transform.localEulerAngles = new Vector3(0, 180f, 0);
				var view = _collectionObject.GetComponent<DeathFlagView>();
				view.TriggerFlag();
			}
			else if (_selectedCategory.Id == GameIdGroup.PlayerSkin)
			{
				_collectionObject.AddComponent<MainMenuCharacterViewComponent>().PlayEnterAnimation = false;
			}
			else
			{
				_degreesToRotate = _collectionSpawnRotation.y;
				_anchorObject.transform.localRotation = Quaternion.Euler(_collectionSpawnRotation);
			}
		}

		void Update()
		{
			if (!_anchorObject)
			{
				return;
			}

			if (_selectedCategory.Id == GameIdGroup.PlayerSkin) return; // No rotation for characters

			if (!_isRotate) return;
			_degreesToRotate += (ITEM_ROTATE_SPEED * Time.deltaTime);

			if (_degreesToRotate > 360f)
			{
				_degreesToRotate = 0f;
			}

			var eulerAngles = _anchorObject.transform.localEulerAngles;
			_anchorObject.transform.localRotation = Quaternion.Euler(eulerAngles.x, _degreesToRotate, eulerAngles.z);
		}

		/// Updated cost of Collection items / has it been equipped, etc. 
		private void UpdateCollectionDetails(CollectionCategory category)
		{
			var selectedItem = GetSelectedItem();
			if (selectedItem == null)
			{
				return;
			}

			var selectedId = selectedItem.Id;
			var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
			// If an item is already equipped, show SELECTED instead of Equip
			_equipButton.text = equipped != null && selectedId == equipped.Id
				? ScriptLocalization.General.Selected.ToUpper()
				: ScriptLocalization.General.Equip;

			_selectedItemLabel.text = selectedItem.GetDisplayName();
			_selectedItemDescription.text = selectedId.GetDescriptionLocalization();

			var buffs = _gameDataProvider.BuffsLogic.GetMetaBuffs(selectedItem);
			var owned = _gameDataProvider.CollectionDataProvider.IsItemOwned(GetSelectedItem());
			_nameLockedIcon.SetDisplay(!owned);
			_equipButton.SetDisplay(owned);
			_infoButton.SetDisplay(buffs.Count > 0 && selectedItem.IsNft());
		}

		private void OnInfoClicked()
		{
			var selectedItem = GetSelectedItem();
			if (selectedItem == null)
			{
				return;
			}

			var buffs = _gameDataProvider.BuffsLogic.GetMetaBuffs(selectedItem);
			if (buffs.Count > 0)
			{
				var tooltip = "When Owned: \n\n";
				tooltip += string.Join("\n", buffs.SelectMany(buff => buff.Modifiers)
					.Select(mod => $"+{mod.Value.AsInt}% {_services.BuffService.GetDisplayString(mod.Stat)}"));
				_infoButton.OpenTooltip(Root, tooltip);
				FLog.Verbose("Item has buffs ! " + tooltip);
			}
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
				card.Clicked += OnCollectionItemSelected;
				row.Add(card);
			}

			return row;
		}

		private void BindCollectionListItem(VisualElement visualElement, int rowNumber)
		{
			var rowCards = visualElement.Children().Cast<CollectionCardElement>().ToArray();
			var rowItems = _collectionList.itemsSource[rowNumber] as IList<ItemData>;
			for (var x = 0; x < PAGE_SIZE; x++)
			{
				var card = rowCards[x];

				if (x >= rowItems.Count)
				{
					card.visible = false;
					foreach (var c in card.Children())
					{
						c.visible = false;
					}

					continue;
				}

				card.visible = true;
				foreach (var c in card.Children())
				{
					c.visible = true;
				}

				var selectedItem = rowItems[x];

				var itemIndex = rowNumber * PAGE_SIZE + x;
				var collectionDataProvider = _gameDataProvider.CollectionDataProvider;
				var category = collectionDataProvider.GetCollectionType(selectedItem);
				var equipped = collectionDataProvider.GetEquipped(category);
				var owned = collectionDataProvider.IsItemOwned(selectedItem);
				var unseenItems = _services.RewardService.UnseenItems(ItemMetadataType.Collection);
				var isUnseenItem = unseenItems.Contains(selectedItem);
				card.SetCollectionElement(selectedItem, selectedItem.GetDisplayName(), itemIndex);
				card.SetIsOwned(owned);
				card.SetIsEquipped(equipped != null && equipped.Equals(selectedItem));
				card.SetSelected(itemIndex == _selectedIndex);
				card.SetIsNft(selectedItem.IsNft());
				card.SetNotificationPip(isUnseenItem);
			}
		}

		private void OnCollectionItemSelected(int newIndex)
		{
			var oldRow = _selectedIndex / PAGE_SIZE;
			var newRow = newIndex / PAGE_SIZE;
			_selectedIndex = newIndex;

			Update3DObject().Forget();
			UpdateCollectionDetails(_selectedCategory);

			if (oldRow != newRow)
			{
				_collectionList.RefreshItem(oldRow);
			}

			_collectionList.RefreshItem(newRow);
		}

		private bool IsItemSeen(UniqueId item)
		{
			return _seenItems.Contains(item) || !_gameDataProvider.UniqueIdDataProvider.NewIds.Contains(item);
		}
	}
}