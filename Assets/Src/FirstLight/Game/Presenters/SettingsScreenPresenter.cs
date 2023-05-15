using System;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using I2.Loc;
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

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private ImageButton _closeScreenButton;

		private Label _buildInfoLabel;
		private Button _faqButton;
		private Button _serverButton;
		private Button _logoutButton;
		private Button _deleteAccountButton;
		private Button _connectIdButton;
		private Label _accountStatusLabel;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements(VisualElement root)
		{
			_closeScreenButton = root.Q<ImageButton>("CloseButton");
			_closeScreenButton.clicked += Data.OnClose;

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
			SetupToggle(root.Q<LocalizedToggle>("CameraPanning").Required(),
				() => _gameDataProvider.AppDataProvider.UseDynamicCamera,
				val => _gameDataProvider.AppDataProvider.UseDynamicCamera = val);
			SetupToggle(root.Q<LocalizedToggle>("ScreenShake").Required(),
				() => _gameDataProvider.AppDataProvider.UseScreenShake,
				val => _gameDataProvider.AppDataProvider.UseScreenShake = val);

			SetupToggle(root.Q<Toggle>("AimBackground").Required(),
				() => _gameDataProvider.AppDataProvider.ConeAim,
				val => _gameDataProvider.AppDataProvider.ConeAim = val);

			// Graphics
			SetupRadioButtonGroup(root.Q<LocalizedRadioButtonGroup>("FPSRBG").Required(),
				() => _gameDataProvider.AppDataProvider.FpsTarget,
				val => _gameDataProvider.AppDataProvider.FpsTarget = val);
			SetupRadioButtonGroup(root.Q<LocalizedRadioButtonGroup>("GraphicsRBG").Required(),
				() => _gameDataProvider.AppDataProvider.CurrentDetailLevel,
				val => _gameDataProvider.AppDataProvider.CurrentDetailLevel = val);

			// Account
			_logoutButton = root.Q<Button>("LogoutButton");
			_logoutButton.clicked += OnLogoutClicked;
			_deleteAccountButton = root.Q<Button>("DeleteAccountButton");
			_deleteAccountButton.clicked += OnDeleteAccountClicked;
			_connectIdButton = root.Q<Button>("ConnectButton");
			_connectIdButton.clicked += Data.OnConnectIdClicked;
			_accountStatusLabel = root.Q<Label>("AccountStatusLabel");
			UpdateAccountStatus();

			// Footer buttons
			_faqButton = root.Q<Button>("FAQButton");
			_faqButton.clicked += _services.HelpdeskService.ShowFaq;
			_serverButton = root.Q<Button>("ServerButton");
			_serverButton.clicked += OpenServerSelect;

#if UNITY_IOS && !UNITY_EDITOR
			_faqButton.SetDisplay(false);
#endif

			root.SetupClicks(_services);
		}

		private void SetupToggle(Toggle toggle, Func<bool> getter, Action<bool> setter)
		{
			toggle.value = getter();
			toggle.RegisterCallback<ChangeEvent<bool>, Action<bool>>((e, s) =>
			{
				s(e.newValue);
				Save();
			}, setter);
		}

		private void SetupRadioButtonGroup<T>(RadioButtonGroup group, Func<T> getter, Action<T> setter)
			where T : Enum, IConvertible
		{
			var options = Enum.GetNames(typeof(T)); // TODO: Needs some sort of localization
			group.choices = options;
			group.value = EnumUtils.ToInt(getter());

			group.RegisterCallback<ChangeEvent<int>, Action<T>>((e, s) =>
			{
				s(EnumUtils.FromInt<T>(e.newValue));
				Save();
			}, setter);
		}

		private void Save()
		{
			_services.DataSaver.SaveData<AppData>();
		}

		public void UpdateAccountStatus()
		{
			if (_gameDataProvider.AppDataProvider.IsGuest)
			{
				_connectIdButton.SetDisplay(true);
				_deleteAccountButton.SetDisplay(false);
				_logoutButton.SetDisplay(false);
				_accountStatusLabel.text = ScriptLocalization.UITSettings.flg_id_not_connected;
			}
			else
			{
				_connectIdButton.SetDisplay(false);
				_deleteAccountButton.SetDisplay(true);
				_logoutButton.SetDisplay(true);
				_accountStatusLabel.text = string.Format(ScriptLocalization.UITSettings.flg_id_connected,
					_gameDataProvider.AppDataProvider.DisplayName.Value);
			}
		}
		
		
		private void OpenServerSelect()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			Data.OnServerSelectClicked();
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