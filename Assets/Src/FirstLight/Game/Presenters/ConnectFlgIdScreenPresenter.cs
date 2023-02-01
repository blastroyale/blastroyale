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
			public Action AuthLoginFail;
			public Action AuthRegisterFail;
			public Action CloseClicked;
		}

		private VisualElement _loginPopupRoot;
		private VisualElement _registerPopupRoot;
		private TextField _loginEmailField;
		private TextField _loginPasswordField;
		private TextField _registerEmailField;
		private TextField _registerUsernameField;
		private TextField _registerPasswordField;
		private VisualElement _blockerElement;
		private Button _closeButton;
		private Button _loginButton;
		private Button _registerButton;
		private Button _goToLoginButton;
		private Button _goToRegisterButton;
		private Button _resetPasswordButton;
		
		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_blockerElement = root.Q("Blocker");
			_loginPopupRoot = root.Q("LoginPopup").Required();
			_registerPopupRoot = root.Q("RegisterPopup").Required();
			_loginButton = root.Q<Button>("LoginButton").Required();
			_closeButton = root.Q<Button>("CloseButton").Required();
			_registerButton = root.Q<Button>("RegisterButton").Required();
			_goToLoginButton = root.Q<Button>("GoToLoginButton").Required();
			_goToRegisterButton = root.Q<Button>("GoToRegisterButton").Required();
			_resetPasswordButton = root.Q<Button>("ResetPasswordButton").Required();
			_loginEmailField = _loginPopupRoot.Q<TextField>("EmailTextField").Required();
			_loginPasswordField = _loginPopupRoot.Q<TextField>("PasswordTextField").Required();
			_registerEmailField = _registerPopupRoot.Q<TextField>("EmailTextField").Required();
			_registerPasswordField = _registerPopupRoot.Q<TextField>("PasswordTextField").Required();
			_registerUsernameField = _registerPopupRoot.Q<TextField>("UsernameTextField").Required();

			_loginButton.clicked += LoginWithAccount;
			_registerButton.clicked += RegisterAttachAccountDetails;
			_goToLoginButton.clicked += ShowLoginScreen;
			_goToRegisterButton.clicked += ShowRegisterScreen;
			_resetPasswordButton.clicked += OpenPasswordRecoveryPopup;
			_closeButton.clicked += OnCloseClicked;

			root.SetupClicks(_services);
			
			ShowLoginScreen();
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
			LoginClicked(_loginEmailField.text.Trim(), _loginPasswordField.text.Trim());
		}

		private void RegisterAttachAccountDetails()
		{
			RegisterClicked(_registerEmailField.text.Trim(), _registerUsernameField.text.Trim(), _registerPasswordField.text.Trim());
		}
		
		private void RegisterClicked(string email, string username, string password)
		{
			if (AuthenticationUtils.IsUsernameFieldValid(username)
			    && AuthenticationUtils.IsEmailFieldValid(email)
			    && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				_uiService.OpenUi<LoadingSpinnerScreenPresenter>();
				_services.AuthenticationService.AttachLoginDataToAccount(email, username, password, OnRegisterSuccess, OnRegisterFail);
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

			void OnRegisterSuccess(LoginData data)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};
				_services.GenericDialogService.OpenButtonDialog("Success", "Registration success.", false, confirmButton);
				
				_uiService.CloseUi<LoadingSpinnerScreenPresenter>();
				Data.AuthRegisterSuccess();
			}

			void OnRegisterFail(PlayFabError error)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};
				_services.GenericDialogService.OpenButtonDialog("Fail", "Registration fail.", false, confirmButton);
				
				_uiService.CloseUi<LoadingSpinnerScreenPresenter>();
				Data.AuthRegisterFail();
			}
		}
		
		private void LoginClicked(string email, string password)
		{
			if (AuthenticationUtils.IsEmailFieldValid(email) && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				_services.AuthenticationService.LoginWithEmail(email, password, OnLoginSuccess, OnLoginFail);
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
			
			void OnLoginSuccess(LoginData data)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};
				_services.GenericDialogService.OpenButtonDialog("Success", "Login success.", false, confirmButton);
				
				_uiService.CloseUi<LoadingSpinnerScreenPresenter>();
				Data.AuthLoginSuccess();
			}

			void OnLoginFail(PlayFabError error)
			{
				var confirmButton = new GenericDialogButton
				{
					ButtonText = ScriptLocalization.General.OK,
					ButtonOnClick = _services.GenericDialogService.CloseDialog
				};
				_services.GenericDialogService.OpenButtonDialog("Fail", "Login fail.", false, confirmButton);
				
				_uiService.CloseUi<LoadingSpinnerScreenPresenter>();
				Data.AuthLoginFail();
			}
		}

		private void OpenPasswordRecoveryPopup()
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

		private void OnCloseClicked()
		{
			Data.CloseClicked();
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