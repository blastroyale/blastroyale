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
	public class RegisterScreenPresenter : AnimatedUiPresenterData<RegisterScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string,string,string> RegisterClicked;
			public Action GoToLoginClicked;
		}

		[SerializeField] private TMP_InputField _emailInputField;
		[SerializeField] private TMP_InputField _nameInputField;
		[SerializeField] private TMP_InputField _passwordInputField;

		[SerializeField] private Button _goToLoginButton;
		[SerializeField] private Button _registerButton;

		[SerializeField] private GameObject _frontDimBlocker;

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_frontDimBlocker.SetActive(active);
		}
		
		private void Awake()
		{
			_goToLoginButton.onClick.AddListener(GoToLoginClicked);
			_registerButton.onClick.AddListener(RegisterClicked);
		}

		private void OnEnable()
		{
			SetFrontDimBlockerActive(false);
		}

		private void RegisterClicked()
		{
			Data.RegisterClicked(_emailInputField.text, _nameInputField.text, _passwordInputField.text);
		}

		private void GoToLoginClicked()
		{
			Data.GoToLoginClicked();
		}
	}
}