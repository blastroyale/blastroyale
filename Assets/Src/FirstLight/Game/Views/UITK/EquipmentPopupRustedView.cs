using System;
using System.Collections.Generic;
using FirstLight.Game.Infos;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the rusted items content on the equipment popup
	/// </summary>
	public class EquipmentPopupRustedView : IUIView
	{
		private ScrollView _scrollView;

		private Action _goRepairAction;
		private Action _closeAction;

		public void Attached(VisualElement element)
		{
			_scrollView = element.Q<ScrollView>("ScrollView").Required();

			element.Q<LocalizedButton>("RepairButton").clicked += () => _goRepairAction();
			element.Q<LocalizedButton>("BackButton").clicked += () => _closeAction();
		}

		public void SetData(IEnumerable<EquipmentInfo> rustedEquipment, Action goRepairAction, Action closeAction)
		{
			_goRepairAction = goRepairAction;
			_closeAction = closeAction;

			_scrollView.Clear();

			foreach (var info in rustedEquipment)
			{
				_scrollView.Add(new EquipmentCardElement(info.Equipment));
			}
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}