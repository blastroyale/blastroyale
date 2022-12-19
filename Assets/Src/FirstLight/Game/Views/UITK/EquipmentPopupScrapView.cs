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
		private VisualElement _requirements;
		private VisualElement _areYouSureLabel;
		private VisualElement _bottomFiller;
		
		private LocalizedButton _scrapButton;

		private Action _confirmAction;

		public void Attached(VisualElement element)
		{
			_amount = element.Q<Label>("Amount").Required();
			_requirements = element.Q<VisualElement>("Requirements").Required();
			_areYouSureLabel = element.Q<VisualElement>("AreYouSureLabel").Required();
			_bottomFiller = element.Q<VisualElement>("BottomFiller").Required();
			
			_scrapButton = element.Q<LocalizedButton>("ScrapButton").Required();
			_scrapButton.clicked += () => _confirmAction();
		}

		public void SetData(EquipmentInfo info, Action confirmAction)
		{
			_amount.text = info.ScrappingValue.Value.ToString();
			_scrapButton.SetVisibility(!info.IsNft);

			// TODO - Adjust desired behavior when calculations are correct client side and can be displayed
			//_requirements.SetDisplay(true);
			_requirements.SetDisplay(!info.IsNft);
			_areYouSureLabel.SetVisibility(!info.IsNft);
			_bottomFiller.SetDisplay(info.IsNft);
			
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