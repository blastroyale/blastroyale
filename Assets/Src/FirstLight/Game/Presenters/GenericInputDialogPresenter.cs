using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Modules.UIService.Runtime;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	[UILayer(UIService.UIService.UILayer.Popup)]
	public class GenericInputDialogPresenter : GenericDialogPresenterBase
	{
		private GenericDialogButton<string> _confirmButton;
		private Action<string> _closeCallback;
		private TextField _inputField;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_inputField = Root.Q<TextField>().Required();

			_closeCallback = null;
			_confirmButton = new GenericDialogButton<string>();

			Root.SetupClicks(_services);
		}

		/// <summary>
		/// Shows the input text field
		/// If defined can call the <paramref name="closeCallback"/> when the Dialog is closed.
		/// </summary>
		public void SetInfo(string title, string desc, string initialInputText, GenericDialogButton<string> button,
							bool showCloseButton, Action<string> closeCallback = null,
							TouchScreenKeyboardType keyboardType = TouchScreenKeyboardType.Default)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = button.ButtonText,
				ButtonOnClick = OnConfirmButtonClicked
			};

			// TODO: this is critical. Makes me sad. Reimplmenet
			//_inputField.contentType = contentType;
			_inputField.value = initialInputText;
			_confirmButton = button;
			_closeCallback = closeCallback;
			_inputField.keyboardType = keyboardType;

			SetBaseInfo(title, desc, showCloseButton, confirmButton, OnCloseButtonClicked);
		}

		private void OnConfirmButtonClicked()
		{
			_confirmButton.ButtonOnClick(_inputField.text);
		}

		private void OnCloseButtonClicked()
		{
			_closeCallback?.Invoke(_inputField.text);
		}
	}
}