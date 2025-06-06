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
using FirstLight.Game.Services.Authentication;
using FirstLight.UIService;
using PlayFab;
using Unity.Services.Authentication;
using UnityEngine;

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
			public Action OnDeleteAccountClicked;
		}

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private ImageButton _closeScreenButton;

		private Label _buildInfoLabel;
		private LocalizedButton _serverButton;
		private LocalizedButton _customizeHudButton;
		private LocalizedButton _logoutButton;
		private LocalizedButton _deleteAccountButton;
		private LocalizedButton _connectIdButton;
		private LocalizedButton _web3Button;
		private LocalizedButton _supportButton;
		private LocalizedLabel _accountStatusLabel;
		
		private TextField _playerIDField;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
		}

		protected override void QueryElements()
		{
			_closeScreenButton = Root.Q<ImageButton>("CloseButton").Required();
			_closeScreenButton.clicked += Data.OnClose;

			// Build Info Text
			_buildInfoLabel = Root.Q<Label>("BuildInfoLabel");
			_buildInfoLabel.text = VersionUtils.VersionInternal;

			var accountNotification = 	Root.Q("AccountNotification").Required();
			accountNotification.SetDisplay(_services.AuthService.SessionData.IsGuest);
			Root.Q("ConnectNotification").Required().SetDisplay(_services.AuthService.SessionData.IsGuest);

			// Sound
			SetupToggle(Root.Q<LocalizedToggle>("SoundEffects").Required(), _services.LocalPrefsService.IsSFXEnabled);
			SetupToggle(Root.Q<LocalizedToggle>("Announcer").Required(), _services.LocalPrefsService.IsDialogueEnabled);
			SetupToggle(Root.Q<LocalizedToggle>("BGMusic").Required(), _services.LocalPrefsService.IsBGMEnabled);

			// Controls
			SetupToggle(Root.Q<LocalizedToggle>("HapticFeedback").Required(), _services.LocalPrefsService.IsHapticsEnabled);
			SetupToggle(Root.Q<LocalizedToggle>("InvertSpecialCancelling").Required(), _services.LocalPrefsService.InvertSpecialCanceling);
			SetupToggle(Root.Q<LocalizedToggle>("ScreenShake").Required(), _services.LocalPrefsService.IsScreenShakeEnabled);
			SetupToggle(Root.Q<Toggle>("SwitchJoysticks").Required(), _services.LocalPrefsService.SwapJoysticks);

			SetupRadioButtonGroup(Root.Q<RadioButtonGroup>("PerformanceMode").Required(),
				() => _services.GameAppService.PerformanceManager.Mode,
				m => _services.GameAppService.PerformanceManager.UpdatePerformanceMode(m));

			_customizeHudButton = Root.Q<LocalizedButton>("CustomizeHud").Required();
			_customizeHudButton.clicked += OpenCustomizeHud;

			// Graphics
			SetupToggle(Root.Q<LocalizedToggle>("FPSLimit").Required(), _services.LocalPrefsService.IsFPSLimitEnabled);
			SetupToggle(Root.Q<Toggle>("UseOverheadUI").Required(), _services.LocalPrefsService.UseOverheadUI);
			SetupToggle(Root.Q<Toggle>("ShowLatency").Required(), _services.LocalPrefsService.ShowLatency);

			// Account
			_web3Button = Root.Q<LocalizedButton>("Web3Button").Required();
			_logoutButton = Root.Q<LocalizedButton>("LogoutButton");
			_logoutButton.clicked += OnLogoutClicked;
			_deleteAccountButton = Root.Q<LocalizedButton>("DeleteAccountButton");
			_deleteAccountButton.clicked += OnDeleteAccountClicked;
			_connectIdButton = Root.Q<LocalizedButton>("ConnectButton");
			_connectIdButton.clicked += Data.OnConnectIdClicked;
			_accountStatusLabel = Root.Q<LocalizedLabel>("AccountStatusLabel").Required();
			_playerIDField = Root.Q<TextField>("PlayerID").Required();
			UpdateAccountStatus();
			// Footer buttons
			_supportButton = Root.Q<LocalizedButton>("SupportButton").Required();
			_supportButton.clicked += OpenSupportService;
			_serverButton = Root.Q<LocalizedButton>("ServerButton").Required();
			_serverButton.clicked += OpenServerSelect;

			var link = Root.Q<LocalizedButton>("LinkButton").Required();
			link.SetDisplay(false);
			
			if (MainInstaller.ResolveWeb3().IsEnabled())
			{
				_web3Button.clicked += () => _services.UIService.OpenScreen<Web3PopupPresenter>().Forget();
			}
			else
			{
				_web3Button.SetDisplay(false);
			}
			Root.SetupClicks(_services);

			_services.AuthService.CanLinkToNativeAccount().ContinueWith(canUseNative =>
			{
				if (!canUseNative) return;
				accountNotification.SetDisplay(true);
				link.SetDisplay(true);
#if !UNITY_IOS
				link.LocalizationKey = link.text = "LINK TO GOOGLE PLAY";
#else
				link.LocalizationKey = link.text = "LINK TO APPLE ACCOUNT";
#endif
				link.clicked += () =>
				{
					_services.AuthService.TryToLinkNativeAccount().ContinueWith(() => { link.SetDisplay(false); }).Forget();
				};
			}).Forget();
		}
		
		
		
		private void SetupToggle(Toggle toggle, ObservableField<bool> observable)
		{
			toggle.value = observable.Value;
			toggle.RegisterCallback<ChangeEvent<bool>, ObservableField<bool>>((e, o) =>
			{
				o.Value = e.newValue;
				Save();
			}, observable);
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
			if (_services.AuthService.SessionData.IsGuest)
			{
				_connectIdButton.SetDisplay(true);
				_deleteAccountButton.SetDisplay(false);
				_logoutButton.SetDisplay(false);
				_accountStatusLabel.text = string.Format(ScriptLocalization.UITSettings.flg_id_not_connected);
			}
			else
			{
				_connectIdButton.SetDisplay(false);
				_deleteAccountButton.SetDisplay(true);
				_logoutButton.SetDisplay(true);
				_accountStatusLabel.text = string.Format(ScriptLocalization.UITSettings.flg_id_connected);
			}

			_playerIDField.value = AuthenticationService.Instance.PlayerId + "-" + PlayFabSettings.staticPlayer.PlayFabId;
		}

		private void OpenCustomizeHud()
		{
			_services.UIService.OpenScreen<HudCustomizationScreenPresenter>(new HudCustomizationScreenPresenter.StateData()
			{
				OnClose = () =>
				{
					_services.UIService.OpenScreen<SettingsScreenPresenter>(Data).Forget();
				},
				OnSave = e =>
				{
					_services.ControlsSetup.SaveControlsPositions(e);
					_services.UIService.OpenScreen<SettingsScreenPresenter>(Data).Forget();
				}
			}).Forget();
		}

		private void OpenSupportService()
		{
			_services.CustomerSupportService.OpenCustomerSupportTicketForm();
		}

		private void OpenServerSelect()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			_services.UIService.OpenScreen<ServerSelectScreenPresenter>().Forget();
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