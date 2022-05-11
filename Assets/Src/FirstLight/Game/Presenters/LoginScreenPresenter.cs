using System;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using I2.Loc;
using Quantum;
using TMPro;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class LoginScreenPresenter : AnimatedUiPresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string,string> LoginClicked;
			public Action GoToRegisterClicked;
		}

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _passwordInputField;

		[SerializeField] private GameObject _registerRootObject;
		[SerializeField] private Button _goToRegisterButton;
		[SerializeField] private Button _loginButton;
		
		[SerializeField] private GameObject _frontDimBlocker;
		
		private void Awake()
		{
			_registerRootObject.SetActive(Debug.isDebugBuild);
			_goToRegisterButton.onClick.AddListener(GoToRegisterClicked);
			_loginButton.onClick.AddListener(LoginClicked);
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
		}
	}
}