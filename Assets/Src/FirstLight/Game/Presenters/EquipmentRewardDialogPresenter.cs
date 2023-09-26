using System;
using FirstLight.Game.Ids;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Quantum;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class EquipmentRewardDialogPresenter : UiToolkitPresenterData<EquipmentRewardDialogPresenter.StateData>
	{
		public struct StateData
		{
			public Action ConfirmClicked;
			public Equipment Equipment;
		}
		
		private EquipmentCardElement _equipmentCard;
		private Button _confirmButton;

		protected override void QueryElements(VisualElement root)
		{
			_equipmentCard = root.Q<EquipmentCardElement>("EquipmentCard").Required();
			_confirmButton = root.Q<Button>("ConfirmButton").Required();
			
			_confirmButton.clicked += Data.ConfirmClicked;
		}

		/// <summary>
		/// Initializes the equipment card on the popup
		/// </summary>
		public void InitEquipment()
		{
			_equipmentCard.SetEquipment(Data.Equipment);
		}
	}
}

