using System;
using System.Linq;
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
			public Action OnCustomizeHudClicked;
			public Action OnDeleteAccountClicked;
		}

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private ImageButton _closeScreenButton;

		private Label _buildInfoLabel;
		private Button _faqButton;
		private Button _serverButton;
		private Button _customizeHudButton;
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
			//TODO: enable when hooked up to floating joystick logic
			root.Q<LocalizedToggle>("DynamicJoystick").SetDisplay(false);
			//SetupToggle(root.Q<LocalizedToggle>("DynamicJoystick").Required(),
			//	() => _gameDataProvider.AppDataProvider.UseDynamicJoystick,
			//	val => _gameDataProvider.AppDataProvider.UseDynamicJoystick = val);
			
			SetupToggle(root.Q<LocalizedToggle>("HapticFeedback").Required(),
				() => _gameDataProvider.AppDataProvider.IsHapticOn,
				val => _gameDataProvider.AppDataProvider.IsHapticOn = val);

			//root.Q<LocalizedToggle>("InvertSpecialCancelling").SetDisplay(false);
			SetupToggle(root.Q<LocalizedToggle>("InvertSpecialCancelling").Required(),
				() => _gameDataProvider.AppDataProvider.InvertSpecialCancellling,
				val => _gameDataProvider.AppDataProvider.InvertSpecialCancellling = val);

			SetupToggle(root.Q<LocalizedToggle>("ScreenShake").Required(),
				() => _gameDataProvider.AppDataProvider.UseScreenShake,
				val => _gameDataProvider.AppDataProvider.UseScreenShake = val);
			
			SetupToggle(root.Q<LocalizedToggle>("ShowRealDamage").Required(),
				() => _gameDataProvider.AppDataProvider.ShowRealDamage,
				val => _gameDataProvider.AppDataProvider.ShowRealDamage = val);

			SetupToggle(root.Q<Toggle>("AimBackground").Required(),
				() => _gameDataProvider.AppDataProvider.ConeAim,
				val => _gameDataProvider.AppDataProvider.ConeAim = val);

			// Graphics
			SetupRadioButtonGroup(root.Q<LocalizedRadioButtonGroup>("FPSRBG").Required(),
				() => _gameDataProvider.AppDataProvider.FpsTarget,
				val => _gameDataProvider.AppDataProvider.FpsTarget = val,
				FpsTarget.Normal, FpsTarget.High);
			SetupToggle(root.Q<Toggle>("UseOverheadUI").Required(),
				() => _gameDataProvider.AppDataProvider.UseOverheadUI,
				val => _gameDataProvider.AppDataProvider.UseOverheadUI = val);

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
			_customizeHudButton = root.Q<Button>("CustomizeHud");
			_customizeHudButton.clicked += OpenCustomizeHud;
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

		private void OpenCustomizeHud()
		{
			Data.OnCustomizeHudClicked();
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
