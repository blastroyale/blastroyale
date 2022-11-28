using System;
using FirstLight.Game.Infos;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the repairing content on the equipment popup
	/// </summary>
	public class EquipmentPopupRepairView : IUIView
	{
		private const string DURABILITY_AMOUNT = "{0}/{1}";

		private VisualElement _durabilityBar;
		private Label _durabilityAmount;
		private Label _durabilityPlusAmount;
		private LocalizedButton _repairButton;

		private Action _confirmAction;

		public void Attached(VisualElement element)
		{
			_durabilityBar = element.Q<VisualElement>("DurabilityProgress").Required();
			_durabilityAmount = element.Q<Label>("DurabilityAmount").Required();
			_durabilityPlusAmount = element.Q<Label>("DurabilityAmount").Required();
			_repairButton = element.Q<LocalizedButton>("RepairButton").Required();

			_repairButton.clicked += () => _confirmAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction)
		{
			_durabilityAmount.text =
				string.Format(DURABILITY_AMOUNT, info.Equipment.Durability, info.Equipment.MaxDurability);
			_durabilityBar.style.flexGrow = info.Equipment.Durability / info.Equipment.MaxDurability;
			
			_repairButton.SetVisibility(!info.IsNft);

			_confirmAction = confirmAction;
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}