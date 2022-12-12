using System;
using FirstLight.Game.Infos;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles the scrapping content on the equipment popup
	/// </summary>
	public class EquipmentPopupScrapView : IUIView
	{
		private Label _amount;
		private VisualElement _buttonsContainer;
		private VisualElement _requirements;
		
		private LocalizedButton _scrapButton;
		private LocalizedButton _cancelButton;

		private Action _confirmAction;
		private Action _cancelAction;

		public void Attached(VisualElement element)
		{
			_amount = element.Q<Label>("Amount").Required();
			_buttonsContainer = element.Q<VisualElement>("ButtonContainer").Required();
			_requirements = element.Q<VisualElement>("Requirements").Required();
			
			element.Q<LocalizedButton>("ScrapButton").clicked += () => _confirmAction();
			element.Q<LocalizedButton>("CancelButton").clicked += () => _cancelAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction, Action cancelAction)
		{
			_amount.text = info.ScrappingValue.Value.ToString();
			_buttonsContainer.SetVisibility(!info.IsNft);

			// TODO - Adjust desired behavior when calculations are correct client side and can be displayed
			//_requirements.SetDisplay(true);
			_requirements.SetDisplay(false);
			
			_confirmAction = confirmAction;
			_cancelAction = cancelAction;
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}
	}
}