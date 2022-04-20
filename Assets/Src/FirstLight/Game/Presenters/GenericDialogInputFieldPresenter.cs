using System;
using FirstLight.Game.Services;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace FirstLight.Game.Presenters
{
	/// <inheritdoc />
	public class GenericDialogInputFieldPresenter : GenericDialogPresenterBase
	{
		[SerializeField, Required] private TMP_InputField _textField;

		private GenericDialogButton<string> _confirmButton;
		private Action<string> _closeCallback;

		private void OnValidate()
		{
			_textField = _textField == null ? GetComponent<TMP_InputField>() : _textField;
		}
		
		/// <summary>
		/// Shows the input text field
		/// If defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public void SetInfo(string title, string initialInputText, GenericDialogButton<string> button, 
		                    bool showCloseButton, Action<string> closeCallback = null)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = button.ButtonText,
				ButtonOnClick = OnConfirmButtonClicked
			};
			
			_textField.text = initialInputText;
			_confirmButton = button;
			_closeCallback = closeCallback;
			
			SetBaseInfo(title, showCloseButton, confirmButton, OnDeclineButtonClicked);
		}

		private void OnConfirmButtonClicked()
		{
			_confirmButton.ButtonOnClick(_textField.text);
		}

		private void OnDeclineButtonClicked()
		{
			_closeCallback?.Invoke(_textField.text);
		}
	}
}

