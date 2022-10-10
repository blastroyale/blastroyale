using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Sirenix.OdinInspector;
using UnityEngine.Events;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class SettingsScreenPresenter : AnimatedUiPresenterData<SettingsScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action LogoutClicked;
			public Action OnClose;
			public Action OnConnectIdClicked;
			public Action OnServerSelectClicked;
		}
		
		[SerializeField, Required] private TextMeshProUGUI _versionText;
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private Button _blockerButton;
		[SerializeField, Required] private Button _logoutButton;
		[SerializeField, Required] private UiToggleButtonView _backgroundMusicToggle;
		[SerializeField, Required] private UiToggleButtonView _sfxToggle;
		[SerializeField, Required] private UiToggleButtonView _dialogueToggle;
		[SerializeField, Required] private UiToggleButtonView _hapticToggle;
		[SerializeField, Required] private UiToggleButtonView _dynamicJoystickToggle;
		[SerializeField, Required] private UiToggleButtonView _highFpsToggle;
		[SerializeField, Required] private DetailLevelToggleView _detailLevelView;
		[SerializeField, Required] private Button _helpdesk;
		[SerializeField, Required] private Button _faq;
		[SerializeField, Required] private Button _serverSelectButton;
		[SerializeField, Required] private TextMeshProUGUI _selectedServerText;
		[SerializeField, Required] private Button _connectIdButton;
		[SerializeField, Required] private TextMeshProUGUI _idConnectionStatusText;
		[SerializeField, Required] private TextMeshProUGUI _idConnectionNameText;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_gameDataProvider.AppDataProvider.ConnectionRegion.InvokeObserve(OnConnectionRegionChange);

			_versionText.text = VersionUtils.VersionInternal;

			if (string.IsNullOrEmpty(_gameDataProvider.AppDataProvider.LastLoginEmail.Value))
			{
				_connectIdButton.gameObject.SetActive(true);
				_idConnectionNameText.gameObject.SetActive(false);
				_idConnectionStatusText.text = ScriptLocalization.MainMenu.FirstLightIdNeedConnection;
			}
			else
			{
				_connectIdButton.gameObject.SetActive(false);
				_idConnectionNameText.gameObject.SetActive(true);
				_idConnectionStatusText.text = ScriptLocalization.MainMenu.FirstLightIdConnected;
				_idConnectionNameText.text = string.Format(ScriptLocalization.General.UserId,
				                                           _gameDataProvider.AppDataProvider.DisplayName.Value);
			}

			_closeButton.onClick.AddListener(OnClosedCompleted);
			_blockerButton.onClick.AddListener(OnBlockerButtonPressed);
			_logoutButton.onClick.AddListener(OnLogoutClicked);
			_connectIdButton.onClick.AddListener(OpenConnectId);
			_backgroundMusicToggle.onValueChanged.AddListener(OnBgmChanged);
			_sfxToggle.onValueChanged.AddListener(OnSfxChanged);
			_dialogueToggle.onValueChanged.AddListener(OnDialogueChanged);
			_hapticToggle.onValueChanged.AddListener(OnHapticChanged);
			_dynamicJoystickToggle.onValueChanged.AddListener(OnDynamicJoystickChanged);
			_highFpsToggle.onValueChanged.AddListener(OnHighFpsModeChanged);
			_detailLevelView.ValueChanged += OnDetailLevelChanged;

			_backgroundMusicToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsBgmEnabled);
			_sfxToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsSfxEnabled);
			_dialogueToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsDialogueEnabled);
			_hapticToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsHapticOn);
			_dynamicJoystickToggle.SetInitialValue(_gameDataProvider.AppDataProvider.UseDynamicJoystick);
			_highFpsToggle.SetInitialValue(_gameDataProvider.AppDataProvider.UseHighFpsMode);
			_detailLevelView.SetSelectedDetailLevel(_gameDataProvider.AppDataProvider.CurrentDetailLevel);
			_blockerButton.onClick.AddListener(OnBlockerButtonPressed);
			_helpdesk.onClick.AddListener(OnHelpdeskButtonPressed);
			_faq.onClick.AddListener(OnFaqButtonPressed);
			_serverSelectButton.onClick.AddListener(OpenServerSelect);
			_logoutButton.gameObject.SetActive(FeatureFlags.EMAIL_AUTH);

			var regionName = _gameDataProvider.AppDataProvider.ConnectionRegion.Value.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());

#if UNITY_IOS
			_faq.gameObject.SetActive(false);
#endif
		}

		private void OnConnectionRegionChange(string previousValue, string newValue)
		{
			var regionName = newValue.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());
		}

		/// <inheritdoc />
		protected override void OnClosedCompleted()
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

		private void OnBgmChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsBgmEnabled = value;
		}

		private void OnSfxChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsSfxEnabled = value;
		}
		
		private void OnDialogueChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsDialogueEnabled = value;
		}

		private void OnHapticChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsHapticOn = value;
		}
		
		private void OnDynamicJoystickChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.UseDynamicJoystick = value;
		}
		
		private void OnHighFpsModeChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.UseHighFpsMode = value;
		}

		private void OnDetailLevelChanged(GraphicsConfig.DetailLevel detailLevel)
		{
			_gameDataProvider.AppDataProvider.CurrentDetailLevel = detailLevel;
		}
		
		private void OnLogoutClicked()
		{
			var title = string.Format(ScriptLocalization.MainMenu.LogoutConfirm);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = new UnityAction(Data.LogoutClicked)
			};

			_services.GenericDialogService.OpenDialog(title, true, confirmButton);
		}

		private void OnBlockerButtonPressed()
		{
			Data.OnClose();
		}
	}
}