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
	public class ConnectIdScreenPresenter : UiToolkitPresenterData<ConnectIdScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string, string, string> ConnectClicked;
			public Action BackClicked;
		}

		private TextField _emailField;
		private TextField _usernameField;
		private TextField _passwordField;
		private Button _viewHideButton;
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
			_viewHideButton = root.Q<Button>("ViewHideButton").Required();

			_blockerElement = root.Q("Blocker").Required();

			root.Q<Button>("RegisterButton").clicked += OnRegisterClicked;
			root.Q<Button>("BackButton").clicked += OnBackButtonClicked;
			_viewHideButton.clicked += OnViewHideClicked;

			root.SetupClicks(_services);
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_blockerElement.EnableInClassList("blocker-hidden", !active);
		}

		private void OnRegisterClicked()
		{
			Data.ConnectClicked(_emailField.text.Trim(), _usernameField.text.Trim(), _passwordField.text.Trim());
		}

		private void OnBackButtonClicked()
		{
			Data.BackClicked();
		}

		private void OnViewHideClicked()
		{
			_viewHideButton.ToggleInClassList("view-hide-button--show");
			_passwordField.isPasswordField = !_viewHideButton.ClassListContains("view-hide-button--show");
		}
	}
}