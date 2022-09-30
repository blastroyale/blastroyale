using UnityEngine;
using UnityEngine.UI;
using FirstLight.Game.Services;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using UnityEngine.Events;
using Quantum;
using Sirenix.OdinInspector;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This script shows currently equipped items on the Loot Screen.
	/// </summary>
	public class EquippedLootView : MonoBehaviour
	{
		[SerializeField] protected GameIdGroup _slot;
		[SerializeField, Required] protected EquipmentCardView _cardView;
		[SerializeField, Required] protected Image _slotImage;
		[SerializeField, Required] protected Button _button;

		public UnityEvent<GameIdGroup> OnClick = new();

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		public UniqueId ItemId { get; protected set; } = UniqueId.Invalid;

		protected void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();

			_button.onClick.AddListener(OnButtonClick);
		}

		protected void OnDestroy()
		{
			_services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		/// <summary>
		/// Updates the icon state view
		/// </summary>
		public void UpdateItem()
		{
			if (_gameDataProvider.EquipmentDataProvider.Loadout.TryGetValue(_slot, out var uniqueId))
			{
				var equipment = _gameDataProvider.EquipmentDataProvider.Inventory[uniqueId];

				// Don't show Default/Melee weapon
				if (equipment.IsWeapon() && equipment.IsDefaultItem())
				{
					ClearSlot();
				}
				else
				{
					_slotImage.enabled = false;
					_cardView.gameObject.SetActive(true);

					if (ItemId != uniqueId)
					{
						_cardView.Initialise(equipment);
					}

					ItemId = uniqueId;
				}
			}
			else
			{
				ClearSlot();
			}
		}

		private void ClearSlot()
		{
			ItemId = UniqueId.Invalid;
			_slotImage.enabled = true;
			_cardView.gameObject.SetActive(false);
		}

		protected virtual void OnButtonClick()
		{
			OnClick.Invoke(_slot);
		}
	}
}