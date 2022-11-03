using System;
using System.Threading.Tasks;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Creates a Generic Dialog for showing information to the player usually for yes / no choices.
	/// Usually used for pointing out something informative to players.
	/// </summary>
	public abstract class GenericDialogPresenterBase :  UiToolkitPresenterData<GenericDialogPresenterBase.StateData>
	{
		public struct StateData
		{
		}

		private Label _titleLabel;
		private Label _descLabel;
		private Button _confirmButton;
		private Button _cancelButton;
		private Button _blockerButton;
		
		private Action _closeCallback;

		private void CloseRequested()
		{
			Close(false);
		}
		
		protected override void Close(bool destroy)
		{
			// TODO - check if removing this IsOpenedComplete breaks things
			//if (IsOpenedComplete)
			{
				base.Close(destroy);
			}
		}

		protected override Task OnClosed()
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
		}

		protected void SetBaseInfo(string title, string desc, bool showCloseButton, GenericDialogButton button, Action closeCallback)
		{
			_titleLabel.text = title;
			_closeCallback = closeCallback;

			if (button.IsEmpty)
			{
				_confirmButton.SetDisplayActive(false);
			}
			else
			{
				_confirmButton.text = button.ButtonText;

				_confirmButton.SetDisplayActive(true);
				_confirmButton.clicked += CloseRequested;
				_confirmButton.clicked += button.ButtonOnClick;
			}

			_cancelButton.SetDisplayActive(showCloseButton);
			
			if (showCloseButton)
			{
				_cancelButton.clicked += CloseRequested;
				_blockerButton.clicked += CloseRequested;
			}
		}
	}
}