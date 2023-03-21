using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using Sirenix.OdinInspector;

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
			public Action OnDeleteAccountClicked;
		}

		[SerializeField, Required] private TextMeshProUGUI _versionText;
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private Button _blockerButton;
		[SerializeField, Required] private Button _logoutButton;
		[SerializeField, Required] private Button _deleteAccountButton;
		[SerializeField, Required] private UiToggleButtonView _backgroundMusicToggle;
		[SerializeField, Required] private UiToggleButtonView _sfxToggle;
		[SerializeField, Required] private UiToggleButtonView _dialogueToggle;
		[SerializeField, Required] private UiToggleButtonView _hapticToggle;
		[SerializeField, Required] private UiToggleButtonView _dynamicJoystickToggle;
		[SerializeField, Required] private UiToggleButtonView _dynamicCameraToggle;
		[SerializeField, Required] private UiToggleButtonView _screenshakeToggle;
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
			_services.PartyService.HasParty.InvokeObserve(OnHasPartyUpdate);
			_services.MatchmakingService.IsMatchmaking.InvokeObserve(OnIsMatchmakingUpdate);

			_closeButton.onClick.AddListener(OnClosedCompleted);
			_blockerButton.onClick.AddListener(OnBlockerButtonPressed);
			_logoutButton.onClick.AddListener(OnLogoutClicked);
			_deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);
			_connectIdButton.onClick.AddListener(OpenConnectId);
			_backgroundMusicToggle.onValueChanged.AddListener(OnBgmChanged);
			_sfxToggle.onValueChanged.AddListener(OnSfxChanged);
			_dialogueToggle.onValueChanged.AddListener(OnDialogueChanged);
			_hapticToggle.onValueChanged.AddListener(OnHapticChanged);
			_dynamicJoystickToggle.onValueChanged.AddListener(OnDynamicJoystickChanged);
			_dynamicCameraToggle.onValueChanged.AddListener(OnDynamicCameraChanged);
			_screenshakeToggle.onValueChanged.AddListener(OnScreenshakeToggleChanged);
			_highFpsToggle.onValueChanged.AddListener(OnHighFpsModeChanged);
			_detailLevelView.ValueChanged += OnDetailLevelChanged;
			_helpdesk.onClick.AddListener(OnHelpdeskButtonPressed);
			_faq.onClick.AddListener(OnFaqButtonPressed);
			_serverSelectButton.onClick.AddListener(OpenServerSelect);
		}

		private void OnHasPartyUpdate(bool _, bool __)
		{
			UpdateServerSelectButton();
		}
		
		private void OnIsMatchmakingUpdate(bool _, bool __)
		{
			UpdateServerSelectButton();
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			UpdateAccountStatus();

			_versionText.text = VersionUtils.VersionInternal;

			_backgroundMusicToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsBgmEnabled);
			_sfxToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsSfxEnabled);
			_dialogueToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsDialogueEnabled);
			_hapticToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsHapticOn);
			_dynamicJoystickToggle.SetInitialValue(_gameDataProvider.AppDataProvider.UseDynamicJoystick);
			_dynamicCameraToggle.SetInitialValue(_gameDataProvider.AppDataProvider.UseDynamicCamera);
			_highFpsToggle.SetInitialValue(_gameDataProvider.AppDataProvider.FpsTarget == GameConstants.Visuals.LOW_FPS_MODE_TARGET);
			_detailLevelView.SetSelectedDetailLevel(_gameDataProvider.AppDataProvider.CurrentDetailLevel);
			_logoutButton.gameObject.SetActive(true);

			UpdateServerSelectButton();


#if UNITY_IOS
			_faq.gameObject.SetActive(false);
#endif
		}

		private void UpdateServerSelectButton()
		{
			UpdateServerSelectButton(_gameDataProvider.AppDataProvider.ConnectionRegion.Value);
		}

		private void UpdateServerSelectButton(string connectionRegion)
		{
			if (_selectedServerText == null) return;
			var regionName = connectionRegion.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());
			if (_services.PartyService.HasParty.Value|| _services.MatchmakingService.IsMatchmaking.Value)
			{
				_serverSelectButton.interactable = false;
			}
			else
			{
				_serverSelectButton.interactable = true;
			}
		}

		/// <summary>
		/// Updates the FLG ID account status
		/// </summary>
		public void UpdateAccountStatus()
		{
			if (_gameDataProvider.AppDataProvider.IsGuest)
			{
				_connectIdButton.gameObject.SetActive(true);
				_idConnectionNameText.gameObject.SetActive(false);
				_idConnectionStatusText.text = ScriptLocalization.UITSettings.flg_id_not_connected;
			}
			else
			{
				_connectIdButton.gameObject.SetActive(false);
				_idConnectionNameText.gameObject.SetActive(true);
				_idConnectionStatusText.text = ScriptLocalization.UITSettings.flg_id_connected;
				_idConnectionNameText.text = string.Format(ScriptLocalization.General.UserId, _gameDataProvider.AppDataProvider.DisplayName.Value);
			}
		}

		private void OnConnectionRegionChange(string previousValue, string newValue)
		{
			UpdateServerSelectButton(newValue);
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

		private void OnDynamicCameraChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.UseDynamicCamera = value;
		}

		private void OnScreenshakeToggleChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.UseDynamicCamera = value;
		}

		private void OnHighFpsModeChanged(bool value)
		{
			var targetFps = value
				? GameConstants.Visuals.LOW_FPS_MODE_TARGET
				: GameConstants.Visuals.HIGH_FPS_MODE_TARGET;

			_gameDataProvider.AppDataProvider.FpsTarget = targetFps;
		}

		private void OnDetailLevelChanged(GraphicsConfig.DetailLevel detailLevel)
		{
			_gameDataProvider.AppDataProvider.CurrentDetailLevel = detailLevel;

			// This is temporary solution. When settings screen is made in UITK, the whole 
			_services.AudioFxService.PlayClip2D(AudioId.ButtonClickForward);
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

		private void OnBlockerButtonPressed()
		{
			Data.OnClose();
		}
	}
}