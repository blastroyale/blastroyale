using System;
using UnityEngine;
using TMPro;
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
		}

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _passwordInputField;
		[SerializeField] private Button _goToRegisterButton;
		[SerializeField] private Button _goToDevRegisterButton;
		[SerializeField] private Button _loginButton;
		[SerializeField] private GameObject _frontDimBlocker;

		private void Awake()
		{
			_goToRegisterButton.onClick.AddListener(GoToRegisterClicked);
			_loginButton.onClick.AddListener(LoginClicked);

			_goToDevRegisterButton.onClick.AddListener(GoToDevRegisterClicked);
			_goToDevRegisterButton.gameObject.SetActive(Debug.isDebugBuild);
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
			Data.GoToRegisterClicked();
			
			if (Debug.isDebugBuild)
			{
				//The devs will need to know to replace the link with a real marketplace link once real market is open
				Application.OpenURL("http://flgmarketplacestorage.z33.web.core.windows.net/");
			}
		}
		
		private void GoToDevRegisterClicked()
		{
			Application.OpenURL("http://flgmarketplacestorage.z33.web.core.windows.net/");
		}
	}
}