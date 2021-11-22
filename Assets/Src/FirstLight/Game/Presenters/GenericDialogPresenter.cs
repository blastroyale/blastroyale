using System;
using FirstLight.Game.Services;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class GenericDialogPresenter : GenericDialogPresenterBase
	{
		/// <summary>
		/// Shows the Generic Dialog PopUp with the given <paramref name="title"/> and the <paramref name="button"/> information.
		/// If the given <paramref name="showCloseButton"/> is true, then will show the close button icon on the dialog.
		/// Optionally if defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public void SetInfo(string title, bool showCloseButton, GenericDialogButton button, Action closeCallback = null)
		{
			SetBaseInfo(title, showCloseButton, button, closeCallback);
		}
	}
}

