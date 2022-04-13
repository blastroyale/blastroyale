using UnityEngine;
using UnityEngine.UI;
using FirstLight.Game.Services;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Infos;
using I2.Loc;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using TMPro;
using UnityEngine.Events;
using FirstLight.Game.Messages;
using Quantum;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script shows currently equipped items on the Loot Screen.
	/// </summary>
	public class EquippedLootView : MonoBehaviour
	{
		[SerializeField] protected GameIdGroup _slot;
		[SerializeField] protected Image _iconImage;
		[SerializeField] protected Image _rarityImage;
		[SerializeField] protected Image _slotImage;
		[SerializeField] protected Button _button;
		[SerializeField] protected TextMeshProUGUI _levelText;
		[SerializeField] protected NotificationUniqueIdUpgradeView _notificationUniqueIdUpgradeView;
		
		public UnityEvent<GameIdGroup> OnClick = new UnityEvent<GameIdGroup>();

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		public UniqueId ItemId { get; protected set; } = UniqueId.Invalid;

		protected void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			
			_services.MessageBrokerService.Subscribe<ItemUnequippedMessage>(OnItemUnequipped);
			_button.onClick.AddListener(OnButtonClick);
		}

		protected void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this );
		}

		/// <summary>
		/// Updates the icon state view
		/// </summary>
		public async void UpdateItem()
		{
			if (_gameDataProvider.EquipmentDataProvider.EquippedItems.TryGetValue(_slot, out var uniqueId))
			{
				var info = _gameDataProvider.EquipmentDataProvider.GetEquipmentInfo(uniqueId);
				
				// Don't show Default/Melee weapon
				if (info.IsWeapon && info.Stats[EquipmentStatType.MaxCapacity] < 0)
				{
					ClearSlot();
				}
				else
				{
					_levelText.text = $"{ScriptLocalization.General.Level} {info.DataInfo.Data.Level.ToString()}";
					_iconImage.enabled = true;
					_slotImage.enabled = false;
					_rarityImage.enabled = true;
					_rarityImage.sprite = await _services.AssetResolverService.RequestAsset<ItemRarity, Sprite>(info.DataInfo.Data.Rarity);

					if (ItemId != uniqueId)
					{
						_iconImage.sprite = await _services.AssetResolverService.RequestAsset<GameId, Sprite>(info.DataInfo.GameId);
					}

					ItemId = uniqueId;
				}
			}
			else
			{
				ClearSlot();
			}
			
			_notificationUniqueIdUpgradeView.SetUniqueId(ItemId);
		}

		private void ClearSlot()
		{
			ItemId = UniqueId.Invalid;
			_levelText.text = "";
			_iconImage.enabled = false;
			_slotImage.enabled = true;
			_rarityImage.enabled = false;
		}

		private void OnItemUnequipped(ItemUnequippedMessage itemMessage)
		{
			if (itemMessage.ItemId == ItemId)
			{
				UpdateItem();
			}
		}

		protected virtual void OnButtonClick()
		{
			OnClick.Invoke(_slot);
		}
	}
}