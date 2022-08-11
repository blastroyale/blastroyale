using System;
using Codice.Utils;
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
			public UnityAction<string> ForgotPasswordClicked;
		}

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _passwordInputField;
		[SerializeField] private Button _goToRegisterButton;
		[SerializeField] private Button _goToDevRegisterButton;
		[SerializeField] private Button _loginButton;
		[SerializeField] private GameObject _frontDimBlocker;
		[SerializeField] private Button _forgotPasswordButton;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			
			_goToRegisterButton.onClick.AddListener(GoToRegisterClicked);
			_loginButton.onClick.AddListener(LoginClicked);

			_goToDevRegisterButton.onClick.AddListener(GoToDevRegisterClicked);
			_goToDevRegisterButton.gameObject.SetActive(Debug.isDebugBuild);
			_forgotPasswordButton.onClick.AddListener(GoToForgotYourPassword);
		}

		private void OnEnable()
		{
			SetFrontDimBlockerActive(false);
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
			Application.OpenURL(GameConstants.Links.MARKETPLACE_URL);
		}
		
		private void GoToDevRegisterClicked()
		{
			Data.GoToRegisterClicked();
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