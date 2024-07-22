using System;
using FirstLight.Game.UIElements;
using FirstLight.UIService;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Generic information Popup
	/// </summary>
	public class GenericInfoPopupView : UIView
	{
        [Q("InfoText")] private Label _infoTextLabel;
		[Q("OkButton")] private LocalizedButton _okButton;

        private readonly string _infoText;
        private readonly Action _onOkButton;
        
		public GenericInfoPopupView(string infoText, Action onOkButton)
        {
            _infoText = infoText;
            _onOkButton = onOkButton;
        }

		protected override void Attached()
		{
			Element.AssignQueryResults(this);

            _infoTextLabel.text = _infoText;
            _okButton.clicked += () => _onOkButton?.Invoke();
        }
	}
}