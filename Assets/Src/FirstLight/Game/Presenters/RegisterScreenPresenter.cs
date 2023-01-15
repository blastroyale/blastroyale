using System;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the register screen
	/// </summary>
	[LoadSynchronously]
	public class RegisterScreenPresenter : UiToolkitPresenterData<RegisterScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string, string, string> RegisterClicked;
			public Action GoToLoginClicked;
		}

		private TextField _emailField;
		private TextField _usernameField;
		private TextField _passwordField;
		private VisualElement _blockerElement;
		
		private IGameServices _services;
		
		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_emailField = root.Q<TextField>("EmailTextField").Required();
			_usernameField = root.Q<TextField>("UsernameTextField").Required();
			_passwordField = root.Q<TextField>("PasswordTextField").Required();

			_blockerElement = root.Q("Blocker").Required();
			
			root.Q<Button>("LoginFlgButton").clicked += OnLoginClicked;
			root.Q<Button>("RegisterButton").clicked += OnRegisterClicked;

			root.SetupClicks(_services);
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_blockerElement.EnableInClassList("blocker-hidden", !active);
		}

		private void OnLoginClicked()
		{
			Debug.LogWarning("LoginFlgButton clicked");
			Data.GoToLoginClicked();
		}

		private void OnRegisterClicked()
		{
			Data.RegisterClicked(_emailField.text.Trim(), _usernameField.text.Trim(), _passwordField.text.Trim());
		}
	}
}