using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Views.GridViews;
using FirstLight.Game.Views.MainMenuViews;
using TMPro;
using UnityEngine;
using System.Linq;
using I2.Loc;
using FirstLight.Game.Messages;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles Equipment Fusion.
	/// </summary>
	public class FuseScreenPresenter : AnimatedUiPresenterData<FuseScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnCloseClicked;
			public Action<List<UniqueId>> OnFusionClicked;
		}

		[Header("Fuse Dialog / OSA")]
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private GenericGridView _gridView;
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private TextMeshProUGUI _noItemsCollectedText;

		[Header("Fuse Info Panel")]
		[SerializeField, Required] private TextMeshProUGUI _descriptionText;
		[SerializeField, Required] private GameObject _fuseButtonHolder;
		[SerializeField, Required] private Button _fuseButton;
		[SerializeField, Required] private TextMeshProUGUI _itemTitleText;
		[SerializeField, Required] private TextMeshProUGUI _fuseCostText;
		[SerializeField, Required] private TextMeshProUGUI _itemCraftedText;
		[SerializeField] private FuseChanceItemView[] _fuseItemChances;
		[SerializeField, Required] private Image _fuseChangeItemImage;
		[SerializeField, Required] private Sprite _fuseEmptySprite;
		
		[Header("Fusion Slots")]
		[SerializeField] private SlotEquipmentFillerView [] _fuseSlotViews;
		
		private IGameDataProvider _gameDataProvider;
		private List<UniqueId> _showNotifications;
		private GameIdGroup _sortGroup;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			Services = MainInstaller.Resolve<IGameServices>();
			_showNotifications = new List<UniqueId>();
			_sortGroup = GameIdGroup.Equipment;

			foreach (var slot in _fuseSlotViews)
			{
				slot.SetInfo(UniqueId.Invalid);
				slot.OnClick.AddListener(OnFuseSlotClicked);
			}
			
			_fuseButtonHolder.SetActive(false);
			_closeButton.onClick.AddListener(OnCloseClicked);
			_fuseButton.onClick.AddListener(OnFuseClicked);
			
			Services.MessageBrokerService.Subscribe<FuseCompletedMessage>(OnFuseCompletedMessage);
		}

		protected void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			_showNotifications = _gameDataProvider.UniqueIdDataProvider.NewIds.ToList();
			_itemCraftedText.text = "";

			foreach (var slot in _fuseSlotViews)
			{
				slot.SetInfo(UniqueId.Invalid);
			}
			
			foreach (var view in _fuseItemChances)
			{
				view.SetInfo(0);
			}
			
			UpdateFusionInformation();
			UpdateEquipmentMenu();
		}
		
		private void OnFuseCompletedMessage(FuseCompletedMessage message)
		{
			foreach (var slot in _fuseSlotViews)
			{
				slot.SetInfo(UniqueId.Invalid);
			}
			
			foreach (var view in _fuseItemChances)
			{
				view.SetInfo(0);
			}
			
			UpdateFusionInformation();
			UpdateEquipmentMenu();
		}

		private void OnEquipmentClicked(UniqueId itemClicked)
		{
			var freeSlot = (SlotEquipmentFillerView) null;
			
			foreach (var view in _fuseSlotViews)
			{
				if (view.ItemId == itemClicked)
				{
					view.SetInfo(UniqueId.Invalid);

					freeSlot = null;
					
					break;
				}
				if (!view.IsFilled && freeSlot == null)
				{
					freeSlot = view;
				}
			}

			if (freeSlot != null)
			{
				freeSlot.SetInfo(itemClicked);
			}

			UpdateFusionInformation();
			UpdateEquipmentMenu();
		}

		private void OnFuseSlotClicked(UniqueId slotClicked)
		{
			UpdateFusionInformation();
			UpdateEquipmentMenu();
		}

		private void OnFuseClicked()
		{
			var fusionList = GetFusionItemList();

			if (fusionList.Count < _fuseSlotViews.Length)
			{
				return;
			}

			var info = _gameDataProvider.EquipmentDataProvider.GetFusionInfo(fusionList);
			
			
			if (_gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.SC) < info.FusingCost)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = Services.GenericDialogService.CloseDialog
				};

				Services.GenericDialogService.OpenDialog(ScriptLocalization.General.NotEnoughCash, false, confirmButton);
				
				return;
			}

			Data.OnFusionClicked(fusionList);
		}

		private async void UpdateFusionInformation()
		{
			var fuseList = GetFusionItemList();
			
			_fuseButtonHolder.SetActive(fuseList.Count == _fuseSlotViews.Length);

			if (fuseList.Count == 0)
			{
				_fuseChangeItemImage.sprite = _fuseEmptySprite;
				_itemCraftedText.text = "";
				
				foreach (var view in _fuseItemChances)
				{
					view.SetInfo(0);
				}

				return;
			}
			
			var info = _gameDataProvider.EquipmentDataProvider.GetFusionInfo(fuseList);
				
			_fuseChangeItemImage.sprite = await Services.AssetResolverService.RequestAsset<ItemRarity, Sprite>(info.FusingResultRarity);
			_fuseCostText.text = info.FusingCost.ToString();
			_itemCraftedText.text = string.Format(ScriptLocalization.MainMenu.ItemCraftedText, 
	           GetRarityColor(info.FusingResultRarity), info.FusingResultRarity.ToString(), "<color=\"white\">");
			
			foreach (var view in _fuseItemChances)
			{
				view.SetInfo(info.ResultPercentages[view.GameIdGroup]);
			}
		}
		

		private void OnCloseClicked()
		{
			Data.OnCloseClicked();
		}

		private List<UniqueId> GetFusionItemList()
		{
			var fuseList = new List<UniqueId>();
			
			foreach (var slot in _fuseSlotViews)
			{
				if (slot.IsFilled)
				{
					fuseList.Add(slot.ItemId);
				}
			}

			return fuseList;
		}

		private void UpdateEquipmentMenu()
		{
			var dataProvider = _gameDataProvider.EquipmentDataProvider;
			var inventory = dataProvider.Inventory;
			var list = new List<EquipmentGridItemView.EquipmentGridItemData>(inventory.Count);
			var fuseList = GetFusionItemList();
			var itemRarity = fuseList.Count == 0 ? ItemRarity.TOTAL : dataProvider.GetEquipmentDataInfo(fuseList[0]).Data.Rarity;
			var isFullSelected = fuseList.Count > 0 && fuseList.Count == GameConstants.FUSION_SLOT_AMOUNT;
			
			for (var i = 0; i < inventory.Count; i++)
			{
				var gameId = _gameDataProvider.UniqueIdDataProvider.Ids[inventory[i].Id];

				if (!gameId.IsInGroup(_sortGroup))
				{
					continue;
				}
				
				var info = dataProvider.GetEquipmentInfo(inventory[i].Id);
				var isSelected = fuseList.Contains(info.DataInfo.Data.Id);
				
				var viewData = new EquipmentGridItemView.EquipmentGridItemData
				{
					Info = info,
					IsSelected = isSelected,
					PlayViewNotificationAnimation = _showNotifications.Contains(info.DataInfo.Data.Id),
					OnEquipmentClicked = OnEquipmentClicked,
					IsSelectable = isSelected || 
					               !isFullSelected && !info.IsEquipped && 
					               (fuseList.Count == 0 || itemRarity == info.DataInfo.Data.Rarity)
				};
					
				list.Add(viewData);
			}

			_noItemsCollectedText.enabled = list.Count == 0;
			
			list.Sort(new EquipmentSorter(EquipmentSorter.EquipmentSortState.Rarity));
			
			_gridView.UpdateData(list);
			_showNotifications.Clear();
		}

		private string GetRarityColor(ItemRarity rarity)
		{
			string colorString = "<color=\"red\">";

			switch (rarity)
			{
				case ItemRarity.Common: colorString = "<color=\"gray\">"; break;
				case ItemRarity.Uncommon: colorString = "<color=\"green\">"; break;
				case ItemRarity.Rare: colorString = "<color=#0099ff>"; break;
				case ItemRarity.Epic: colorString = "<color=\"red\">"; break;
				case ItemRarity.Legendary: colorString = "<color=\"yellow\">"; break;
			}

			return colorString;
		}
	}
}