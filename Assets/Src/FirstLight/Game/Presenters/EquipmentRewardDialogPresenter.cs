using System;
using FirstLight.Game.Ids;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.UIService;
using Quantum;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	/// TODO: Is this still used?
	public class EquipmentRewardDialogPresenter : UIPresenterData<EquipmentRewardDialogPresenter.StateData>
	{
		public class StateData
		{
			public Action ConfirmClicked;
			public Equipment Equipment;
		}
		
		private EquipmentCardElement _equipmentCard;
		private Button _confirmButton;

		protected override void QueryElements()
		{
			_equipmentCard = Root.Q<EquipmentCardElement>("EquipmentCard").Required();
			_confirmButton = Root.Q<Button>("ConfirmButton").Required();
			
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

