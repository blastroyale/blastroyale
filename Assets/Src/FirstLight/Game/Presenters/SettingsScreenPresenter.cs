using System;
using System.Linq;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.UIElements;
using I2.Loc;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Cysharp.Threading.Tasks;
using FirstLight.UIService;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class SettingsScreenPresenter : UIPresenterData<SettingsScreenPresenter.StateData>
	{
		public class StateData
		{
			public Action LogoutClicked;
			public Action OnClose;
			public Action OnConnectIdClicked;
			public Action OnServerSelectClicked;
			public Action OnCustomizeHudClicked;
			public Action OnDeleteAccountClicked;
		}

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private ImageButton _closeScreenButton;

		private Label _buildInfoLabel;
		private Button _serverButton;
		private Button _customizeHudButton;
		private Button _logoutButton;
		private Button _deleteAccountButton;
		private Button _connectIdButton;
		private Button _web3Button;
		private Button _supportButton;
		private Label _accountStatusLabel;
		private Label _web3StatusLabel;
		private VisualElement _web3Notification;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_closeScreenButton = Root.Q<ImageButton>("CloseButton");
			_closeScreenButton.clicked += Data.OnClose;

			// Build Info Text
			_buildInfoLabel = Root.Q<Label>("BuildInfoLabel");
			_buildInfoLabel.text = VersionUtils.VersionInternal;

			Root.Q("AccountNotification").Required().SetDisplay(_services.AuthenticationService.IsGuest);
			Root.Q("ConnectNotification").Required().SetDisplay(_services.AuthenticationService.IsGuest);
			_web3Notification = Root.Q("ConnectWeb3Notification").Required();
			_web3Notification.SetDisplay(false);

			// Sound
			SetupToggle(Root.Q<LocalizedToggle>("SoundEffects").Required(),
				() => _gameDataProvider.AppDataProvider.IsSfxEnabled,
				val => _gameDataProvider.AppDataProvider.IsSfxEnabled = val);
			SetupToggle(Root.Q<LocalizedToggle>("Announcer").Required(),
				() => _gameDataProvider.AppDataProvider.IsDialogueEnabled,
				val => _gameDataProvider.AppDataProvider.IsDialogueEnabled = val);
			SetupToggle(Root.Q<LocalizedToggle>("BGMusic").Required(),
				() => _gameDataProvider.AppDataProvider.IsBgmEnabled,
				val => _gameDataProvider.AppDataProvider.IsBgmEnabled = val);

			// Controls
			SetupToggle(Root.Q<LocalizedToggle>("HapticFeedback").Required(),
				() => _gameDataProvider.AppDataProvider.IsHapticOn,
				val => _gameDataProvider.AppDataProvider.IsHapticOn = val);

			SetupToggle(Root.Q<LocalizedToggle>("InvertSpecialCancelling").Required(),
				() => _gameDataProvider.AppDataProvider.InvertSpecialCancellling,
				val => _gameDataProvider.AppDataProvider.InvertSpecialCancellling = val);

			SetupToggle(Root.Q<LocalizedToggle>("ScreenShake").Required(),
				() => _gameDataProvider.AppDataProvider.UseScreenShake,
				val => _gameDataProvider.AppDataProvider.UseScreenShake = val);

			SetupToggle(Root.Q<Toggle>("AimBackground").Required(),
				() => _gameDataProvider.AppDataProvider.ConeAim,
				val => _gameDataProvider.AppDataProvider.ConeAim = val);
			
			SetupToggle(Root.Q<Toggle>("SwitchJoysticks").Required(),
				() => _gameDataProvider.AppDataProvider.SwitchJoysticks,
				val => _gameDataProvider.AppDataProvider.SwitchJoysticks = val);

			_customizeHudButton = Root.Q<Button>("CustomizeHud").Required();
			_customizeHudButton.clicked += OpenCustomizeHud;

			// Graphics
			SetupRadioButtonGroup(Root.Q<LocalizedRadioButtonGroup>("FPSRBG").Required(),
				() => _gameDataProvider.AppDataProvider.FpsTarget,
				val => _gameDataProvider.AppDataProvider.FpsTarget = val,
				FpsTarget.Normal, FpsTarget.High);
			SetupToggle(Root.Q<Toggle>("UseOverheadUI").Required(),
				() => _gameDataProvider.AppDataProvider.UseOverheadUI,
				val => _gameDataProvider.AppDataProvider.UseOverheadUI = val);

			// Account
			_web3Button = Root.Q<Button>("Web3Button").Required();
			_logoutButton = Root.Q<Button>("LogoutButton");
			_logoutButton.clicked += OnLogoutClicked;
			_deleteAccountButton = Root.Q<Button>("DeleteAccountButton");
			_deleteAccountButton.clicked += OnDeleteAccountClicked;
			_connectIdButton = Root.Q<Button>("ConnectButton");
			_connectIdButton.clicked += Data.OnConnectIdClicked;
			_accountStatusLabel = Root.Q<Label>("AccountStatusLabel");
			_web3StatusLabel = Root.Q<Label>("Web3StatusLabel");
			UpdateAccountStatus();
			UpdateWeb3State(MainInstaller.ResolveWeb3().State);

			// Footer buttons
			_supportButton = Root.Q<Button>("SupportButton").Required();
			_supportButton.clicked += OpenSupportService;
			_serverButton = Root.Q<Button>("ServerButton").Required();
			_serverButton.clicked += OpenServerSelect;

			var web3 = MainInstaller.ResolveWeb3();
			_web3Button.clicked += () => web3.RequestLogin().Forget();
			_web3Button.SetEnabled(web3.State != Web3State.Unavailable);
			Root.SetupClicks(_services);
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			MainInstaller.ResolveWeb3().OnStateChanged += UpdateWeb3State;
			return base.OnScreenOpen(reload);
		}

		protected override UniTask OnScreenClose()
		{
			MainInstaller.ResolveWeb3().OnStateChanged -= UpdateWeb3State;
			return base.OnScreenClose();
		}

		private void UpdateWeb3State(Web3State state)
		{
			var web3 = MainInstaller.ResolveWeb3();
			_web3StatusLabel.text = $"{state} {web3.Web3Account ?? ""}";
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

		private void SetupRadioButtonGroup<T>(RadioButtonGroup group, Func<T> getter, Action<T> setter, params T[] validValues)
			where T : Enum, IConvertible
		{
			var allowedValues = validValues;
			if (allowedValues.Length == 0)
			{
				allowedValues = Enum.GetValues(typeof(T))
					.Cast<T>()
					.ToArray();
			}

			var options = allowedValues.Select(v => Enum.GetName(typeof(T), v));
			// TODO: Needs some sort of localization
			group.choices = options;

			// If the allowed values change in the future set the first value of the list
			var currentValue = getter();
			if (!EnumUtils.IsValid(allowedValues, currentValue))
			{
				currentValue = allowedValues[0];
			}

			group.value = EnumUtils.ToInt(allowedValues, currentValue);
			group.RegisterCallback<ChangeEvent<int>, Action<T>>((e, s) =>
			{
				var value = Math.Max(0, e.newValue);
				s(EnumUtils.FromInt<T>(allowedValues, value));
				Save();
			}, setter);
		}

		private void Save()
		{
			_services.DataSaver.SaveData<AppData>();
		}

		public void UpdateAccountStatus()
		{
			if (_services.AuthenticationService.IsGuest)
			{
				_connectIdButton.SetDisplay(true);
				_deleteAccountButton.SetDisplay(false);
				_logoutButton.SetDisplay(false);
				_accountStatusLabel.text = string.Format(ScriptLocalization.UITSettings.flg_id_not_connected,
														 _gameDataProvider.AppDataProvider.DisplayName.Value);
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

		private void OpenCustomizeHud()
		{
			Data.OnCustomizeHudClicked();
		}

		private void OpenSupportService()
		{
			_services.CustomerSupportService.OpenCustomerSupportTicketForm();
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
