using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the login screen
	/// </summary>
	[LoadSynchronously]
	public class LoginScreenPresenter : UiToolkitPresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string, string> LoginClicked;
			public Action GoToRegisterClicked;
			public Action PlayAsGuestClicked;
			public UnityAction<string> ForgotPasswordClicked;
		}

		private TextField _emailField;
		private TextField _passwordField;
		private VisualElement _blockerElement;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_emailField = root.Q<TextField>("EmailTextField");
			_passwordField = root.Q<TextField>("PasswordTextField");
			_blockerElement = root.Q("Blocker");

			root.Q<Button>("LoginButton").clicked += OnLoginButtonClicked;
			root.Q<Button>("RegisterButton").clicked += OnRegisterButtonClicked;
			root.Q<Button>("ResetPasswordButton").clicked += OnResetPasswordButtonClicked;
			root.Q<Button>("PlayAsGuestButton").clicked += OnPlayAsGuestButtonClicked;
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