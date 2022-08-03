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
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Infos;
using FirstLight.Services;
using I2.Loc;
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

		[Header("Equipment Dialog / OSA")] [SerializeField, Required]  
		private Button _closeButton;

		[SerializeField, Required] private GenericGridView _gridView;
		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private TextMeshProUGUI _screenTitleText;
		[SerializeField, Required] private TextMeshProUGUI _titleText;
		[SerializeField, Required] private TextMeshProUGUI _noItemsCollectedText;
		
		[Header("Equipment Info Panel")] [SerializeField, Required]
		private TextMeshProUGUI _descriptionText;

		[SerializeField, Required] private Button _equipUnequipButton;
		[SerializeField, Required] private EquipmentStatInfoView _statInfoViewPoolRef;
		[SerializeField, Required] private EquipmentStatSpecialInfoView _specialStatInfoViewPoolRef;
		[SerializeField, Required] private EquipmentCooldownView _equipmentCooldownViewRef;

		// TODO: This should be a view when we properly implement it
		[SerializeField, Required] private TextMeshProUGUI _itemTitleText;
		[SerializeField, Required] private GameObject _equipmentAttributesHolder;

		[SerializeField, Required] private TextMeshProUGUI _generationText;
		[SerializeField, Required] private TextMeshProUGUI _editionText;
		[SerializeField, Required] private TextMeshProUGUI _rarityText;
		[SerializeField] private Image[] _rarityImage;
		[SerializeField, Required] private TextMeshProUGUI _materialText;
		[SerializeField, Required] private TextMeshProUGUI _gradeText;
		[SerializeField, Required] private TextMeshProUGUI _factionText;
		[SerializeField, Required] private TextMeshProUGUI _manufacturerText;
		[SerializeField, Required] private TextMeshProUGUI _durabilityText;
		[SerializeField, Required] private TextMeshProUGUI _restoredText;
		[SerializeField, Required] private TextMeshProUGUI _levelText;
		[SerializeField, Required] private TextMeshProUGUI _replicationText;
		[SerializeField, Required] private GameObject _actionButtonHolder;
		[SerializeField, Required] private GameObject _equippedStatusObject;
		[SerializeField, Required] private GameObject _itemLevelObject;
		[SerializeField, Required] private GameObject _upgradeCostHolder;
		[SerializeField, Required] private TextMeshProUGUI _equipButtonText;
		[SerializeField, Required] private TextMeshProUGUI _upgradeCostText;
		[SerializeField, Required] private Animation _itemLevelHolderAnimation;

		[SerializeField, Required] private Image _upgradeCoinImage;
		[SerializeField, Required] private Image _upgradeButtonImage;
		[SerializeField, Required] private Sprite _maxUpgradeButtonSprite;
		[SerializeField, Required] private Sprite _upgradeButtonSprite;

		[SerializeField, Required] private Image _weaponTypeImage;
		[SerializeField, Required] private Button _weaponTypeButton;
		[SerializeField, Required] private Button _movieButton;
		

		[SerializeField, Required] private TextMeshProUGUI _weaponTypeText;
		[SerializeField, Required] private TextMeshProUGUI _powerRatingText;
		[SerializeField, Required] private TextMeshProUGUI _powerChangeText;
		[SerializeField, Required] private Animation _powerChangeAnimation;

		[SerializeField] private Color _autoFireColor;
		[SerializeField] private Color _manualFireColor;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private EquipmentSorter.EquipmentSortState _equipmentSortState;
		private IObjectPool<EquipmentStatInfoView> _statInfoViewPool;
		private IObjectPool<EquipmentStatSpecialInfoView> _statSpecialInfoViewPool;
		private UniqueId _selectedId = UniqueId.Invalid;
		private List<UniqueId> _showNotifications;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_statInfoViewPool = new GameObjectPool<EquipmentStatInfoView>(4, _statInfoViewPoolRef);
			_statSpecialInfoViewPool = new GameObjectPool<EquipmentStatSpecialInfoView>(1, _specialStatInfoViewPoolRef);
			_showNotifications = new List<UniqueId>();

			_closeButton.onClick.AddListener(() => Close(true));
			_equipUnequipButton.onClick.AddListener(OnEquipButtonClicked);
			_statInfoViewPoolRef.gameObject.SetActive(false);
			_specialStatInfoViewPoolRef.gameObject.SetActive(false);
			_backButton.onClick.AddListener(OnBlockerButtonPressed);
		}

		protected void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		// We override the OnClosed because we want to show the Loot menu before the close animation completes
		// Also, we need to send command to update our loadout on the logic side
		protected override void OnClosed()
		{
			_showNotifications.Clear();

			base.OnClosed();
			Data.OnCloseClicked();
		}

		protected override async void OnOpened()
		{
			base.OnOpened();
			
			_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(Data.EquipmentSlot, out var id);

			_equipmentSortState = EquipmentSorter.EquipmentSortState.Rarity;
			_selectedId = id;
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

			_equipmentAttributesHolder.SetActive(false);
			_weaponTypeButton.gameObject.SetActive(false);
			_movieButton.gameObject.SetActive(false);
			_itemLevelObject.SetActive(false);
			_actionButtonHolder.SetActive(false);
			_equipmentCooldownViewRef.SetVisualsActive(false);
			_powerRatingText.text = "";
		}

		private void SetStats()
		{
			var equipmentProvider = _gameDataProvider.EquipmentDataProvider;

			_statInfoViewPool?.DespawnAll();
			_statSpecialInfoViewPool?.DespawnAll();
			_equippedStatusObject.SetActive(equipmentProvider.Loadout.ReadOnlyDictionary.Values.Contains(_selectedId));

			if (_selectedId == UniqueId.Invalid)
			{
				ShowStatsForEmptySlot();
				return;
			}

			var equipment = equipmentProvider.GetInfo(_selectedId);

			// Don't show Default/Melee weapon
			if (equipment.Equipment.IsWeapon() && equipment.Equipment.IsDefaultItem())
			{
				ShowStatsForEmptySlot();
				return;
			}

			var descriptionID = equipment.Equipment.GameId.GetTranslationTerm() + GameConstants.Visuals.DESCRIPTION_POSTFIX;
			var isWeapon = equipment.Equipment.GameId.IsInGroup(GameIdGroup.Weapon);

			SetStatInfoData(equipment);
			SetCooldownStatus();
			
			// TODO: Add proper translation logic
			_equipButtonText.SetText(equipment.IsEquipped ? ScriptLocalization.General.Unequip : ScriptLocalization.General.Equip);
			_powerRatingText.text = string.Format(ScriptLocalization.MainMenu.PowerRating, equipment.Stats[EquipmentStatType.Damage].ToString());
			_itemTitleText.text = $"{equipment.Equipment.Adjective} {equipment.Equipment.GameId.GetTranslation()}";
			_editionText.text = equipment.Equipment.Edition.ToString();
			_materialText.text = equipment.Equipment.Material.ToString();
			_gradeText.text = equipment.Equipment.Grade.ToString();
			_factionText.text = equipment.Equipment.Faction.ToString();
			_manufacturerText.text = equipment.Equipment.Manufacturer.ToString();
			_durabilityText.text = $"Durability {equipment.Equipment.Durability}/{equipment.Equipment.MaxDurability}";
			_restoredText.text = "??";
			_replicationText.text = $"Replication {equipment.Equipment.ReplicationCounter}/{equipment.Equipment.InitialReplicationCounter}";
			_screenTitleText.text = equipment.Equipment.GameId.GetSlot().GetTranslation();
			_descriptionText.text = LocalizationManager.GetTranslation(descriptionID);
			_rarityText.text = equipment.Equipment.Rarity.ToString();
			_levelText.text = equipment.Equipment.Level == equipment.Equipment.MaxLevel
				                  ? $"{ScriptLocalization.General.MaxLevel}"
				                  : $"{ScriptLocalization.General.Level} {equipment.Equipment.Level.ToString()}";

			_upgradeCostHolder.SetActive(!equipment.Equipment.IsMaxLevel());

			if (equipment.Equipment.Level < equipment.Equipment.MaxLevel)
			{
				_upgradeButtonImage.sprite = _upgradeButtonSprite;
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
				_rarityImage[i].enabled = i == (int) equipment.Equipment.Rarity;
			}

			_movieButton.gameObject.SetActive(isWeapon);
			_weaponTypeButton.gameObject.SetActive(isWeapon);
			_equipmentAttributesHolder.SetActive(true);
			_itemLevelObject.SetActive(true);
			_actionButtonHolder.SetActive(true);
		}

		private void SetStatInfoData(EquipmentInfo equipment)
		{
			var dataProvider = _gameDataProvider.EquipmentDataProvider;
			var equipmentMaxLevel = equipment.Equipment.AtLevel(equipment.Equipment.MaxLevel);
			var equipmentLevelInc = equipment.Equipment.AtLevel(equipment.Equipment.Level + 1);
			var statsAtMaxLevel = dataProvider.GetEquipmentStats(equipmentMaxLevel);
			var statsAtNextLevel = dataProvider.GetEquipmentStats(equipmentLevelInc);

			foreach (var (stat, value) in equipment.Stats)
			{
				if (stat == EquipmentStatType.AttackCooldown || stat == EquipmentStatType.ProjectileSpeed)
				{
					continue;
				}

				if (value > 0 && (stat == EquipmentStatType.SpecialId0 || stat == EquipmentStatType.SpecialId1))
				{
					GetSpecialIconInfo(stat, _statSpecialInfoViewPool.Spawn(), (GameId) value);
				}
				// Show the player the current stats compared to the stats when this piece of Equipment is upgraded.
				else
				{
					var statsBeautifier = stat == EquipmentStatType.Speed
						                      ? GameConstants.Visuals.MOVEMENT_SPEED_BEAUTIFIER
						                      : 1f;
					var selectedValue = value * statsBeautifier;

					if (equipment.Equipment.IsMaxLevel())
					{
						_statInfoViewPool.Spawn().SetInfo(stat, stat.GetTranslation(), selectedValue,
						                                  statsAtMaxLevel[stat]);
						continue;
					}

					var format = stat == EquipmentStatType.ReloadSpeed ? "N1" : "N0";
					var equippedValue = statsAtNextLevel[stat] * statsBeautifier;
					var statText = selectedValue.ToString(format);
					var delta = stat == EquipmentStatType.ReloadSpeed
						            ? equippedValue - selectedValue
						            : Mathf.RoundToInt(equippedValue) - Mathf.RoundToInt(selectedValue);

					_statInfoViewPool.Spawn().SetComparisonInfo(stat.GetTranslation(), statText, delta,
					                                            stat, selectedValue, equippedValue);
				}
			}
		}

		private void SetCooldownStatus()
		{
			_equipmentCooldownViewRef.InitCooldown(_selectedId);
		}

		private async void GetSpecialIconInfo(EquipmentStatType key, EquipmentStatSpecialInfoView slotInfo,
		                                      GameId specialId)
		{
			var specialType = Services.ConfigsProvider.GetConfig<QuantumSpecialConfig>((int) specialId).SpecialType;
			var sprite = await Services.AssetResolverService.RequestAsset<SpecialType, Sprite>(specialType, false);
			var title = key == EquipmentStatType.SpecialId0
				            ? ScriptLocalization.General.PrimarySpecial
				            : ScriptLocalization.General.SecondarySpecial;

			slotInfo.SetInfo(title, specialId, sprite);
		}

		private void OnEquipmentClicked(UniqueId itemClicked)
		{
			_selectedId = itemClicked;
			_showNotifications.Remove(_selectedId);

			UpdateEquipmentMenu();
			SetStats();
		}

		private void OnEquipButtonClicked()
		{
			var dataProvider = _gameDataProvider.EquipmentDataProvider;
			var loadout = dataProvider.GetLoadoutEquipmentInfo();
			var previousDamage = loadout.GetTotalStat(EquipmentStatType.Damage);
			var item = loadout.Find(infoItem => infoItem.Id == _selectedId);

			if (item.IsEquipped)
			{
				var isWeapon = item.Equipment.IsWeapon();

				// Can't unequip your last weapon.
				if (isWeapon && dataProvider.GetInventoryEquipmentInfo()
				                            .FindAll(info => info.Equipment.GameId.IsInGroup(GameIdGroup.Weapon)).Count == 1)
				{
					var confirmButton = new GenericDialogButton
					{
						ButtonText = ScriptLocalization.General.OK,
						ButtonOnClick = Services.GenericDialogService.CloseDialog
					};

					Services.GenericDialogService.OpenDialog(ScriptLocalization.General.EquipLastWeaponWarning, false,
					                                         confirmButton);

					return;
				}
				
				UnequipItem(_selectedId);

				// Equip Default/Melee weapon after unequipping a regular one
				if (isWeapon)
				{
					var defaultWeapon = dataProvider.Inventory.ReadOnlyDictionary
					                                .FirstOrDefault(e => e.Value.IsWeapon() && e.Value.IsDefaultItem());

					if (defaultWeapon.Key != UniqueId.Invalid)
					{
						EquipItem(defaultWeapon.Key);
					}
				}
			}
			else
			{
				EquipItem(_selectedId);
			}
			
			var damageDiff =  dataProvider.GetLoadoutEquipmentInfo().GetTotalStat(EquipmentStatType.Damage) - previousDamage;
			var postfix = damageDiff < 0 ? "-" : "+";

			_powerChangeText.color = damageDiff < 0 ? Color.red : Color.green;

			if (damageDiff > float.Epsilon)
			{
				_powerChangeText.text = $"{ScriptLocalization.MainMenu.Power} {postfix} {Mathf.Abs(damageDiff).ToString()}";
				_powerChangeText.enabled = true;
				_powerChangeAnimation.Rewind();
				_powerChangeAnimation.Play();
			}
		}

		private void EquipItem(UniqueId item)
		{
			_services.CommandService.ExecuteCommand(new EquipItemCommand { Item = item });
			
			UpdateEquipmentMenu();
			SetStats();
		}

		private void UnequipItem(UniqueId item)
		{
			_services.CommandService.ExecuteCommand(new UnequipItemCommand { Item = item });
			
			UpdateEquipmentMenu();
			SetStats();
		}

		private void UpdateEquipmentMenu()
		{
			var inventoryIds = _gameDataProvider.EquipmentDataProvider.Inventory.ReadOnlyDictionary.Keys.ToList();
			var list = new List<EquipmentGridItemView.EquipmentGridItemData>(inventoryIds.Count);

			foreach (var id in inventoryIds)
			{
				var equipment = _gameDataProvider.EquipmentDataProvider.Inventory[id];

				if (!equipment.GameId.IsInGroup(Data.EquipmentSlot)
				    // Don't show Default/Melee weapon
				    || (equipment.IsWeapon() && equipment.IsDefaultItem()))
				{
					continue;
				}

				var viewData = new EquipmentGridItemView.EquipmentGridItemData
				{
					Id = id,
					Equipment = equipment,
					IsSelected = id == _selectedId,
					PlayViewNotificationAnimation = _showNotifications.Contains(id),
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

		private void OnBlockerButtonPressed()
		{
			Data.OnCloseClicked();
		}
	}
}