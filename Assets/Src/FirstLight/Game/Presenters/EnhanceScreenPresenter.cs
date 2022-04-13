using System;
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
using FirstLight.Game.Infos;
using I2.Loc;
using FirstLight.Game.Messages;
using Quantum;
using Button = UnityEngine.UI.Button;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles Equipment Enhancement.
	/// </summary>
	public class EnhanceScreenPresenter : AnimatedUiPresenterData<EnhanceScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnCloseClicked;
			public Action<List<UniqueId>> OnEnhancedClicked;
		}

		[Header("Enhance Dialog / OSA")]
		[SerializeField] private Button _closeButton;
		[SerializeField] private GenericGridView _gridView;
		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private TextMeshProUGUI _noItemsCollectedText;
		[SerializeField] private FilterEquipmentView[] _filterButtons;
		
		[Header("Enhance Info Panel")]
		[SerializeField] private TextMeshProUGUI _descriptionText;
		[SerializeField] private GameObject _enhanceButtonHolder;
		[SerializeField] private Button _enhanceButton;
		[SerializeField] private TextMeshProUGUI _itemsNeededText;
		[SerializeField] private TextMeshProUGUI _fuseCostText;
		[SerializeField] private TextMeshProUGUI _itemCraftedText;
		[SerializeField] private TextMeshProUGUI _questionMarkText;
		[SerializeField] private TextMeshProUGUI _questionMarkResultText;
		[SerializeField] private Image _enhanceCardImage;
		[SerializeField] private Image _enhanceCardResultImage;
		[SerializeField] private Image _enhancedItemImage;
		[SerializeField] private Image _enhancedResultImage;
		[SerializeField] private Sprite _fuseEmptySprite;
		
		[Header("Enhance Slots")]
		[SerializeField] private SlotEquipmentFillerView [] _slotViews;
		
		private IGameDataProvider _gameDataProvider;
		private List<UniqueId> _showNotifications;
		private GameIdGroup _sortGroup;

		private void Start()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_showNotifications = new List<UniqueId>();
			_sortGroup = GameIdGroup.Equipment;

			foreach (var button in _filterButtons)
			{
				button.OnClick.AddListener(OnSortSlotClicked);
			}
			
			foreach (var slot in _slotViews)
			{
				slot.OnClick.AddListener(OnSlotFillerClicked);
			}
			
			_enhanceButtonHolder.SetActive(false);
			_closeButton.onClick.AddListener(OnCloseClicked);
			_enhanceButton.onClick.AddListener(OnEnhanceClicked);
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();
			
			Services.MessageBrokerService.Subscribe<EnhanceCompletedMessage>(OnEnhanceCompletedMessage);
		}

		protected void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			foreach (var view in _slotViews)
			{
				view.SetInfo(UniqueId.Invalid);
			}
			
			_showNotifications = _gameDataProvider.UniqueIdDataProvider.NewIds.ToList();
			
			SetFilterButtonsState();
			UpdateEnhanceInformation();
			UpdateEquipmentMenu();
		}

		private void OnCloseClicked()
		{
			Data.OnCloseClicked();
		}
		
		private void OnEnhanceCompletedMessage(EnhanceCompletedMessage message)
		{
			foreach (var slot in _slotViews)
			{
				slot.SetInfo(UniqueId.Invalid);
			}

			UpdateEnhanceInformation();
			UpdateEquipmentMenu();
		}

		private void OnEquipmentClicked(UniqueId itemClicked)
		{
			var freeSlot = (SlotEquipmentFillerView) null;
			
			foreach (var view in _slotViews)
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

			UpdateEnhanceInformation();
			UpdateEquipmentMenu();
		}

		private void OnSlotFillerClicked(UniqueId slotClicked)
		{
			UpdateEnhanceInformation();
			UpdateEquipmentMenu();
		}
		
		private void OnSortSlotClicked(GameIdGroup slot)
		{
			_sortGroup = slot;
			
			SetFilterButtonsState();
			UpdateEquipmentMenu();
		}

		private void SetFilterButtonsState()
		{
			foreach (var button in _filterButtons)
			{
				button.SetSelectedState(_sortGroup);
			}
		}

		private void OnEnhanceClicked()
		{
			var enhanceList = GetEnhanceItemList();
			var info = _gameDataProvider.EquipmentDataProvider.GetEnhancementInfo(enhanceList);

			if (enhanceList.Count != info.EnhancementItemRequiredAmount)
			{
				return;
			}
			
			if (_gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.SC) < info.EnhancementCost)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = Services.GenericDialogService.CloseDialog
				};

				Services.GenericDialogService.OpenDialog(ScriptLocalization.General.NotEnoughCash, false, confirmButton);
				
				return;
			}

			Data.OnEnhancedClicked(enhanceList);
		}

		private async void UpdateEnhanceInformation()
		{
			var enhanceList = GetEnhanceItemList();

			if (enhanceList.Count == 0)
			{
				_enhanceCardImage.sprite = _fuseEmptySprite;
				_enhanceCardResultImage.sprite = _fuseEmptySprite;
				_itemCraftedText.text = "";
				_itemsNeededText.text = "";
				_questionMarkText.enabled = true;
				_enhancedItemImage.enabled = false;
				_enhancedResultImage.enabled = false;
				_questionMarkResultText.enabled = true;

				for (var i = 1; i < _slotViews.Length; i++)
				{
					_slotViews[i].gameObject.SetActive(false);
				}
				
				_slotViews[_slotViews.Length - 1].transform.parent.gameObject.SetActive(false);
				_enhanceButtonHolder.SetActive(false);
				
				return;
			}
			
			var info = _gameDataProvider.EquipmentDataProvider.GetEnhancementInfo(enhanceList);
			var dataInfo = _gameDataProvider.EquipmentDataProvider.GetEquipmentDataInfo(enhanceList[0]);
			var rarityResult = info.EnhancementResult.Data.Rarity;
			
			_itemsNeededText.text = string.Format(ScriptLocalization.MainMenu.EnhanceItemsNeeded, info.EnhancementItemRequiredAmount.ToString());
			_enhanceCardImage.sprite = await Services.AssetResolverService.RequestAsset<ItemRarity, Sprite>(dataInfo.Data.Rarity);
			_enhanceCardResultImage.sprite = await Services.AssetResolverService.RequestAsset<ItemRarity, Sprite>(rarityResult);
			_enhancedItemImage.sprite = await Services.AssetResolverService.RequestAsset<GameId, Sprite>(dataInfo.GameId);
			_enhancedItemImage.enabled = true;
			_questionMarkText.enabled = false;
			_questionMarkResultText.enabled = false;
			_enhancedResultImage.enabled = true;
			_enhancedResultImage.sprite = _enhancedItemImage.sprite;
			_fuseCostText.text = info.EnhancementCost.ToString();
			_itemCraftedText.text = string.Format(ScriptLocalization.MainMenu.ItemEnhancedText,
			                                      GetRarityColor(rarityResult), rarityResult.ToString(),
			                                      "<color=\"white\">", dataInfo.GameId.GetTranslation());


			for (var i = 1; i < _slotViews.Length; i++)
			{
				_slotViews[i].gameObject.SetActive(i < info.EnhancementItemRequiredAmount);
			}
			
			_slotViews[_slotViews.Length - 1].transform.parent.gameObject.SetActive(info.EnhancementItemRequiredAmount > 3);
			_enhanceButtonHolder.SetActive(enhanceList.Count == info.EnhancementItemRequiredAmount);
		}

		private void UpdateEquipmentMenu()
		{
			var dataProvider = _gameDataProvider.EquipmentDataProvider;
			var inventory = _gameDataProvider.EquipmentDataProvider.Inventory;
			var list = new List<EquipmentGridItemView.EquipmentGridItemData>(inventory.Count);
			var enhanceList = GetEnhanceItemList();
			var dataInfo = enhanceList.Count == 0 ? new EquipmentDataInfo() : dataProvider.GetEquipmentDataInfo(enhanceList[0]);
			var isFullSelected = enhanceList.Count > 0 && enhanceList.Count == dataProvider.GetEnhancementInfo(enhanceList).EnhancementItemRequiredAmount;
			
			for (var i = 0; i < inventory.Count; i++)
			{
				var gameId = _gameDataProvider.UniqueIdDataProvider.Ids[inventory[i].Id];

				if (!gameId.IsInGroup(_sortGroup))
				{
					continue;
				}
				
				var info = dataProvider.GetEquipmentInfo(inventory[i].Id);
				var isSelected = enhanceList.Contains(info.DataInfo.Data.Id);
				
				var viewData = new EquipmentGridItemView.EquipmentGridItemData
				{
					Info = info,
					IsSelected = isSelected,
					PlayViewNotificationAnimation = _showNotifications.Contains(info.DataInfo.Data.Id),
					OnEquipmentClicked = OnEquipmentClicked,
					IsSelectable = isSelected || enhanceList.Count == 0 ||
					               !isFullSelected && dataInfo.Data.Rarity == info.DataInfo.Data.Rarity && dataInfo.GameId == gameId
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
			var colorString = "<color=\"red\">";

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

		private List<UniqueId> GetEnhanceItemList()
		{
			var enhanceList = new List<UniqueId>();
			
			foreach (var slot in _slotViews)
			{
				if (slot.IsFilled)
				{
					enhanceList.Add(slot.ItemId);
				}
			}

			return enhanceList;
		}
	}
}