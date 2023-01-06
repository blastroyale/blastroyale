using System;
using FirstLight.Game.Services;
using FirstLight.Game.Services.AnalyticsHelpers;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This presenter handles showing the login screen
	/// </summary>
	[LoadSynchronously]
	public class LoginScreenPresenter : UiToolkitPresenterData<LoginScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action<string, string> LoginClicked;
			public Action GoToRegisterClicked;
			public Action PlayAsGuestClicked;
			public UnityAction<string> ForgotPasswordClicked;
		}

		private VisualElement _root;
		private TextField _emailField;
		private TextField _passwordField;
		private VisualElement _blockerElement;
		private Button _viewHideButton;

		private bool _isPasswordHidden = true;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_root = root;
			_emailField = root.Q<TextField>("EmailTextField");
			_passwordField = root.Q<TextField>("PasswordTextField");
			_blockerElement = root.Q("Blocker");

			root.Q<Button>("LoginButton").clicked += OnLoginButtonClicked;
			root.Q<Button>("RegisterButton").clicked += OnRegisterButtonClicked;
			root.Q<Button>("ResetPasswordButton").clicked += OnResetPasswordButtonClicked;
			root.Q<Button>("PlayAsGuestButton").clicked += OnPlayAsGuestButtonClicked;
			_viewHideButton = root.Q<Button>("ViewHideButton").Required();
			_viewHideButton.clicked += OnViewHideClicked;

			root.SetupClicks(_services);
		}

		/// <summary>
		/// Hides the screen without closing it by setting hidden class on root
		/// </summary>
		public void Hide()
		{
			if (_root == null)
			{
				Debug.LogWarning("_root null");
				return;
			}

			_root.AddToClassList("hidden");
		}

		/// <summary>
		/// Removes hidden class on root
		/// </summary>
		public void Show()
		{
			if (_root == null)
			{
				Debug.LogWarning("_root null");
				return;
			}

			_root.RemoveFromClassList("hidden");
		}

		/// <summary>
		/// Sets the activity of the dimmed blocker image that covers the presenter
		/// </summary>
		public void SetFrontDimBlockerActive(bool active)
		{
			_blockerElement.EnableInClassList("blocker-hidden", !active);
		}

		private void OnLoginButtonClicked()
		{
			_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.Login);
			Data.LoginClicked(_emailField.text.Trim(), _passwordField.text.Trim());
		}

		private void OnRegisterButtonClicked()
		{
			Data.GoToRegisterClicked();
		}

		private void OnResetPasswordButtonClicked()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.UITShared.ok,
				ButtonOnClick = Data.ForgotPasswordClicked
			};

			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.UITLoginRegister.send_password_recovery,
				"", confirmButton, true);
		}

		private void OnPlayAsGuestButtonClicked()
		{
			_services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.PlayAsGuest);
			Data.PlayAsGuestClicked();
		}

		private void OnViewHideClicked()
		{
			_viewHideButton.ToggleInClassList("view-hide-button--show");
			_passwordField.isPasswordField = !_viewHideButton.ClassListContains("view-hide-button--show");
		}
	}
}