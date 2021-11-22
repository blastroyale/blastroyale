using System;
using System.Collections;
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
		[SerializeField] private Button _closeButton;
		[SerializeField] private GenericGridView _gridView;
		[SerializeField] private TextMeshProUGUI _screenTitleText;
		[SerializeField] private TextMeshProUGUI _titleText;
		[SerializeField] private TextMeshProUGUI _noItemsCollectedText;
		
		[Header("Equipment Info Panel")]
		[SerializeField] private TextMeshProUGUI _descriptionText;
		[SerializeField] private Button _equipUnequipButton;
		[SerializeField] private Button _sellButton;
		[SerializeField] private Button _upgradeButton;
		[SerializeField] private EquipmentStatInfoView _statInfoViewPoolRef;
		[SerializeField] private EquipmentStatSpecialInfoView _specialStatInfoViewPoolRef;
		[SerializeField] private TextMeshProUGUI _itemTitleText;
		[SerializeField] private TextMeshProUGUI _itemLevelText;
		[SerializeField] private GameObject _actionButtonHolder;
		[SerializeField] private GameObject _equippedStatusObject;
		[SerializeField] private GameObject _itemLevelObject;
		[SerializeField] private GameObject _upgradeCostHolder;
		[SerializeField] private TextMeshProUGUI _equipButtonText;
		[SerializeField] private TextMeshProUGUI _upgradeCostText;
		[SerializeField] private TextMeshProUGUI _sellCostText;
		[SerializeField] private Animation _itemLevelHolderAnimation;

		[SerializeField] private Image _upgradeCoinImage;
		[SerializeField] private Image _upgradeButtonImage;
		[SerializeField] private Sprite _maxUpgradeButtonSprite;
		[SerializeField] private Sprite _upgradeButtonSprite;

		[SerializeField] private GameObject _rarityHolder;
		[SerializeField] private Image _weaponTypeImage;
		[SerializeField] private Button _weaponTypeButton;
		[SerializeField] private Button _movieButton;
		[SerializeField] private Image [] _rarityImage;
		[SerializeField] private TextMeshProUGUI _rarityText;
		[SerializeField] private TextMeshProUGUI _weaponTypeText;
		[SerializeField] private TextMeshProUGUI _powerRatingText;
		[SerializeField] private TextMeshProUGUI _powerChangeText;
		[SerializeField] private Animation _powerChangeAnimation;
		
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
			_movieButton.onClick.AddListener(OnMovieClicked);
			_weaponTypeButton.onClick.AddListener(OnMovieClicked);
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
		
		private void SetStats()
		{
			var equipmentProvider = _gameDataProvider.EquipmentDataProvider;
			var itemEquipped = equipmentProvider.IsEquipped(_uniqueId);
			
			_statInfoViewPool?.DespawnAll();
			_statSpecialInfoViewPool?.DespawnAll();
			_equippedStatusObject.SetActive(itemEquipped);

			if (_uniqueId == UniqueId.Invalid)
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

				return;
			}
			
			var info = equipmentProvider.GetEquipmentInfo(_uniqueId);
			var maxLevelInfo = equipmentProvider.GetEquipmentInfo(info.DataInfo.GameId, info.DataInfo.Data.Rarity, info.MaxLevel);
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
			
			if (info.IsWeapon)
			{
				var config = Services.ConfigsProvider.GetConfig<QuantumWeaponConfig>((int) info.DataInfo.GameId);
				
				_weaponTypeText.text = config.IsAutoShoot ? ScriptLocalization.MainMenu.AutoFire : ScriptLocalization.MainMenu.ManualFire;
				_weaponTypeImage.color = config.IsAutoShoot ? _autoFireColor : _manualFireColor;
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
			if (_upgradeButtonImage.sprite == _maxUpgradeButtonSprite)
			{
				_mainMenuServices.UiVfxService.PlayFloatingText(ScriptLocalization.MainMenu.WeaponIsAtMaxLevel);
				
				return;
			}

			var info = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(_uniqueId);
			
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

					if (statType == EquipmentStatType.SpecialId0 || statType == EquipmentStatType.SpecialId1)
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
				
				if (statType == EquipmentStatType.SpecialId0 || statType == EquipmentStatType.SpecialId1)
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
						selectedInfo.DataInfo.Data.Rarity, selectedInfo.DataInfo.Data.Level + 1);
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
				// Can't unequip your last weapon.
				if (_gameDataProvider.EquipmentDataProvider.GetInventoryInfo(GameIdGroup.Weapon).Count == 1 
				    && _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(_uniqueId).IsWeapon)
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
			}
			else
			{
				Services.CommandService.ExecuteCommand(new EquipItemCommand { ItemId = _uniqueId });
			}
			
			ShowPowerChange((int) previousPower);
		}

		private void OnMovieClicked()
		{
			if (!_gameDataProvider.EquipmentDataProvider.TryGetWeaponInfo(_uniqueId, out var info))
			{
				return;
			}
			
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.AWESOME,
				ButtonOnClick = Services.GenericDialogService.CloseDialog
			};

			var title = info.WeaponConfig.IsAutoShoot ? 
				            ScriptLocalization.MainMenu.AutoFire : 
				            ScriptLocalization.MainMenu.ManualFire;
			var description = info.WeaponConfig.IsAutoShoot ? 
				                  ScriptLocalization.MainMenu.HoldToFireDescription : 
				                  ScriptLocalization.MainMenu.DragAndReleaseToFireDescription;
			var videoId = info.WeaponConfig.IsAutoShoot ? GameId.AssaultRifle : GameId.SniperRifle;
			
			Services.GenericDialogService.OpenVideoDialog(title, description, videoId, false, confirmButton);
		}
		
		private void OnSellClicked()
		{
			var info = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(_uniqueId);
			
			// Selling your last weapon isn't allowed. In the future we may allow players to enter the game without any weapon.
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
				var viewData = new EquipmentGridItemView.EquipmentGridItemData
				{
					Info = info,
					IsSelected = info.DataInfo.Data.Id == _uniqueId,
					PlayViewNotificationAnimation = _showNotifications.Contains(info.DataInfo.Data.Id),
					IsSelectable = true,
					OnEquipmentClicked = OnEquipmentClicked
				};

				if (info.DataInfo.GameId.IsInGroup(Data.EquipmentSlot))
				{
					list.Add(viewData);
				}
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