using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using FirstLight.Game.Logic;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using FirstLight.FLogger;
using FirstLight.Game.Commands.OfflineCommands;
using FirstLight.Game.Infos;
using FirstLight.Game.UIElements;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles the equipment selection screen.
	/// </summary>
	[LoadSynchronously]
	public class EquipmentSelectionPresenter : UiToolkitPresenterData<EquipmentSelectionPresenter.StateData>
	{
		private const string HEADER_LOC_KEY = "UITEquipment/selection_{0}";
		private const string ADJECTIVE_LOC_KEY = "UITEquipment/adjective_{0}";
		private const string RARITY_LOC_KEY = "UITEquipment/rarity_{0}";
		private const string NO_ITEMS_LOC_KEY = "UITEquipment/details_no_{0}";
		private const string DURABILITY_AMOUNT = "{0}/{1}";

		private const string UssEquipmentTagRarity = "equipment-tag--rarity";
		private const string UssEquipmentTagRarityModifier = UssEquipmentTagRarity + "-{0}";
		private const string UssEquipmentTagSpecial = "equipment-tag--special";
		private const string UssEquipmentTagSpecialModifier = UssEquipmentTagSpecial + "-{0}";

		public struct StateData
		{
			public GameIdGroup EquipmentSlot;
			public Action OnCloseClicked;
			public Action OnBackClicked;
		}

		private ScreenHeaderElement _header;
		private ListView _equipmentList;
		private Label _mightLabel;

		private VisualElement _details;
		private Label _missingEquipment;
		private Label _equipmentName;
		private VisualElement _equipmentIcon;
		private ListView _statsList;
		private VisualElement _durabilityBar;
		private Label _durabilityAmount;
		private Button _equipButton;

		private VisualElement _cooldownTag;
		private VisualElement _rarityTag;
		private VisualElement _special0Tag;
		private VisualElement _special1Tag;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private List<EquipmentListRow> _equipmentListRows;
		private Dictionary<UniqueId, int> _itemRowMap;
		private List<KeyValuePair<EquipmentStatType, float>> _statItems;

		private Tweener _mightTweener;
		private float _currentMight;

		private UniqueId _selectedItem;
		private UniqueId _equippedItem;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_header = root.Q<ScreenHeaderElement>("Header").Required();
			_header.backClicked += Data.OnBackClicked;
			_header.homeClicked += Data.OnCloseClicked;

			_equipmentList = root.Q<ListView>("EquipmentList").Required();
			_equipmentList.DisableScrollbars();
			_mightLabel = root.Q<Label>("MightLabel").Required();
			_missingEquipment = root.Q<Label>("MissingEquipment").Required();
			_missingEquipment.text = string.Format(NO_ITEMS_LOC_KEY, Data.EquipmentSlot.ToString().ToLowerInvariant())
				.LocalizeKey();

			_details = root.Q("Details").Required();
			_equipmentName = root.Q<Label>("EquipmentName").Required();
			_equipmentIcon = root.Q("EquipmentIcon").Required();
			_statsList = root.Q<ListView>("StatsList").Required();
			_statsList.DisableScrollbars();

			_cooldownTag = root.Q("CooldownTag").Required();
			_rarityTag = root.Q("RarityTag").Required();
			_special0Tag = root.Q("Special0Tag").Required();
			_special1Tag = root.Q("Special1Tag").Required();

			_durabilityBar = root.Q("DurabilityProgress").Required();
			_durabilityAmount = root.Q<Label>("DurabilityAmount").Required();
			_equipButton = root.Q<Button>("EquipButton").Required();

			_equipButton.clicked += OnEquipClicked;

			_equipmentList.makeItem = MakeEquipmentListItem;
			_equipmentList.bindItem = BindEquipmentListItem;

			_statsList.makeItem = MakeEquipmentStatListItem;
			_statsList.bindItem = BindEquipmentStatListItem;
			
			root.SetupClicks(_services);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_header.SetTitle(LocalizationManager.GetTranslation(
				string.Format(HEADER_LOC_KEY, Data.EquipmentSlot.ToString().ToLowerInvariant())));

			UpdateEquipmentList();
			UpdateEquipmentDetails();
			UpdateEquipButton();
			UpdateMight(false);

			_gameDataProvider.EquipmentDataProvider.Loadout.Observe(Data.EquipmentSlot, OnLoadoutUpdated);
		}

		protected override Task OnClosed()
		{
			_gameDataProvider.EquipmentDataProvider.Loadout.StopObservingAll(this);
			return base.OnClosed();
		}

		private void OnLoadoutUpdated(GameIdGroup group, UniqueId previous, UniqueId current, ObservableUpdateType type)
		{
			var previousItem = _equippedItem;

			_equippedItem = current;

			if (previousItem != UniqueId.Invalid) _equipmentList.RefreshItem(_itemRowMap[previousItem]);
			if (_equippedItem != UniqueId.Invalid) _equipmentList.RefreshItem(_itemRowMap[_equippedItem]);
			UpdateEquipButton();
			UpdateMight();
		}

		private void UpdateEquipmentList()
		{
			var items = _gameDataProvider.EquipmentDataProvider.Inventory.ReadOnlyDictionary
				.Where(kvp => kvp.Value.GameId.IsInGroup(Data.EquipmentSlot))
				.ToList();

			// Sort items by GameID (string)
			items.Sort((x, y) =>
				string.Compare(x.Value.GameId.ToString(), y.Value.GameId.ToString(), StringComparison.Ordinal));

			_equipmentListRows = new List<EquipmentListRow>(items.Count / 2);
			_itemRowMap = new Dictionary<UniqueId, int>(items.Count / 2);

			for (var i = 0; i < items.Count; i += 2)
			{
				var item1 = items[i];

				if (i + 1 >= items.Count)
				{
					_equipmentListRows.Add(
						new EquipmentListRow(new EquipmentListRow.Item(item1.Key, item1.Value), null));
					_itemRowMap[item1.Key] = _equipmentListRows.Count - 1;
				}
				else
				{
					var item2 = items[i + 1];
					_equipmentListRows.Add(new EquipmentListRow(
						new EquipmentListRow.Item(item1.Key, item1.Value),
						new EquipmentListRow.Item(item2.Key, item2.Value)));
					_itemRowMap[item1.Key] = _equipmentListRows.Count - 1;
					_itemRowMap[item2.Key] = _equipmentListRows.Count - 1;
				}
			}

			if (!_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(Data.EquipmentSlot, out _equippedItem))
			{
				_equippedItem = UniqueId.Invalid;
			}

			if (_equipmentListRows.Count == 0)
			{
				_missingEquipment.style.display = DisplayStyle.Flex;
				_selectedItem = UniqueId.Invalid;
			}
			else
			{
				_missingEquipment.style.display = DisplayStyle.None;
				_selectedItem = _equipmentListRows[0].Item1.UniqueId;
				_equipmentList.ScrollToItem(0);

				// Set the first item as viewed
				_gameDataProvider.UniqueIdDataProvider.NewIds.Remove(_selectedItem);
			}

			_equipmentList.itemsSource = _equipmentListRows;
			_equipmentList.RefreshItems();
		}

		private async void UpdateEquipmentDetails()
		{
			_details.style.display = _selectedItem == UniqueId.Invalid ? DisplayStyle.None : DisplayStyle.Flex;

			if (_selectedItem == UniqueId.Invalid) return;

			var info = _gameDataProvider.EquipmentDataProvider.GetInfo(_selectedItem);

			// Title
			_equipmentName.text = string.Format(ScriptLocalization.UITEquipment.equipment_details_title,
				string.Format(ADJECTIVE_LOC_KEY, info.Equipment.Adjective.ToString().ToLowerInvariant()).LocalizeKey(),
				info.Equipment.GameId.GetTranslation(),
				info.Equipment.Level);

			// Durability
			_durabilityAmount.text =
				string.Format(DURABILITY_AMOUNT, info.CurrentDurability.ToString(), info.Equipment.MaxDurability.ToString());
			_durabilityBar.style.flexGrow = info.CurrentDurability / info.Equipment.MaxDurability;

			// Stats
			_statItems = info.Stats.Where(pair => EquipmentStatBarElement.CanShowStat(pair.Key, pair.Value)).ToList();
			_statItems.Sort((x1, x2) => x1.Key.CompareTo(x2.Key));
			_statsList.itemsSource = _statItems;
			_statsList.RefreshItems();

			// Cooldown tag
			_cooldownTag.style.visibility = Visibility.Hidden;
			if (_gameDataProvider.EquipmentDataProvider.TryGetNftInfo(_selectedItem, out var nftInfo))
			{
				if (nftInfo.IsOnCooldown)
				{
					_cooldownTag.style.visibility = Visibility.Visible;
				}
			}

			// Rarity tag
			_rarityTag.RemoveModifiers();
			_rarityTag.AddToClassList(UssEquipmentTagRarity);
			_rarityTag.AddToClassList(string.Format(UssEquipmentTagRarityModifier,
				info.Equipment.Rarity.ToString().Replace("Plus", "").ToLowerInvariant()));
			_rarityTag.Q<Label>("Title").text = string.Format(RARITY_LOC_KEY,
				info.Equipment.Rarity.ToString().ToLowerInvariant()).LocalizeKey();

			// Specials tags
			_special0Tag.style.display = DisplayStyle.None;
			_special0Tag.RemoveModifiers();
			if (info.Stats.TryGetValue(EquipmentStatType.SpecialId0, out var special0))
			{
				var special0ID = (GameId) special0;
				_special0Tag.style.display = DisplayStyle.Flex;
				_special0Tag.AddToClassList(UssEquipmentTagSpecial);
				_special0Tag.AddToClassList(string.Format(UssEquipmentTagSpecialModifier,
					special0ID.ToString().Replace("Special", "").ToLowerInvariant()));
				_special0Tag.Q<Label>("Title").text = special0ID.GetTranslation();
			}

			_special1Tag.style.display = DisplayStyle.None;
			_special1Tag.RemoveModifiers();
			if (info.Stats.TryGetValue(EquipmentStatType.SpecialId1, out var special1))
			{
				var special1ID = (GameId) special1;
				_special1Tag.style.display = DisplayStyle.Flex;
				_special1Tag.AddToClassList(UssEquipmentTagSpecial);
				_special1Tag.AddToClassList(string.Format(UssEquipmentTagSpecialModifier,
					special1ID.ToString().Replace("Special", "").ToLowerInvariant()));
				_special1Tag.Q<Label>("Title").text = special1ID.GetTranslation();
			}

			// Icon
			_equipmentIcon.style.backgroundImage = new StyleBackground(
				await _services.AssetResolverService.RequestAsset<GameId, Sprite>(
					info.Equipment.GameId, instantiate: false));

			// Set item as viewed
			_gameDataProvider.UniqueIdDataProvider.NewIds.Remove(_selectedItem);
		}

		private void UpdateEquipButton()
		{
			_equipButton.text = _equippedItem == _selectedItem
				? ScriptLocalization.UITEquipment.unequip
				: ScriptLocalization.UITEquipment.equip;
		}

		private void UpdateMight(bool animate = true)
		{
			var loadout = _gameDataProvider.EquipmentDataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.All);
			var might = loadout.GetTotalMight(_services.ConfigsProvider.GetConfigsDictionary<QuantumStatConfig>());

			_mightTweener?.Kill();

			if (animate)
			{
				_mightTweener = DOVirtual.Float(_currentMight, might, 0.3f,
					val =>
					{
						_mightLabel.text = string.Format(ScriptLocalization.UITEquipment.might, val.ToString("F0"));
					});
			}
			else
			{
				_mightLabel.text = string.Format(ScriptLocalization.UITEquipment.might, might.ToString("F0"));
			}

			_currentMight = might;
		}

		private VisualElement MakeEquipmentStatListItem()
		{
			return new EquipmentStatBarElement();
		}

		private VisualElement MakeEquipmentListItem()
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

			var item1 = new EquipmentCardElement {name = "item-1"};
			var item2 = new EquipmentCardElement {name = "item-2"};

			item1.clicked += OnEquipmentClicked;
			item2.clicked += OnEquipmentClicked;

			row.Add(item1);
			row.Add(item2);

			return row;
		}

		private void BindEquipmentListItem(VisualElement visualElement, int index)
		{
			var row = _equipmentListRows[index];

			var card1 = visualElement.Q<EquipmentCardElement>("item-1");
			var card2 = visualElement.Q<EquipmentCardElement>("item-2");

			card1.SetEquipment(row.Item1.Equipment, row.Item1.UniqueId, false,
				_gameDataProvider.EquipmentDataProvider.NftInventory.ContainsKey(row.Item1.UniqueId),
				card1.UniqueId == _equippedItem,
				_gameDataProvider.UniqueIdDataProvider.NewIds.Contains(row.Item1.UniqueId));

			if (row.Item2 != null)
			{
				card2.SetDisplayActive(true);
				card2.SetEquipment(row.Item2.Equipment, row.Item2.UniqueId, false,
					_gameDataProvider.EquipmentDataProvider.NftInventory.ContainsKey(row.Item2.UniqueId),
					card2.UniqueId == _equippedItem,
					_gameDataProvider.UniqueIdDataProvider.NewIds.Contains(row.Item2.UniqueId));
			}
			else
			{
				card2.SetDisplayActive(false);
			}

			card1.SetSelected(card1.UniqueId == _selectedItem);
			card2.SetSelected(card2.UniqueId == _selectedItem);
		}

		private void BindEquipmentStatListItem(VisualElement visualElement, int index)
		{
			var statElement = (EquipmentStatBarElement) visualElement;

			var stat = _statItems[index];
			statElement.SetValue(stat.Key, stat.Value);
		}

		private void OnEquipmentClicked(Equipment equipment, UniqueId id)
		{
			if (id == _selectedItem) return;

			var previousItem = _selectedItem;

			_selectedItem = id;
			UpdateEquipmentDetails();

			_equipmentList.RefreshItem(_itemRowMap[previousItem]);
			_equipmentList.RefreshItem(_itemRowMap[_selectedItem]);
			UpdateEquipButton();
		}

		private void OnEquipClicked()
		{
			var dataProvider = _gameDataProvider.EquipmentDataProvider;
			var loadout = dataProvider.GetLoadoutEquipmentInfo(EquipmentFilter.All);
			var item = loadout.Find(infoItem => infoItem.Id == _selectedItem);

			if (item.IsEquipped)
			{
				_services.AudioFxService.PlayClip2D(AudioId.UnequipEquipment);
				UnequipItem(_selectedItem);

				// Equip Default/Melee weapon after unequipping a regular one
				if (item.Equipment.IsWeapon())
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
				_services.AudioFxService.PlayClip2D(AudioId.EquipEquipment);
				EquipItem(_selectedItem);
			}
		}

		private void EquipItem(UniqueId item)
		{
			_services.CommandService.ExecuteCommand(new EquipItemCommand {Item = item});
		}

		private void UnequipItem(UniqueId item)
		{
			_services.CommandService.ExecuteCommand(new UnequipItemCommand {Item = item});
		}

		private class EquipmentListRow
		{
			public Item Item1 { get; }
			public Item Item2 { get; }

			public EquipmentListRow(Item item1, Item item2)
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