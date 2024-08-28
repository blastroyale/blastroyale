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
	public class GenericConfirmPopupView : UIView
	{
        [Q("ConfirmText")] private Label _confirmTextLabel;
		[Q("ConfirmButton")] private LocalizedButton _confirmButton;
        [Q("CancelButton")] private LocalizedButton _cancelButton;

        private readonly string _confirmText;
        private readonly Action _onConfirmAction;
        private readonly Action _onCancelAction;
        
		public GenericConfirmPopupView(string confirmText, Action onConfirmAction, Action onCancelAction)
        {
            _confirmText = confirmText;
            _onConfirmAction = onConfirmAction;
            _onCancelAction = onCancelAction;
        }

		protected override void Attached()
		{
			Element.AssignQueryResults(this);

            _confirmTextLabel.text = _confirmText;
            _confirmButton.clicked += () => _onConfirmAction?.Invoke();
            _cancelButton.clicked += () => _onCancelAction?.Invoke();
        }
	}
}