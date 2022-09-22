using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using UnityEngine;
using TMPro;
using UnityEngine.Events;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the login screen
	/// </summary>
	public class LoginScreenPresenter : AnimatedUiPresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string, string> LoginClicked;
			public Action GoToRegisterClicked;
			public Action PlayAsGuestClicked;
			public UnityAction<string> ForgotPasswordClicked;
		}

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _passwordInputField;
		[SerializeField] private Button _goToRegisterButton;
		[SerializeField] private Button _loginButton;
		[SerializeField] private Button _playAsGuestButton;
		[SerializeField] private GameObject _frontDimBlocker;
		[SerializeField] private Button _forgotPasswordButton;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_loginButton.onClick.AddListener(LoginClicked);
			_goToRegisterButton.onClick.AddListener(GoToRegisterClicked);
			_playAsGuestButton.onClick.AddListener(PlayAsGuestClicked);
			_forgotPasswordButton.onClick.AddListener(GoToForgotYourPassword);
		}

		private void OnEnable()
		{
			SetFrontDimBlockerActive(false);
		}
		
		protected override void OnOpened()
		{
			base.OnOpened();
			
			_playAsGuestButton.gameObject.SetActive(true);
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_frontDimBlocker.SetActive(active);
		}

		private void LoginClicked()
		{
			Data.LoginClicked(_emailInputField.text, _passwordInputField.text);
		}

		private void GoToRegisterClicked()
		{
			Data.GoToRegisterClicked();
		}
		
		private void PlayAsGuestClicked()
		{
			_playAsGuestButton.gameObject.SetActive(false);
			Data.PlayAsGuestClicked();
		}

		private void GoToForgotYourPassword()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = Data.ForgotPasswordClicked
			};
			
			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.SendPasswordEmail, 
			                                                    "", confirmButton, true);
		}
	}
}