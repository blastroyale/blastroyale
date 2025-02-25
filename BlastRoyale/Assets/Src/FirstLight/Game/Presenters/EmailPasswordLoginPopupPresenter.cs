using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Logic;
using FirstLight.Game.Presenters.Store;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.UIElements.Kit;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using I2.Loc;
using Quantum;
using QuickEye.UIToolkit;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// Handles purchase confirmations
	/// </summary>
	[UILayer(UILayer.Popup)]
	public class EmailPasswordLoginPopupPresenter : UIPresenterResult<EmailPasswordLoginPopupPresenter.Result>
	{
		public class Result
		{
			public enum ResultType
			{
				Login,
				ResetPassword,
				Close
			}

			public string Email;
			public string Password;
			public ResultType ResultActionType;
		}

		[Q("EmailTextField")] public TextField _emailField;
		[Q("PasswordTextField")] public PasswordFieldElement _passwordField;
		[Q("ResetPasswordButton")] public LocalizedButton _forgotPassword;
		[Q("LoginButton")] public LocalizedButton _loginButton;
		[Q("Popup")] public GenericPopupElement _genericPopup;

		protected override void QueryElements()
		{
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_emailField.value = "";
			_passwordField.value = "";
			_genericPopup.CloseClicked += () =>
			{
				SetResult(new Result()
				{
					ResultActionType = Result.ResultType.Close
				});
			};
			_forgotPassword.clicked += OnForgotPasswordClicked;
			_loginButton.clicked += OnLoginButtonClicked;
			return _genericPopup.AnimateOpen();
		}

		protected override UniTask OnScreenClose()
		{
			return _genericPopup.AnimateClose();
		}

		private void LoginClicked(string email, string password)
		{
			if (AuthenticationUtils.IsEmailFieldValid(email) && AuthenticationUtils.IsPasswordFieldValid(password))
			{
				SetResult(new Result()
				{
					Email = email,
					Password = password,
					ResultActionType = Result.ResultType.Login,
				});
				return;
			}

			string errorMessage =
				LocalizationManager.TryGetTranslation("UITLoginRegister/invalid_input", out var translation)
					? translation
					: $"#{"UITLoginRegister/invalid_input"}#";
			MainInstaller.ResolveServices().InGameNotificationService.QueueNotification(errorMessage);
			DisableButtonAndEnableAgain().Forget();
		}

		public async UniTask DisableButtonAndEnableAgain()
		{
			this._loginButton.SetEnabled(false);
			await UniTask.Delay(TimeSpan.FromSeconds(2), cancellationToken: GetCancellationTokenOnClose());
			this._loginButton.SetEnabled(true);
		}

		private void OnLoginButtonClicked()
		{
			LoginClicked(this._emailField.value.Trim(), this._passwordField.value.Trim());
		}

		private void OnForgotPasswordClicked()
		{
			if (!AuthenticationUtils.IsEmailFieldValid(_emailField.value))
			{
				string errorMessage =
					LocalizationManager.TryGetTranslation("UITLoginRegister/invalid_input", out var translation)
						? translation
						: $"#{"UITLoginRegister/invalid_input"}#";
				MainInstaller.ResolveServices().InGameNotificationService.QueueNotification(errorMessage);
				return;
			}

			SetResult(new Result()
			{
				Email = _emailField.value,
				ResultActionType = Result.ResultType.ResetPassword,
			});
		}

		public void ClearPasswordField()
		{
			_passwordField.value = "";
		}
	}
}