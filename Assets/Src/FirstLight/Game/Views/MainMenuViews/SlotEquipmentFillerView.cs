using UnityEngine;
using FirstLight.Game.Logic;
using FirstLight.Game.Utils;
using FirstLight.Game.Ids;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Views.MainMenuViews
{
	/// <summary>
	/// This class handles Slots that can be filled with equipment icons in it in different screens
	/// </summary>
	public class SlotEquipmentFillerView : MonoBehaviour
	{
		[SerializeField, Required] private EquipmentIconItemView _equipmentIconView;
		[SerializeField, Required] private Button _button;
		[SerializeField] private int _slotId;

		/// <summary>
		/// Triggered when the button is clicked and passing the <see cref="UniqueId"/> of this item referencing the button
		/// </summary>
		public UnityEvent<UniqueId> OnClick = new UnityEvent<UniqueId>();

		private IGameDataProvider _dataProvider;
		
		/// <summary>
		/// Requests the <see cref="UniqueId"/> of this item
		/// </summary>
		public UniqueId ItemId { get; private set; }
		/// <summary>
		/// Requests the SlotId of this slot. 
		/// </summary>
		public bool IsFilled => ItemId != UniqueId.Invalid;

		private void Awake()
		{
			_dataProvider ??= MainInstaller.Resolve<IGameDataProvider>();

			_equipmentIconView.gameObject.SetActive(false);
			_button.onClick.AddListener(OnSlotClicked);
		}

		/// <summary>
		/// Sets the given info data for this view
		/// </summary>
		public void SetInfo(UniqueId itemId)
		{
			// Do nothing if the info is already set to the correct one
			if (ItemId == itemId && itemId != UniqueId.Invalid)
			{
				return;
			}
			
			ItemId = itemId;

			if (ItemId == UniqueId.Invalid)
			{
				_equipmentIconView.gameObject.SetActive(false);
				return;
			}
			
			var info = _dataProvider.EquipmentDataProvider.GetEquipmentInfo(itemId);
			
			_equipmentIconView.gameObject.SetActive(true);
			_equipmentIconView.SetInfo(info.DataInfo);
		}

		private void OnSlotClicked()
		{
			if (!IsFilled)
			{
				return;
			}
			
			SetInfo(UniqueId.Invalid);
			OnClick.Invoke(ItemId);
		}
	}
}


