using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Newtonsoft.Json;
using PlayFab;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the register screen
	/// </summary>
	public class ConnectFlgIdScreenPresenter : UIPresenterData<ConnectFlgIdScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action AuthLoginSuccess;
			public Action AuthRegisterSuccess;
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
		private ImageButton _closeButton;
		private Button _loginButton;
		private Button _registerButton;
		private Button _switchScreenButton;
		private Button _resetPasswordButton;
		private Label _switchScreenDesc;

		private IGameServices _services;

		private bool _showingRegisterScreen = false;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_loginPopupRoot = Root.Q("LoginPopup").Required();
			_registerPopupRoot = Root.Q("RegisterPopup").Required();
			_closeButton = Root.Q<ImageButton>("CloseButton").Required();
			_switchScreenButton = Root.Q<Button>("SwitchScreenButton").Required();
			_switchScreenDesc = Root.Q<Label>("SwitchScreenDesc").Required();
			_resetPasswordButton = Root.Q<Button>("ResetPasswordButton").Required();

			_loginButton = _loginPopupRoot.Q<Button>("LoginButton").Required();
			_loginEmailField = _loginPopupRoot.Q<TextField>("EmailTextField").Required();
			_loginPasswordField = _loginPopupRoot.Q<TextField>("PasswordTextField").Required();

			_registerButton = _registerPopupRoot.Q<Button>("RegisterButton").Required();
			_registerEmailField = _registerPopupRoot.Q<TextField>("EmailTextField").Required();
			_registerPasswordField = _registerPopupRoot.Q<TextField>("PasswordTextField").Required();
			_registerUsernameField = _registerPopupRoot.Q<TextField>("UsernameTextField").Required();

			_loginButton.clicked += LoginWithAccount;
			_registerButton.clicked += RegisterAttachAccountDetails;
			_switchScreenButton.clicked += SwitchScreen;
			_resetPasswordButton.clicked += OpenPasswordRecoveryPopup;
			_closeButton.clicked += OnCloseClicked;

			Root.SetupClicks(_services);

			ShowRegisterScreen();
		}

		private void SwitchScreen()
		{
			if (_showingRegisterScreen)
			{
				ShowLoginScreen();
			}
			else
			{
				ShowRegisterScreen();
			}
		}

		private void ShowLoginScreen()
		{
			_showingRegisterScreen = false;

			_loginPopupRoot.SetDisplay(true);
			_registerPopupRoot.SetDisplay(false);

			_switchScreenDesc.text = ScriptLocalization.UITLoginRegister.i_dont_have_account;
			_switchScreenButton.text = ScriptLocalization.UITLoginRegister.register;
		}

		private void ShowRegisterScreen()
		{
			_showingRegisterScreen = true;

			_loginPopupRoot.SetDisplay(false);
			_registerPopupRoot.SetDisplay(true);

			_switchScreenDesc.text = ScriptLocalization.UITLoginRegister.i_have_account;
			_switchScreenButton.text = ScriptLocalization.UITLoginRegister.login;
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
				_services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>().Forget();
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
		}

		private void OnRegisterSuccess(LoginData data)
		{
			var title = ScriptLocalization.UITSettings.success;
			var desc = ScriptLocalization.UITSettings.flg_id_connect_register_success;

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);

			_services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
			Data.AuthRegisterSuccess();
		}

		private string GetErrorString(PlayFabError error)
		{
			var realError = error.ErrorDetails?.Values.FirstOrDefault()?.FirstOrDefault();
			return realError ?? error.ErrorMessage;
		}

		void OnRegisterFail(PlayFabError error)
		{
			var title = ScriptLocalization.UITSettings.failure;
			var desc = string.Format(ScriptLocalization.UITSettings.flg_id_register_fail, GetErrorString(error));

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);
		}

		private void LoginClicked(string email, string password)
		{
			if (AuthenticationUtils.IsEmailFieldValid(email) && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				_services.UIService.OpenScreen<LoadingSpinnerScreenPresenter>().Forget();

				// We need to disconnect photon to re-authenticate and re-generate an auth token
				// when logging in with a different user
				_services.NetworkService.DisconnectPhoton();
				_services.AuthenticationService.LoginWithEmail(email, password, OnLoginSuccess, OnLoginFail, true);
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

		void OnLoginSuccess(LoginData data)
		{
			var title = ScriptLocalization.UITSettings.success;
			var desc = ScriptLocalization.UITSettings.flg_id_connect_login_success;

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);

			_services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
			Data.AuthLoginSuccess();
		}

		void OnLoginFail(PlayFabError error)
		{
			var title = ScriptLocalization.UITSettings.failure;
			var desc = string.Format(ScriptLocalization.UITSettings.flg_id_login_fail, GetErrorString(error));

			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = _services.GenericDialogService.CloseDialog
			};
			_loginPasswordField.value = "";
			_services.UIService.CloseScreen<LoadingSpinnerScreenPresenter>();
			_services.GenericDialogService.OpenButtonDialog(title, desc, false, confirmButton);
		}

		private void OpenPasswordRecoveryPopup()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.UITLoginRegister.reset_button,
				ButtonOnClick = (input) =>
				{
					_services.AuthenticationService.SendAccountRecoveryEmail(input, OnRecoveryEmailSuccess, OnRecoveryEmailError);
				}
			};

			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITLoginRegister.reset_password,
				ScriptLocalization.UITLoginRegister.reset_password_desc,
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
				ScriptLocalization.UITLoginRegister.reset_password_confirm, false,
				confirmButton);
		}

		private void OnRecoveryEmailError(PlayFabError error)
		{
			_services.AnalyticsService.ErrorsCalls.ReportError(AnalyticsCallsErrors.ErrorType.Login,
				GetErrorString(error));

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

			_services.GenericDialogService.OpenButtonDialog(ScriptLocalization.UITShared.error, GetErrorString(error),
				false, confirmButton);
		}
	}
}