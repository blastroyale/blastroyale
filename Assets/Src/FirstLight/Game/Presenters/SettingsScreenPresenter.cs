using System;
using FirstLight.Game.Services;
using UnityEngine;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class SettingsScreenPresenter : UiToolkitPresenterData<SettingsScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action LogoutClicked;
			public Action OnClose;
			public Action OnConnectIdClicked;
			public Action OnServerSelectClicked;
			public Action OnDeleteAccountClicked;
		}

		[SerializeField, Required] private TextMeshProUGUI _versionText;
		[SerializeField, Required] private TextMeshProUGUI _selectedServerText;

		private ImageButton _closeScreenButton;
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private Label _buildInfoLabel;
		private Button _faqButton;
		private Button _serverButton;

		// Account Toggle
		private Button _logoutButton;
		private Button _deleteAccountButton;
		private Button _connectIdButton;
		private Label _connectionStatusLabel;
		private Label _connectionNameText;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider.AppDataProvider.ConnectionRegion.InvokeObserve(OnConnectionRegionChange);
		}

		protected override void QueryElements(VisualElement root)
		{
			_closeScreenButton = root.Q<ImageButton>("CloseButton");
			_closeScreenButton.clicked += OnCloseClicked;

			// Build Info Text
			_buildInfoLabel = root.Q<Label>("BuildInfoLabel");
			_buildInfoLabel.text = VersionUtils.VersionInternal;

			// Sound
			SetupToggle(root.Q<LocalizedToggle>("SoundEffects").Required(),
				() => _gameDataProvider.AppDataProvider.IsSfxEnabled,
				val => _gameDataProvider.AppDataProvider.IsSfxEnabled = val);
			SetupToggle(root.Q<LocalizedToggle>("Announcer").Required(),
				() => _gameDataProvider.AppDataProvider.IsDialogueEnabled,
				val => _gameDataProvider.AppDataProvider.IsDialogueEnabled = val);
			SetupToggle(root.Q<LocalizedToggle>("BGMusic").Required(),
				() => _gameDataProvider.AppDataProvider.IsBgmEnabled,
				val => _gameDataProvider.AppDataProvider.IsBgmEnabled = val);

			// Controls
			SetupToggle(root.Q<LocalizedToggle>("DynamicJoystick").Required(),
				() => _gameDataProvider.AppDataProvider.UseDynamicJoystick,
				val => _gameDataProvider.AppDataProvider.UseDynamicJoystick = val);
			SetupToggle(root.Q<LocalizedToggle>("HapticFeedback").Required(),
				() => _gameDataProvider.AppDataProvider.IsHapticOn,
				val => _gameDataProvider.AppDataProvider.IsHapticOn = val);

			// Graphics
			SetupRadioButtonGroup(root.Q<LocalizedRadioButtonGroup>("FPSRBG").Required(),
				() => _gameDataProvider.AppDataProvider.FpsTarget,
				val => _gameDataProvider.AppDataProvider.FpsTarget = val);
			SetupRadioButtonGroup(root.Q<LocalizedRadioButtonGroup>("GraphicsRBG").Required(),
				() => _gameDataProvider.AppDataProvider.CurrentDetailLevel,
				val => _gameDataProvider.AppDataProvider.CurrentDetailLevel = val);

			// Account Buttons
			_logoutButton = root.Q<Button>("LogoutButton");
			_logoutButton.clicked += OnLogoutClicked;
			_deleteAccountButton = root.Q<Button>("DeleteAccountButton");
			_deleteAccountButton.clicked += OnDeleteAccountClicked;
			_connectIdButton = root.Q<Button>("ConnectButton");
			_connectIdButton.clicked += OpenConnectId;
			_connectionNameText = root.Q<Label>("ConnectionNameLabel");
			_connectionStatusLabel = root.Q<Label>("ConnectionStatusLabel");
			UpdateAccountStatus();

			// Misc Buttons
			_faqButton = root.Q<Button>("FAQButton");
			_faqButton.clicked += OnFaqButtonPressed;
			_serverButton = root.Q<Button>("ServerButton");
			_serverButton.clicked += OpenServerSelect;

			root.SetupClicks(_services);
		}

		private static void SetupToggle(Toggle toggle, Func<bool> getter, Action<bool> setter)
		{
			toggle.value = getter();
			toggle.RegisterCallback<ChangeEvent<bool>, Action<bool>>((e, s) => s(e.newValue), setter);
		}

		private static void SetupRadioButtonGroup<T>(RadioButtonGroup group, Func<T> getter, Action<T> setter)
			where T : Enum, IConvertible
		{
			var options = Enum.GetNames(typeof(T)); // TODO: Needs some sort of localization
			group.choices = options;
			group.value = (int) (object) getter();

			group.RegisterCallback<ChangeEvent<int>, Action<T>>((e, s) => s((T) (object) e.newValue), setter);
		}

		private void OnCloseClicked()
		{
			Debug.Log("Close Clicked");
			_gameDataProvider?.AppDataProvider?.ConnectionRegion?.StopObserving(OnConnectionRegionChange);
			Data.OnClose();
		}

		public void UpdateAccountStatus()
		{
			if (_gameDataProvider.AppDataProvider.IsGuest)
			{
				_connectIdButton.SetDisplay(true);
				_connectionNameText.SetDisplay(false);
				_connectionStatusLabel.text = ScriptLocalization.UITSettings.flg_id_not_connected;
			}
			else
			{
				_connectIdButton.SetDisplay(false);
				_connectionNameText.SetDisplay(true);
				_connectionStatusLabel.text = ScriptLocalization.UITSettings.flg_id_connected;
				_connectionNameText.text = string.Format(ScriptLocalization.General.UserId,
					_gameDataProvider.AppDataProvider.DisplayName.Value);
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_versionText.text = VersionUtils.VersionInternal;

			var regionName = _gameDataProvider.AppDataProvider.ConnectionRegion.Value.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());

#if UNITY_IOS && !UNITY_EDITOR
			_faqButton.SetDisplay(false);
#endif
		}

		private void OnConnectionRegionChange(string previousValue, string newValue)
		{
			var regionName = newValue.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());
		}

		protected void OnClosedCompleted()
		{
			_gameDataProvider?.AppDataProvider?.ConnectionRegion?.StopObserving(OnConnectionRegionChange);
			Data.OnClose();
		}

		private void OpenConnectId()
		{
			Data.OnConnectIdClicked();
		}

		private void OpenServerSelect()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			Data.OnServerSelectClicked();
		}

		private void OnHelpdeskButtonPressed()
		{
			_services.HelpdeskService.StartConversation();
		}

		private void OnFaqButtonPressed()
		{
			_services.HelpdeskService.ShowFaq();
		}

		private void OnScreenshakeToggleChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.UseDynamicCamera = value;
		}

		private void OnLogoutClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			var title = ScriptLocalization.UITShared.confirmation;
			var desc = ScriptLocalization.UITSettings.logout_confirm_desc;
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITSettings.logout,
				ButtonOnClick = Data.LogoutClicked
			};

			_services.GenericDialogService.OpenButtonDialog(title, desc, true, confirmButton);
		}

		private void OnDeleteAccountClicked()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			var title = ScriptLocalization.UITShared.confirmation;
			var desc = ScriptLocalization.UITSettings.delete_account_request_desc;
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.UITSettings.delete_account,
				ButtonOnClick = Data.OnDeleteAccountClicked
			};

			_services.GenericDialogService.OpenButtonDialog(title, desc, true, confirmButton);
		}
	}
}