using System;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the register screen
	/// </summary>	
	[LoadSynchronously]
	public class ConnectFlgIdScreenPresenter : UiToolkitPresenterData<ConnectFlgIdScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action AuthLoginSuccess;
			public Action AuthRegisterSuccess;
			public Action AuthFail;
			public Action BackClicked;
		}

		private VisualElement _loginPopupRoot;
		private VisualElement _registerPopupRoot;
		private TextField _loginEmailField;
		private TextField _loginPasswordField;
		private TextField _registerEmailField;
		private TextField _registerUsernameField;
		private TextField _registerPasswordField;
		private VisualElement _blockerElement;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_loginPopupRoot = root.Q("LoginPopup").Required();
			_registerPopupRoot = root.Q("RegisterPopup").Required();
			_loginEmailField = _loginPopupRoot.Q<TextField>("EmailTextField").Required();
			_loginPasswordField = _loginPopupRoot.Q<TextField>("PasswordTextField").Required();
			_registerEmailField = _registerPopupRoot.Q<TextField>("EmailTextField").Required();
			_registerPasswordField = _registerPopupRoot.Q<TextField>("PasswordTextField").Required();
			_registerUsernameField = _registerPopupRoot.Q<TextField>("UsernameTextField").Required();
			_blockerElement = root.Q("Blocker");

			root.Q<Button>("LoginButton").clicked += LoginWithAccount;
			root.Q<Button>("RegisterButton").clicked += RegisterAttachAccountDetails;

			root.Q<Button>("GoToLoginButton").clicked += ShowLoginScreen;
			root.Q<Button>("GoToRegisterButton").clicked += ShowRegisterScreen;

			root.Q<Button>("ResetPasswordButton").clicked += OpenPasswordRecoveryPopup;
			root.Q<Button>("PlayAsGuestButton").clicked += ClosePresenter;

			root.SetupClicks(_services);
			
			ShowLoginScreen();
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_blockerElement.EnableInClassList("blocker-hidden", !active);
		}

		private void ShowLoginScreen()
		{
			_loginPopupRoot.SetDisplay(true);
			_registerPopupRoot.SetDisplay(false);
		}

		private void ShowRegisterScreen()
		{
			_loginPopupRoot.SetDisplay(false);
			_registerPopupRoot.SetDisplay(true);
		}

		private void LoginWithAccount()
		{
			//_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.Login);
			//Data.LoginClicked(_emailField.text.Trim(), _passwordField.text.Trim());
		}

		private void RegisterAttachAccountDetails()
		{
			//_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.Login);

			//Data.RegisterClicked(_emailField.text.Trim(), _usernameField.text.Trim(), _passwordField.text.Trim());
		}
		
		private void RegisterClicked(string email, string username, string password)
		{
			if (AuthenticationUtils.IsUsernameFieldValid(username)
			    && AuthenticationUtils.IsEmailFieldValid(email)
			    && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				//_services.AuthenticationService.AttachLoginDataToAccount(email, username, password, OnConnectIdComplete, OnConnectIdError);
			}
			else
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};

				string errorMessage =
					LocalizationManager.TryGetTranslation("UITLoginRegister/invalid_input", out var translation)
						? translation
						: $"#{"UITLoginRegister/invalid_input"}#";
				_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, errorMessage, false,
					confirmButton);
			}
		}
		
		private void LoginClicked(string email, string password)
		{
			if (AuthenticationUtils.IsEmailFieldValid(email) && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				//_services.AuthenticationService.LoginWithEmail(email, password, onSuccess, onError);
			}
			else
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};

				string errorMessage =
					LocalizationManager.TryGetTranslation("UITLoginRegister/invalid_input", out var translation)
						? translation
						: $"#{"UITLoginRegister/invalid_input"}#";
				_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, errorMessage, false,
					confirmButton);
			}
		}
		
		private void ClosePresenter()
		{
		}
		
		public void OpenPasswordRecoveryPopup()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = (input) =>
				{
					_services.AuthenticationService.SendAccountRecoveryEmail(input, OnRecoveryEmailSuccess, OnRecoveryEmailError);
				}
			};

			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.UITLoginRegister.send_password_recovery,
				_loginEmailField.value, confirmButton, true);
		}
		
		private void OnRecoveryEmailSuccess()
		{
			_services.GenericDialogService.CloseDialog();

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.MainMenu.SendPasswordEmailConfirm, false,
				confirmButton);
		}

		private void OnRecoveryEmailError(PlayFabError error)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login,
				error.ErrorMessage);

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = () =>
				{
					_services.GenericDialogService.CloseDialog();
					OpenPasswordRecoveryPopup();
				}
			};

			if (error.ErrorDetails != null)
			{
				FLog.Error("Authentication Fail - " + JsonConvert.SerializeObject(error.ErrorDetails));
			}

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, error.ErrorMessage,
				false, confirmButton);
		}
	}
}