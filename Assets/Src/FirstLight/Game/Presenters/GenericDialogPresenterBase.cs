using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Creates a Generic Dialog for showing information to the player usually for yes / no choices.
	/// Usually used for pointing out something informative to players.
	/// </summary>
	public abstract class GenericDialogPresenterBase :  UiToolkitPresenter
	{
		private Label _titleLabel;
		private Label _descLabel;
		private Button _confirmButton;
		private Button _cancelButton;
		private Button _blockerButton;
		
		private Action _closeCallback;
		private Action _confirmCallback;

		public string Title => _titleLabel?.text; 

		private void CloseRequested()
		{
			Close(false);
		}
		
		protected override void Close(bool destroy)
		{
			// TODO - check if IsOpenedComplete check needs to be added to prevent "closing too early" edge cases
			base.Close(destroy);
				
			_confirmButton.clicked -= CloseRequested;
			_cancelButton.clicked -= CloseRequested;
			_blockerButton.clicked -= CloseRequested;
			_confirmButton.clicked -= _confirmCallback;
		}

		protected override UniTask OnClosed()
		{
			_closeCallback?.Invoke();
			return base.OnClosed();
		}

		protected override void QueryElements(VisualElement root)
		{
			_titleLabel = root.Q<Label>("Title").Required();
			_descLabel = root.Q<Label>("Desc").Required();

			_confirmButton = root.Q<Button>("ConfirmButton").Required();
			_cancelButton = root.Q<Button>("CancelButton").Required();
			_blockerButton = root.Q<Button>("BlockerButton").Required();

			_confirmCallback = null;
			_closeCallback = null;
		}

		protected void SetBaseInfo(string title, string desc, bool showCloseButton, GenericDialogButton button, Action closeCallback)
		{
			_titleLabel.text = title;
			_descLabel.text = desc;
			_closeCallback = closeCallback;
			_cancelButton.SetDisplay(showCloseButton);
			
			if (button.IsEmpty)
			{
				_confirmButton.SetDisplay(false);
			}
			else
			{
				_confirmCallback = button.ButtonOnClick;
				_confirmButton.text = button.ButtonText;

				_confirmButton.SetDisplay(true);
				_confirmButton.clicked += _confirmCallback;
				_confirmButton.clicked += CloseRequested;
			}

			if (showCloseButton)
			{
				_cancelButton.clicked += CloseRequested;
				_blockerButton.clicked += CloseRequested;
			}
		}
	}
}