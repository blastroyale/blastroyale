using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExitGames.Client.Photon.StructWrapping;
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
      [SerializeField] private Camera _renderTextureCamera;
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

      private IGameServices _services;
      private IGameDataProvider _gameDataProvider;
      
      private int _selectedIndex;
      private GameIdGroup _selectedCategory;
      private GameObject _collectionObject;

      private CollectionCategoryElement[] _collectionCategories;
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

         _categoryCharacters = root.Q<CollectionCategoryElement>("CategoryCharacters").Required();
         _categoryBanners = root.Q<CollectionCategoryElement>("CategoryBanners").Required();
         _categoryGliders = root.Q<CollectionCategoryElement>("CategoryGliders").Required();

         root.SetupClicks(_services);
      }

      protected override void OnOpened()
      {
         base.OnOpened();

         
         SetupCategories();
         
         
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
         _collectionCategories = new CollectionCategoryElement[3];

         _categoryCharacters.clicked += OnCategoryClicked;
         _categoryGliders.clicked += OnCategoryClicked;
         _categoryBanners.clicked += OnCategoryClicked;

         _selectedCategory = GameIdGroup.PlayerSkin;
         _categoryCharacters.SetSelected(true);

         _collectionCategories[0] = _categoryCharacters;
         _collectionCategories[1] = _categoryGliders;
         _collectionCategories[2] = _categoryBanners;
      }

      private void OnCategoryClicked(GameIdGroup group)
      {
         if (_selectedCategory == group) return;

         _selectedCategory = group;

         foreach (var category in _collectionCategories)
         {
            category.SetSelected(category.Category == group);
         }

         if (group == GameIdGroup.PlayerSkin)
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

         // TODO. When these categories show actual objects, they should no longer be hidden.
         _renderTexture.visible = _selectedCategory == GameIdGroup.PlayerSkin;
         _equipButton.visible = _selectedCategory == GameIdGroup.PlayerSkin;
         _selectedItemLabel.visible = _selectedCategory == GameIdGroup.PlayerSkin;
         _selectedItemDescription.visible = _selectedCategory == GameIdGroup.PlayerSkin;
      }

      private void SelectEquipped(GameIdGroup category)
      {
         var collection = GetViewCollection();
         var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
         var previousIndex = _selectedIndex;
         if (equipped != null)
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
      private async void ViewOwnedItemsFromCategory(GameIdGroup category)
      {
         var collection = GetViewCollection();
         var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
         _selectedCategory = category;
         _collectionList.itemsSource = new List<IList<CollectionItem>>();
         foreach (var page in collection.ChunksOf(PAGE_SIZE))
         {
            _collectionList.itemsSource.Add(page);
         }
         _collectionList.RefreshItems();
      }

      private CollectionItem GetSelectedItem()
      {
         return GetViewCollection()[_selectedIndex];
      }

      public List<CollectionItem> GetViewCollection()
      {
         // to be able to view all collection
         // return _gameDataProvider.CollectionDataProvider.GetFullCollection(_selectedCategory);
         return _gameDataProvider.CollectionDataProvider.GetOwnedCollection(_selectedCategory);
      }

      private void OnEquipClicked()
      {
         // HACK FOR DEMO
         (_gameDataProvider as GameLogic).CollectionLogic.Equip(_selectedCategory, GetSelectedItem());
         
         // Implement generic logic in CollectionLogic & make command that call that generic logic
         //_services.CommandService.ExecuteCommand(new UpdatePlayerSkinCommand {SkinId = GetSelectedItem().Id});
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
         _collectionObject =
            await _services.AssetResolverService.RequestAsset<GameId, GameObject>(selectedItem.Id, true,
               true);
         _collectionObject.transform.SetPositionAndRotation(_collectionSpawnPosition, new Quaternion(0, 0, 0, 0));
         
      }

      /// Updated cost of Collection items / has it been equipped, etc. 
      private void UpdateCollectionDetails(GameIdGroup category)
      {
         var selectedId = GetSelectedItem().Id;
         var equipped = _gameDataProvider.CollectionDataProvider.GetEquipped(category);
         // If an item is already equipped, show SELECTED instead of Equip
         _equipButton.text = equipped != null && selectedId == equipped.Id
            ? ScriptLocalization.General.Selected.ToUpper()
            : ScriptLocalization.General.Equip;
         
         _selectedItemLabel.text = selectedId.GetLocalization();
         _selectedItemDescription.text = selectedId.GetDescriptionLocalization();
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
         var cards = new CollectionCardElement[3];
         for (var i = 0; i < PAGE_SIZE; i++)
         {
            var card = new CollectionCardElement {name = "item-"+(i+1)};
            card.clicked += OnCollectionItemSelected;
            cards[i] = card;
            row.Add(card);
         }
         row.userData = cards;
         return row;
      }

      private void BindCollectionListItem(VisualElement visualElement, int rowNumber)
      {
         var rowCards = visualElement.userData as CollectionCardElement[];
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
            card.SetCollectionElement(selectedItem.Id, itemIndex, equipped != null && equipped.Equals(selectedItem));
            card.SetSelected(card.MenuGameId == selectedItem.Id);
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