using System;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.UiService;
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

		protected override void QueryElements(VisualElement root)
		{
			_emailField = root.Q<TextField>("EmailTextField").Required();
			_usernameField = root.Q<TextField>("UsernameTextField").Required();
			_passwordField = root.Q<TextField>("PasswordTextField").Required();

			_blockerElement = root.Q("Blocker").Required();

			root.Q<Button>("LoginButton").clicked += OnLoginClicked;
			root.Q<Button>("RegisterButton").clicked += OnRegisterClicked;
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
			Data.GoToLoginClicked();
		}

		private void OnRegisterClicked()
		{
			Data.RegisterClicked(_emailField.text, _usernameField.text, _passwordField.text);
		}
	}
}