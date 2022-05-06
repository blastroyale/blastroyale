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
using System.Threading.Tasks;
using FirstLight.Game.Infos;
using FirstLight.Services;
using I2.Loc;
using FirstLight.Game.Messages;
using FirstLight.Game.Commands;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;
using UnityEngine.UI;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This View handles the Equipment / Loot Menu.
	/// </summary>
	public class EquipmentScreenPresenter : AnimatedUiPresenterData<EquipmentScreenPresenter.StateData>
	{
		public struct StateData
		{
			public GameIdGroup EquipmentSlot;
			public Action OnCloseClicked;
		}

		[Header("Equipment Dialog / OSA")]
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private GenericGridView _gridView;
		[SerializeField, Required] private TextMeshProUGUI _screenTitleText;
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private TextMeshProUGUI _noItemsCollectedText;
		
		[Header("Equipment Info Panel")]
		[SerializeField, Required] private TextMeshProUGUI _descriptionText;
		[SerializeField, Required] private Button _equipUnequipButton;
		[SerializeField, Required] private Button _sellButton;
		[SerializeField, Required] private Button _upgradeButton;
		[SerializeField, Required] private EquipmentStatInfoView _statInfoViewPoolRef;
		[SerializeField, Required] private EquipmentStatSpecialInfoView _specialStatInfoViewPoolRef;
		[SerializeField, Required] private TextMeshProUGUI _itemTitleText;
		[SerializeField, Required] private TextMeshProUGUI _itemLevelText;
		[SerializeField, Required] private GameObject _actionButtonHolder;
		[SerializeField, Required] private GameObject _equippedStatusObject;
		[SerializeField, Required] private GameObject _itemLevelObject;
		[SerializeField, Required] private GameObject _upgradeCostHolder;
		[SerializeField, Required] private TextMeshProUGUI _equipButtonText;
		[SerializeField, Required] private TextMeshProUGUI _upgradeCostText;
		[SerializeField, Required] private TextMeshProUGUI _sellCostText;
		[SerializeField, Required] private Animation _itemLevelHolderAnimation;

		[SerializeField, Required] private Image _upgradeCoinImage;
		[SerializeField, Required] private Image _upgradeButtonImage;
		[SerializeField, Required] private Sprite _maxUpgradeButtonSprite;
		[SerializeField, Required] private Sprite _upgradeButtonSprite;

		[SerializeField, Required] private GameObject _rarityHolder;
		[SerializeField, Required] private Image _weaponTypeImage;
		[SerializeField, Required] private Button _weaponTypeButton;
		[SerializeField, Required] private Button _movieButton;
		[SerializeField] private Image [] _rarityImage;
		[SerializeField, Required] private TextMeshProUGUI _rarityText;
		[SerializeField, Required] private TextMeshProUGUI _weaponTypeText;
		[SerializeField, Required] private TextMeshProUGUI _powerRatingText;
		[SerializeField, Required] private TextMeshProUGUI _powerChangeText;
		[SerializeField, Required] private Animation _powerChangeAnimation;
		
		[SerializeField] private Color _autoFireColor;
		[SerializeField] private Color _manualFireColor;
		
		private IMainMenuServices _mainMenuServices;
		private IGameDataProvider _gameDataProvider;
		private EquipmentSorter.EquipmentSortState _equipmentSortState;
		private IObjectPool<EquipmentStatInfoView> _statInfoViewPool;
		private IObjectPool<EquipmentStatSpecialInfoView> _statSpecialInfoViewPool;
		private UniqueId _uniqueId = UniqueId.Invalid;
		private List<UniqueId> _showNotifications;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_statInfoViewPool = new GameObjectPool<EquipmentStatInfoView>(4, _statInfoViewPoolRef);
			_statSpecialInfoViewPool = new GameObjectPool<EquipmentStatSpecialInfoView>(1, _specialStatInfoViewPoolRef);
			_showNotifications = new List<UniqueId>();
			
			_closeButton.onClick.AddListener(Close);
			_equipUnequipButton.onClick.AddListener(OnEquipButtonClicked);
			_sellButton.onClick.AddListener(OnSellClicked);
			_upgradeButton.onClick.AddListener(OnUpgradeClicked);
			_statInfoViewPoolRef.gameObject.SetActive(false);
			_specialStatInfoViewPoolRef.gameObject.SetActive(false);
		}

		protected void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnInitialized()
		{
			base.OnInitialized();

			Services.MessageBrokerService.Subscribe<ItemUnequippedMessage>(OnItemUnequippedMessage);
			Services.MessageBrokerService.Subscribe<ItemEquippedMessage>(OnItemEquippedMessage);
			Services.MessageBrokerService.Subscribe<ItemSoldMessage>(OnItemSoldMessage);
			Services.MessageBrokerService.Subscribe<ItemUpgradedMessage>(OnItemUpgradedMessage);
		}

		// We override the OnClosed because we want to show the Loot menu before the close animation completes
		protected override void OnClosed()
		{
			_showNotifications.Clear();
			
			base.OnClosed();
			Data.OnCloseClicked();
		}

		protected override async void OnOpened()
		{
			base.OnOpened();
			
			_gameDataProvider.EquipmentDataProvider.EquippedItems.TryGetValue(Data.EquipmentSlot, out var id);

			_equipmentSortState = EquipmentSorter.EquipmentSortState.Rarity;
			_uniqueId = id;
			_showNotifications = _gameDataProvider.UniqueIdDataProvider.NewIds.ToList();
			_powerChangeText.enabled = false;

			// Used to fix OSA order of execution issue.
			await Task.Yield(); 
			
			UpdateEquipmentMenu();
			SetStats();
		}

		private void ShowStatsForEmptySlot()
		{
			_screenTitleText.text = Data.EquipmentSlot.GetTranslation();
			_itemTitleText.text = ScriptLocalization.General.SlotEmpty;
			_descriptionText.text = ScriptLocalization.General.CollectItemsFromCrates;
				
			_rarityHolder.SetActive(false);
			_weaponTypeButton.gameObject.SetActive(false);
			_movieButton.gameObject.SetActive(false);
			_itemLevelObject.SetActive(false);
			_actionButtonHolder.SetActive(false);
			_powerRatingText.text = "";
		}
		
		private void SetStats()
		{
			var equipmentProvider = _gameDataProvider.EquipmentDataProvider;
			var itemEquipped = equipmentProvider.IsEquipped(_uniqueId);
			
			_statInfoViewPool?.DespawnAll();
			_statSpecialInfoViewPool?.DespawnAll();
			_equippedStatusObject.SetActive(itemEquipped);

			if (_uniqueId == UniqueId.Invalid)
			{
				ShowStatsForEmptySlot();
				return;
			}
			
			var info = equipmentProvider.GetEquipmentInfo(_uniqueId);
			
			// Don't show Default/Melee weapon
			if (info.IsWeapon && info.Stats[EquipmentStatType.MaxCapacity] < 0)
			{
				ShowStatsForEmptySlot();
				return;
			}
			
			var maxLevelInfo = equipmentProvider.GetEquipmentInfo(info.DataInfo.GameId, info.DataInfo.Data.Rarity, info.DataInfo.Data.Adjective, info.DataInfo.Data.Material, info.DataInfo.Data.Manufacturer, info.DataInfo.Data.Faction, info.MaxLevel, info.DataInfo.Data.Grade);
			var descriptionID = info.DataInfo.GameId.GetTranslationTerm() + GameConstants.DESCRIPTION_POSTFIX;
			var isWeapon = info.DataInfo.GameId.IsInGroup(GameIdGroup.Weapon);

			SetStatInfoData(info, maxLevelInfo);
			SetEquipButtonStatus();

			_powerRatingText.text = string.Format(ScriptLocalization.MainMenu.PowerRating, info.ItemPower.ToString());
			_itemTitleText.text = info.DataInfo.GameId.GetTranslation();
			_screenTitleText.text = info.DataInfo.GameId.GetSlot().GetTranslation();
			_descriptionText.text = LocalizationManager.GetTranslation(descriptionID);
			_sellCostText.text = $"+ {info.SellCost.ToString()}";
			_rarityText.text = info.DataInfo.Data.Rarity.ToString();
			_itemLevelText.text = info.DataInfo.Data.Level == info.MaxLevel ? $"{ScriptLocalization.General.MaxLevel}" 
				                      : $"{ScriptLocalization.General.Level} {info.DataInfo.Data.Level.ToString()}";

			_upgradeCostHolder.SetActive(!info.IsMaxLevel);
			
			if (info.DataInfo.Data.Level < info.MaxLevel)
			{
				_upgradeButtonImage.sprite = _upgradeButtonSprite;
				_upgradeCostText.text = info.UpgradeCost.ToString();
				_upgradeCoinImage.enabled = true;
			}
			else
			{
				_upgradeButtonImage.sprite = _maxUpgradeButtonSprite;
				_upgradeCostText.text = "";
				_upgradeCoinImage.enabled = false;
			}
			

			for (int i = 0; i < _rarityImage.Length; i++)
			{
				_rarityImage[i].enabled = i == (int) info.DataInfo.Data.Rarity;
			}
			
			_movieButton.gameObject.SetActive(isWeapon);
			_weaponTypeButton.gameObject.SetActive(isWeapon);
			_rarityHolder.SetActive(true);
			_itemLevelObject.SetActive(true);
			_actionButtonHolder.SetActive(true);
		}
		
		private void OnUpgradeCompleted()
		{
			var previousPower = _gameDataProvider.EquipmentDataProvider.GetTotalEquippedItemPower();
			
			Services.CommandService.ExecuteCommand(new UpgradeItemCommand { ItemId = _uniqueId });
			
			ShowPowerChange((int) previousPower);
		}

		private void OnItemUpgradedMessage(ItemUpgradedMessage message)
		{
			UpdateEquipmentMenu();
			SetStats();
			_itemLevelHolderAnimation.Play();
		}
		
		private void OnUpgradeClicked()
		{
			var info = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(_uniqueId);
			
			if (info.IsMaxLevel)
			{
				_mainMenuServices.UiVfxService.PlayFloatingText(ScriptLocalization.MainMenu.WeaponIsAtMaxLevel);
				
				return;
			}
			
			if (info.UpgradeCost <= _gameDataProvider.CurrencyDataProvider.GetCurrencyAmount(GameId.SC))
			{
				var priceString = string.Format(ScriptLocalization.General.UpgradeFor, info.UpgradeCost.ToString());
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.Yes,
					ButtonOnClick = OnUpgradeCompleted
				};

				Services.GenericDialogService.OpenDialog(priceString, true, confirmButton);
			}
			else
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = Services.GenericDialogService.CloseDialog
				};

				Services.GenericDialogService.OpenDialog(ScriptLocalization.General.NotEnoughCash, false, confirmButton);
			}
		}

		private void SetStatInfoData(EquipmentInfo selectedInfo, EquipmentInfo maxLevelInfo)
		{
			var selectedStats = selectedInfo.Stats.ToList();
			
			// If we selected a different weapon to the one equipped, then we want to compare them.
			if (_gameDataProvider.EquipmentDataProvider.EquippedItems.TryGetValue(Data.EquipmentSlot, out var equippedId) && equippedId != _uniqueId )
			{
				var equippedInfo = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(equippedId);
				var equippedStats = equippedInfo.Stats.ToList();

				for (var i = 0; i < selectedStats.Count; i++)
				{
					var statType = selectedStats[i].Key;
					var format = statType == EquipmentStatType.ReloadSpeed ? "N1" : "N0";
					var statsBeautifier = statType == EquipmentStatType.Speed ? GameConstants.MOVEMENT_SPEED_BEAUTIFIER : 1f;
					var selectedValue = selectedStats[i].Value * statsBeautifier;
					var equippedValue = equippedStats[i].Value * statsBeautifier;
					var statText = selectedValue.ToString(format);
					var delta = statType == EquipmentStatType.ReloadSpeed ? selectedValue - equippedValue : Mathf.RoundToInt(selectedValue) - Mathf.RoundToInt(equippedValue);

					if (selectedValue > 0 && (statType == EquipmentStatType.SpecialId0 || statType == EquipmentStatType.SpecialId1))
					{
						GetSpecialIconInfo(selectedStats[i].Key, _statSpecialInfoViewPool.Spawn(), (GameId) selectedValue); 
					}
					else if (statType != EquipmentStatType.AttackCooldown && statType != EquipmentStatType.ProjectileSpeed)
					{
						_statInfoViewPool.Spawn().SetComparisonInfo(statType.GetTranslation(), statText, delta, 
						                                            statType, equippedValue, selectedValue ); 
					}
				}

				return;
			}
			
			for (int i = 0; i < selectedStats.Count; i++)
			{
				var statType = selectedStats[i].Key;
				
				if (statType == EquipmentStatType.AttackCooldown || statType == EquipmentStatType.ProjectileSpeed)
				{
					continue;
				}
				
				if (selectedStats[i].Value > 0 && (statType == EquipmentStatType.SpecialId0 || statType == EquipmentStatType.SpecialId1))
				{
					GetSpecialIconInfo(selectedStats[i].Key, _statSpecialInfoViewPool.Spawn(), (GameId) selectedStats[i].Value); 
				}
				// Show the player the current stats compared to the stats when this piece of Equipment is upgraded.
				else
				{
					var statsBeautifier = statType == EquipmentStatType.Speed ? GameConstants.MOVEMENT_SPEED_BEAUTIFIER : 1f;
					var selectedValue = selectedStats[i].Value * statsBeautifier;
					
					if (selectedInfo.IsMaxLevel)
					{
						_statInfoViewPool.Spawn().SetInfo(statType, statType.GetTranslation(), selectedValue, maxLevelInfo.Stats[statType]);
						continue;
					}
					
					var infoAtNextLevel = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(selectedInfo.DataInfo.GameId, 
						selectedInfo.DataInfo.Data.Rarity, selectedInfo.DataInfo.Data.Adjective, selectedInfo.DataInfo.Data.Material, selectedInfo.DataInfo.Data.Manufacturer, selectedInfo.DataInfo.Data.Faction, selectedInfo.DataInfo.Data.Level + 1, selectedInfo.DataInfo.Data.Grade);
					var equippedStats = infoAtNextLevel.Stats.ToList();
					var format = statType == EquipmentStatType.ReloadSpeed ? "N1" : "N0";
					var equippedValue = equippedStats[i].Value * statsBeautifier;
					var statText = selectedValue.ToString(format);
					var delta = statType == EquipmentStatType.ReloadSpeed ? equippedValue - selectedValue : Mathf.RoundToInt(equippedValue) - Mathf.RoundToInt(selectedValue);
					
					_statInfoViewPool.Spawn().SetComparisonInfo(statType.GetTranslation(), statText, delta, 
					                                            statType, selectedValue, equippedValue);
				}
			}
		}

		private async void GetSpecialIconInfo(EquipmentStatType key, EquipmentStatSpecialInfoView slotInfo, GameId specialId)
		{
			var specialType = Services.ConfigsProvider.GetConfig<QuantumSpecialConfig>((int) specialId).SpecialType;
			var sprite = await Services.AssetResolverService.RequestAsset<SpecialType, Sprite>(specialType, false);
			var title = key == EquipmentStatType.SpecialId0 ? 
				            ScriptLocalization.General.PrimarySpecial : 
				            ScriptLocalization.General.SecondarySpecial;
			
			slotInfo.SetInfo(title , specialId, sprite);
		}
		

		private void SetEquipButtonStatus()
		{
			var status = _gameDataProvider.EquipmentDataProvider.IsEquipped(_uniqueId) ? 
				             ScriptLocalization.General.Unequip :  ScriptLocalization.General.Equip;
			_equipButtonText.SetText(status);
		}

		private void OnItemSoldMessage(ItemSoldMessage message)
		{
			_gameDataProvider.EquipmentDataProvider.EquippedItems.TryGetValue(Data.EquipmentSlot, out var id);

			_uniqueId = id;

			UpdateEquipmentMenu();
			SetStats();
		}
		
		
		private void OnItemUnequippedMessage(ItemUnequippedMessage message)
		{
			_uniqueId = message.ItemId;

			UpdateEquipmentMenu();
			SetStats();
		}

		private void OnItemEquippedMessage(ItemEquippedMessage obj)
		{
			UpdateEquipmentMenu();
			SetStats();
		}

		private void OnEquipmentClicked(UniqueId itemClicked)
		{
			_uniqueId = itemClicked;
			_showNotifications.Remove(_uniqueId);

			UpdateEquipmentMenu();
			SetStats();
		}

		private void OnEquipButtonClicked()
		{
			var previousPower = _gameDataProvider.EquipmentDataProvider.GetTotalEquippedItemPower();
			
			if (_gameDataProvider.EquipmentDataProvider.IsEquipped(_uniqueId))
			{
				var isWeapon = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(_uniqueId).IsWeapon;
				
				// Can't unequip your last weapon.
				if (isWeapon && _gameDataProvider.EquipmentDataProvider.GetInventoryInfo(GameIdGroup.Weapon).Count == 1)
				{
					var confirmButton = new GenericDialogButton
					{
						ButtonText = ScriptLocalization.General.OK,
						ButtonOnClick = Services.GenericDialogService.CloseDialog
					};

					Services.GenericDialogService.OpenDialog(ScriptLocalization.General.EquipLastWeaponWarning, false, confirmButton);
					
					return;
				}
				
				Services.CommandService.ExecuteCommand(new UnequipItemCommand { ItemId = _uniqueId });
				
				// Equip Default/Melee weapon after unequipping a regular one
				if (isWeapon)
				{
					var weapons = _gameDataProvider.EquipmentDataProvider.GetInventoryInfo(GameIdGroup.Weapon);
					for (int i = 0; i < weapons.Count; i++)
					{
						if (_gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(weapons[i].GameId)
						                     .Stats[EquipmentStatType.MaxCapacity] < 0)
						{
							Services.CommandService.ExecuteCommand(new EquipItemCommand {ItemId = weapons[i].Data.Id});
							break;
						}
					}
				}
			}
			else
			{
				Services.CommandService.ExecuteCommand(new EquipItemCommand { ItemId = _uniqueId });
			}
			
			ShowPowerChange((int) previousPower);
		}
		
		private void OnSellClicked()
		{
			var info = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(_uniqueId);
			
			// Selling your last weapon isn't allowed
			if (info.IsWeapon && _gameDataProvider.EquipmentDataProvider.GetInventoryInfo(GameIdGroup.Weapon).Count == 1)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = Services.GenericDialogService.CloseDialog
				};

				Services.GenericDialogService.OpenDialog(ScriptLocalization.General.SellLastWeaponWarning, false, confirmButton);
			}
			else
			{
				var priceString = string.Format(ScriptLocalization.General.SellItemFor, info.SellCost.ToString());
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.Yes,
					ButtonOnClick = OnSaleCompleted
				};

				Services.GenericDialogService.OpenDialog(priceString, true, confirmButton);
			}
		}

		private void ShowPowerChange(int previousPower)
		{
			var power = (int) _gameDataProvider.EquipmentDataProvider.GetTotalEquippedItemPower() - previousPower; 
			var postfix = power < 0 ? "-" : "+";
			
			_powerChangeText.color = power < 0 ? Color.red : Color.green;

			if (power != 0)
			{
				_powerChangeText.text = $"{ScriptLocalization.MainMenu.Power} {postfix} {Mathf.Abs(power).ToString()}";
				_powerChangeText.enabled = true;
				_powerChangeAnimation.Rewind();
				_powerChangeAnimation.Play();
			}
		}

		private void OnSaleCompleted()
		{
			var previousPower = _gameDataProvider.EquipmentDataProvider.GetTotalEquippedItemPower();
			
			Services.CommandService.ExecuteCommand(new SellItemCommand { ItemId = _uniqueId });
			
			ShowPowerChange((int) previousPower);
		}
		
		private void UpdateEquipmentMenu()
		{
			var inventory = _gameDataProvider.EquipmentDataProvider.Inventory;
			var list = new List<EquipmentGridItemView.EquipmentGridItemData>(inventory.Count);

			for (var i = 0; i < inventory.Count; i++)
			{
				var info = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(inventory[i].Id);
				
				if (!info.DataInfo.GameId.IsInGroup(Data.EquipmentSlot)
				    // Don't show Default/Melee weapon
				    || (info.IsWeapon && info.Stats[EquipmentStatType.MaxCapacity] < 0))
				{
					continue;
				}

				var viewData = new EquipmentGridItemView.EquipmentGridItemData
				{
					Info = info,
					IsSelected = info.DataInfo.Data.Id == _uniqueId,
					PlayViewNotificationAnimation = _showNotifications.Contains(info.DataInfo.Data.Id),
					IsSelectable = true,
					OnEquipmentClicked = OnEquipmentClicked
				};
					
				list.Add(viewData);
			}

			_noItemsCollectedText.enabled = (list.Count == 0);

			if (list.Count == 0)
			{
				_descriptionText.text = ScriptLocalization.General.CollectItemsFromCrates;
			}

			list.Sort(new EquipmentSorter(_equipmentSortState));
			_titleText.SetText(Data.EquipmentSlot.GetTranslation());
			_gridView.UpdateData(list);

			_showNotifications.Clear();
		}
	}
}