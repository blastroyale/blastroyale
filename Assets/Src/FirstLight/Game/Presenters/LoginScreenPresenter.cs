using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the login screen
	/// </summary>
	[LoadSynchronously]
	public class LoginScreenPresenter : UiCloseActivePresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string, string> LoginClicked;
			public Action GoToRegisterClicked;
			public Action PlayAsGuestClicked;
			public UnityAction<string> ForgotPasswordClicked;
		}

		[SerializeField] private UIDocument _document;

		private VisualElement _root;

		private TextField _emailField;
		private TextField _passwordField;
		private VisualElement _blockerElement;

		private IGameServices _services;

		private void Start()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_root = _document.rootVisualElement.Q("root");

			_emailField = _root.Q<TextField>("EmailTextField");
			_passwordField = _root.Q<TextField>("PasswordTextField");
			_blockerElement = _root.Q("Blocker");

			_root.Q<Button>("LoginButton").clicked += OnLoginButtonClicked;
			_root.Q<Button>("RegisterButton").clicked += OnRegisterButtonClicked;
			_root.Q<Button>("ResetPasswordButton").clicked += OnResetPasswordButtonClicked;
			_root.Q<Button>("PlayAsGuestButton").clicked += OnPlayAsGuestButtonClicked;

			_root.EnableInClassList("hidden", false);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			if (_root == null) return; // First open

			_root.EnableInClassList("hidden", false);
		}

		protected override void OnClosed()
		{
			base.OnClosed();

			_root.EnableInClassList("hidden", true);
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_blockerElement.EnableInClassList("blocker-hidden", !active);
		}

		private void OnLoginButtonClicked()
		{
			Data.LoginClicked(_emailField.text, _passwordField.text);
		}

		private void OnRegisterButtonClicked()
		{
			Data.GoToRegisterClicked();
		}

		private void OnResetPasswordButtonClicked()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = Data.ForgotPasswordClicked
			};

			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.SendPasswordEmail,
				"", confirmButton, true);
		}

		private void OnPlayAsGuestButtonClicked()
		{
			Data.PlayAsGuestClicked();
		}
	}
}