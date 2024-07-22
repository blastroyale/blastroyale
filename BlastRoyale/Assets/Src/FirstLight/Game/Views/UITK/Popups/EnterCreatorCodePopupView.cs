using System;
using FirstLight.Game.UIElements;
using FirstLight.UIService;
using QuickEye.UIToolkit;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Allows the user to support a content creator using their creator code.
	/// </summary>
	public class EnterCreatorCodePopupView : UIView
	{
		[Q("CreatorCodeInput")] private LocalizedTextField _creatorCodeInput;
		[Q("SupportButton")] private LocalizedButton _supportButton;

		private readonly Action<string> _onSupport;

		public EnterCreatorCodePopupView(Action<string> onSupport)
		{
            _onSupport = onSupport;
		}

		protected override void Attached()
		{
			Element.AssignQueryResults(this);
			
            _supportButton.clicked += () => _onSupport?.Invoke(_creatorCodeInput.value);
		}
	}
}