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
		private const string DURABILITY_PLUS_AMOUNT = "+{0}";

		private const string UssRequirementsIconModifier = "requirements__icon--{0}";

		private VisualElement _durabilityBar;
		private Label _durabilityAmount;
		private Label _durabilityPlusAmount;
		private PriceButton _repairButton;
		private VisualElement _requirements;
		private Label _requirementsAmount;
		private VisualElement _requirementsIcon;

		private Action _confirmAction;

		public void Attached(VisualElement element)
		{
			_durabilityBar = element.Q<VisualElement>("DurabilityProgress").Required();
			_durabilityAmount = element.Q<Label>("DurabilityAmount").Required();
			_durabilityPlusAmount = element.Q<Label>("DurabilityPlusAmount").Required();
			_repairButton = element.Q<PriceButton>("RepairButton").Required();
			_requirements = element.Q<VisualElement>("Requirements").Required();
			_requirementsAmount = _requirements.Q<Label>("Amount").Required();
			_requirementsIcon = _requirements.Q<VisualElement>("Icon").Required();

			_repairButton.clicked += () => _confirmAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction, bool insufficient)
		{
			_durabilityAmount.text =
				string.Format(DURABILITY_AMOUNT, info.CurrentDurability, info.Equipment.MaxDurability);
			_durabilityBar.style.flexGrow = info.CurrentDurability / info.Equipment.MaxDurability;

			_durabilityPlusAmount.text = string.Format(DURABILITY_PLUS_AMOUNT,
				info.Equipment.MaxDurability - info.CurrentDurability);

			_repairButton.SetDisplay(!info.IsNft);
			_repairButton.SetPrice(info.RepairCost, insufficient);

			_requirements.SetDisplay(info.IsNft);
			_requirementsAmount.text = info.RepairCost.Value.ToString();
			_requirementsIcon.RemoveModifiers();
			_requirementsIcon.AddToClassList(string.Format(UssRequirementsIconModifier,
				info.RepairCost.Key.ToString().ToLowerInvariant()));

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