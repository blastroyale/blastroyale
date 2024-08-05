using System;
using FirstLight.Game.UIElements;
using FirstLight.UIService;
using QuickEye.UIToolkit;

namespace FirstLight.Game.Views.UITK.Popups
{
	/// <summary>
	/// Allows the user to join a game using a code.
	/// </summary>
	public class JoinWithCodePopupView : UIView
	{
		[Q("CodeInput")] private LocalizedTextField _codeInput;
		[Q("JoinButton")] private LocalizedButton _joinButton;

		private readonly Action<string> _onJoin;

		public JoinWithCodePopupView(Action<string> onJoin)
		{
			_onJoin = onJoin;
		}

		protected override void Attached()
		{
			Element.AssignQueryResults(this);
			
			_joinButton.clicked += () => _onJoin.Invoke(_codeInput.value);
		}
	}
}