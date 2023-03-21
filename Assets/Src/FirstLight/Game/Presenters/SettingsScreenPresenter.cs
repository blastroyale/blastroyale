using System;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
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
// using Button = UnityEngine.UI.Button;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class SettingsScreenPresenter : UiToolkitPresenterData<SettingsScreenPresenter.StateData> // AnimatedUiPresenterData<SettingsScreenPresenter.StateData>, 
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
		// [SerializeField, Required] private Button _closeButton;
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

		private ImageButton _closeScreenButton;
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		
		private LocalizedButton [] _localizedTabs;
		private VisualElement[] _localizedSelectors;
		private VisualElement[] _localizedContentBlocks;
		
		// Sound Toggle
		private Button _soundToggle;
		private Button [] _soundToggleButtons;
		private VisualElement[] _soundToggleLabels;
		
		// Announcer Toggle
		private Button _announcerToggle;
		private Button [] _announcerToggleButtons;
		private VisualElement[] _announcerToggleLabels;
		
		// BGM Toggle
		private Button _bgmToggle;
		private Button [] _bgmToggleButtons;
		private VisualElement[] _bgmToggleLabels;
		
		// Dynamic Controls Toggle
		private Button _dynamicStickToggle;
		private Button [] _dynamicStickToggleButtons;
		private VisualElement[] _dynamicStickToggleLabels;
		
		// Haptic Toggle
		private Button _hapticFeedbackToggle;
		private Button [] _hapticFeedbackToggleButtons;
		private VisualElement[] _hapticFeedbackToggleLabels;

		private const string UssSpriteSelected = "sprite-home__settings-tab-chosen";
		private const string UssSpriteUnselected = "sprite-home__settings-tab-back";

		private enum SETTINGS_TAB_CATEGORIES
		{
			SOUND = 0,
			CONTROLS = 1,
			GRAPHICS = 2,
			ACCOUNT = 3
		};

		private enum SETTINGS_TOGGLE_CATEGORIES
		{
			OFF = 0,
			ON = 1,
		};

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_gameDataProvider.AppDataProvider.ConnectionRegion.InvokeObserve(OnConnectionRegionChange);

			// _closeButton.onClick.AddListener(OnClosedCompleted);
			// _blockerButton.onClick.AddListener(OnBlockerButtonPressed);
			// _logoutButton.onClick.AddListener(OnLogoutClicked);
			// _deleteAccountButton.onClick.AddListener(OnDeleteAccountClicked);
			// _connectIdButton.onClick.AddListener(OpenConnectId);
			_backgroundMusicToggle.onValueChanged.AddListener(OnBgmChanged);
			_sfxToggle.onValueChanged.AddListener(OnSfxChanged);
			_dialogueToggle.onValueChanged.AddListener(OnDialogueChanged);
			_hapticToggle.onValueChanged.AddListener(OnHapticChanged);
			_dynamicJoystickToggle.onValueChanged.AddListener(OnDynamicJoystickChanged);
			_dynamicCameraToggle.onValueChanged.AddListener(OnDynamicCameraChanged);
			_screenshakeToggle.onValueChanged.AddListener(OnScreenshakeToggleChanged);
			_highFpsToggle.onValueChanged.AddListener(OnHighFpsModeChanged);
			// _detailLevelView.ValueChanged += OnDetailLevelChanged;
			// _helpdesk.onClick.AddListener(OnHelpdeskButtonPressed);
			// _faq.onClick.AddListener(OnFaqButtonPressed);
			// _serverSelectButton.onClick.AddListener(OpenServerSelect);

			int size = Enum.GetNames(typeof(SETTINGS_TAB_CATEGORIES)).Length;
			
			_localizedTabs = new LocalizedButton[size];
			_localizedSelectors = new VisualElement[size];
			_localizedContentBlocks = new VisualElement[size];
			
			size = Enum.GetNames(typeof(SETTINGS_TOGGLE_CATEGORIES)).Length;

			_soundToggleButtons = new Button[size];
			_soundToggleLabels = new VisualElement[size];
			
			_announcerToggleButtons = new Button[size];
			_announcerToggleLabels = new VisualElement[size];
			
			_bgmToggleButtons = new Button[size];
			_bgmToggleLabels = new VisualElement[size];

			_hapticFeedbackToggleButtons = new Button[size];
			_hapticFeedbackToggleLabels = new VisualElement[size];

			_dynamicStickToggleButtons = new Button[size];
			_dynamicStickToggleLabels = new VisualElement[size];
		}

		protected override void QueryElements(VisualElement root)
		{
			_closeScreenButton = root.Q<ImageButton>("CloseButton");
			_closeScreenButton.clicked += OnCloseClicked;

			// Tabs
			_localizedTabs[(int)SETTINGS_TAB_CATEGORIES.SOUND] = root.Q<LocalizedButton>("SoundTab");
			_localizedTabs[(int) SETTINGS_TAB_CATEGORIES.SOUND].clicked += OnSoundTabClicked;
			_localizedSelectors[(int)SETTINGS_TAB_CATEGORIES.SOUND] = root.Q<VisualElement>("SoundSelector");
			_localizedContentBlocks[(int)SETTINGS_TAB_CATEGORIES.SOUND] = root.Q<VisualElement>("SoundContent");
			
			_localizedTabs[(int)SETTINGS_TAB_CATEGORIES.GRAPHICS] = root.Q<LocalizedButton>("GraphicsTab");
			_localizedTabs[(int) SETTINGS_TAB_CATEGORIES.GRAPHICS].clicked += OnGraphicsClicked;
			_localizedSelectors[(int)SETTINGS_TAB_CATEGORIES.GRAPHICS] = root.Q<VisualElement>("GraphicsSelector");
			_localizedContentBlocks[(int)SETTINGS_TAB_CATEGORIES.GRAPHICS] = root.Q<VisualElement>("GraphicsContent");
			
			_localizedTabs[(int)SETTINGS_TAB_CATEGORIES.CONTROLS] = root.Q<LocalizedButton>("ControlsTab");
			_localizedTabs[(int) SETTINGS_TAB_CATEGORIES.CONTROLS].clicked += OnControlsClicked;
			_localizedSelectors[(int)SETTINGS_TAB_CATEGORIES.CONTROLS] = root.Q<VisualElement>("ControlsSelector");
			_localizedContentBlocks[(int)SETTINGS_TAB_CATEGORIES.CONTROLS] = root.Q<VisualElement>("ControlsContent");
			
			_localizedTabs[(int)SETTINGS_TAB_CATEGORIES.ACCOUNT] = root.Q<LocalizedButton>("AccountTab");
			_localizedTabs[(int) SETTINGS_TAB_CATEGORIES.ACCOUNT].clicked += OnAccountClicked;
			_localizedSelectors[(int)SETTINGS_TAB_CATEGORIES.ACCOUNT] = root.Q<VisualElement>("AccountSelector");
			_localizedContentBlocks[(int)SETTINGS_TAB_CATEGORIES.ACCOUNT] = root.Q<VisualElement>("AccountContent");
			
			// _localizedTabs[(int) SETTINGS_TAB_CATEGORIES.SOUND].RemoveSpriteClasses();
			// _localizedTabs[(int) SETTINGS_TAB_CATEGORIES.SOUND].AddToClassList(UssSpriteSelected);

			TabSelected(SETTINGS_TAB_CATEGORIES.SOUND);
			
			// Sound Settings
			_soundToggle = root.Q<Button>("SoundEffectsToggle");
			_soundToggle.clicked += OnSoundToggleClicked;
			_soundToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<Button>("SoundOffButton");
			_soundToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<Button>("SoundOnButton");
			_soundToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<VisualElement>("SoundOffLabel");
			_soundToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<VisualElement>("SoundOnLabel");
			
			// Announcer Settings
			_announcerToggle = root.Q<Button>("AnnouncerToggle");
			_announcerToggle.clicked += OnAnnouncerToggleClicked;
			_announcerToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<Button>("AnnouncerOffButton");
			_announcerToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<Button>("AnnouncerOnButton");
			_announcerToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<VisualElement>("AnnouncerOffLabel");
			_announcerToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<VisualElement>("AnnouncerOnLabel");
			
			// BGM Settings
			_bgmToggle = root.Q<Button>("BGMToggle");
			_bgmToggle.clicked += OnBGMToggleClicked;
			_bgmToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<Button>("BGMOffButton");
			_bgmToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<Button>("BGMOnButton");
			_bgmToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<VisualElement>("BGMOffLabel");
			_bgmToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<VisualElement>("BGMOnLabel");
			
			// Dynamic Joystick
			_dynamicStickToggle = root.Q<Button>("DynamicJoystickToggle");
			_dynamicStickToggle.clicked += OnDynamicJoystickToggleClicked;
			_dynamicStickToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<Button>("DynamicJoystickOffButton");
			_dynamicStickToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<Button>("DynamicJoystickOnButton");
			_dynamicStickToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<VisualElement>("DynamicJoystickOffLabel");
			_dynamicStickToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<VisualElement>("DynamicJoystickOnLabel");
			
			// Haptic Feedback
			_hapticFeedbackToggle = root.Q<Button>("HapticToggle");
			_hapticFeedbackToggle.clicked += OnHapticToggleClicked;
			_hapticFeedbackToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<Button>("HapticOffButton");
			_hapticFeedbackToggleButtons[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<Button>("HapticOnButton");
			_hapticFeedbackToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.OFF] = root.Q<VisualElement>("HapticOffLabel");
			_hapticFeedbackToggleLabels[(int)SETTINGS_TOGGLE_CATEGORIES.ON] = root.Q<VisualElement>("HapticOnLabel");
			
			if (_gameDataProvider.AppDataProvider.IsSfxEnabled)
			{
				OnSoundToggleOffClicked();
			}
			else
			{
				OnSoundToggleOnClicked();
			}
			
			if (_gameDataProvider.AppDataProvider.IsDialogueEnabled)
			{
				OnAnnouncerToggleOffClicked();
			}
			else
			{
				OnAnnouncerToggleOnClicked();
			}
			
			if (_gameDataProvider.AppDataProvider.IsBgmEnabled)
			{
				OnBGMToggleOffClicked();
			}
			else
			{
				OnBGMToggleOnClicked();
			}
			
			if (_gameDataProvider.AppDataProvider.IsHapticOn)
			{
				OnHapticToggleOffClicked();
			}
			else
			{
				OnHapticToggleOnClicked();
			}

			if (_gameDataProvider.AppDataProvider.UseDynamicJoystick)
			{
				OnDynamicJoystickToggleOffClicked();
			}
			else
			{
				OnDynamicJoystickToggleOnClicked();
			}
		}
		
		private void OnCloseClicked()
		{
			Debug.Log("Close Clicked");
			_gameDataProvider?.AppDataProvider?.ConnectionRegion?.StopObserving(OnConnectionRegionChange);
			Data.OnClose();
		}

		private void OnSoundToggleClicked()
		{
			_gameDataProvider.AppDataProvider.IsSfxEnabled = !_gameDataProvider.AppDataProvider.IsSfxEnabled;
			
			if (_gameDataProvider.AppDataProvider.IsSfxEnabled)
			{
				OnSoundToggleOffClicked();
			}
			else
			{
				OnSoundToggleOnClicked();
			}
		}
		
		
		private void OnSoundTabClicked()
		{
			Debug.Log("Sound Clicked");
			TabSelected(SETTINGS_TAB_CATEGORIES.SOUND);
		}

		private void OnSoundToggleOffClicked()
		{
			Debug.Log("On Sound Toggle Off Clicked");
		
			_soundToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_soundToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_soundToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_soundToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_gameDataProvider.AppDataProvider.IsSfxEnabled = true;
		}
		
		private void OnSoundToggleOnClicked()
		{
			Debug.Log("On Sound Toggle On Clicked");
			
			_soundToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_soundToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_soundToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_soundToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_gameDataProvider.AppDataProvider.IsSfxEnabled = false;
		}
		
		private void OnAnnouncerToggleClicked()
		{
			_gameDataProvider.AppDataProvider.IsSfxEnabled = !_gameDataProvider.AppDataProvider.IsSfxEnabled;
			
			if (_gameDataProvider.AppDataProvider.IsSfxEnabled)
			{
				OnAnnouncerToggleOffClicked();
			}
			else
			{
				OnAnnouncerToggleOnClicked();
			}
		}
		
		private void OnAnnouncerToggleOffClicked()
		{
			Debug.Log("On Announcer Toggle Off Clicked");
		
			_announcerToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_announcerToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_announcerToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_announcerToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_gameDataProvider.AppDataProvider.IsDialogueEnabled = true;
		}
		
		private void OnAnnouncerToggleOnClicked()
		{
			Debug.Log("On Announcer Toggle On Clicked");
			
			_announcerToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_announcerToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_announcerToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_announcerToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_gameDataProvider.AppDataProvider.IsDialogueEnabled = false;
		}
		
		private void OnBGMToggleClicked()
		{
			_gameDataProvider.AppDataProvider.IsBgmEnabled = !_gameDataProvider.AppDataProvider.IsBgmEnabled;
			
			if (_gameDataProvider.AppDataProvider.IsBgmEnabled)
			{
				OnBGMToggleOffClicked();
			}
			else
			{
				OnBGMToggleOnClicked();
			}
		}

		private void OnBGMToggleOffClicked()
		{
			Debug.Log("On BGM Off Clicked");
		
			_bgmToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_bgmToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_bgmToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_bgmToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_gameDataProvider.AppDataProvider.IsBgmEnabled = true;
		}
		
		private void OnBGMToggleOnClicked()
		{
			Debug.Log("On BGM On Clicked");
			
			_bgmToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_bgmToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_bgmToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_bgmToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_gameDataProvider.AppDataProvider.IsBgmEnabled = false;
		}
		
		private void OnHapticToggleClicked()
		{
			_gameDataProvider.AppDataProvider.IsHapticOn = !_gameDataProvider.AppDataProvider.IsHapticOn;
			
			if (_gameDataProvider.AppDataProvider.IsHapticOn)
			{
				OnHapticToggleOffClicked();
			}
			else
			{
				OnHapticToggleOnClicked();
			}
		}
		
		private void OnHapticToggleOffClicked()
		{
			Debug.Log("On Haptic Off Clicked");
		
			_hapticFeedbackToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_hapticFeedbackToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_hapticFeedbackToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_hapticFeedbackToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_gameDataProvider.AppDataProvider.IsHapticOn = true;
		}
		
		private void OnHapticToggleOnClicked()
		{
			Debug.Log("On Haptic On Clicked");
			
			_hapticFeedbackToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_hapticFeedbackToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_hapticFeedbackToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_hapticFeedbackToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_gameDataProvider.AppDataProvider.IsHapticOn = false;
		}

		private void OnDynamicJoystickToggleClicked()
		{
			_gameDataProvider.AppDataProvider.UseDynamicJoystick = !_gameDataProvider.AppDataProvider.UseDynamicJoystick;
			
			if (_gameDataProvider.AppDataProvider.UseDynamicJoystick)
			{
				OnDynamicJoystickToggleOffClicked();
			}
			else
			{
				OnDynamicJoystickToggleOnClicked();
			}
		}
		
		private void OnDynamicJoystickToggleOffClicked()
		{
			Debug.Log("On Dynamic Joystick Off Clicked");
		
			_dynamicStickToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_dynamicStickToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_dynamicStickToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = false;
			_dynamicStickToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = true;
			_gameDataProvider.AppDataProvider.UseDynamicJoystick = true;
		}
		
		private void OnDynamicJoystickToggleOnClicked()
		{
			Debug.Log("On Dynamic Joystick On Clicked");
			
			_dynamicStickToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_dynamicStickToggleButtons[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_dynamicStickToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.OFF].visible = true;
			_dynamicStickToggleLabels[(int) SETTINGS_TOGGLE_CATEGORIES.ON].visible = false;
			_gameDataProvider.AppDataProvider.UseDynamicJoystick = false;
		}
		
		private void OnGraphicsClicked()
		{
			Debug.Log("Graphics Clicked");
			TabSelected(SETTINGS_TAB_CATEGORIES.GRAPHICS);
		}
		
		private void OnControlsClicked()
		{
			Debug.Log("Controls Clicked");
			TabSelected(SETTINGS_TAB_CATEGORIES.CONTROLS);
		}
		
		private void OnAccountClicked()
		{
			Debug.Log("Account Clicked");
			TabSelected(SETTINGS_TAB_CATEGORIES.ACCOUNT);
		}

		private void TabSelected(SETTINGS_TAB_CATEGORIES category)
		{
			int size = Enum.GetNames(typeof(SETTINGS_TAB_CATEGORIES)).Length;
			
			for (int i = 0; i < size; i++)
			{
				_localizedTabs[i].RemoveSpriteClasses();
				_localizedSelectors[i].visible = i == (int) category;
				_localizedContentBlocks[i].SetDisplay(i == (int) category);
				
				if (i == (int) category)
				{
					_localizedTabs[i].AddToClassList(UssSpriteSelected);
				}
			}
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
			// _logoutButton.gameObject.SetActive(true);

			var regionName = _gameDataProvider.AppDataProvider.ConnectionRegion.Value.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());

#if UNITY_IOS
			_faq.gameObject.SetActive(false);
#endif
		}

		/// <summary>
		/// Updates the FLG ID account status
		/// </summary>
		public void UpdateAccountStatus()
		{
			if (_gameDataProvider.AppDataProvider.IsGuest)
			{
				// _connectIdButton.gameObject.SetActive(true);
				_idConnectionNameText.gameObject.SetActive(false);
				_idConnectionStatusText.text = ScriptLocalization.UITSettings.flg_id_not_connected;
			}
			else
			{
				// _connectIdButton.gameObject.SetActive(false);
				_idConnectionNameText.gameObject.SetActive(true);
				_idConnectionStatusText.text = ScriptLocalization.UITSettings.flg_id_connected;
				_idConnectionNameText.text = string.Format(ScriptLocalization.General.UserId, _gameDataProvider.AppDataProvider.DisplayName.Value);
			}
		}

		private void OnConnectionRegionChange(string previousValue, string newValue)
		{
			var regionName = newValue.GetPhotonRegionTranslation();
			_selectedServerText.text = string.Format(ScriptLocalization.MainMenu.ServerCurrent, regionName.ToUpper());
		}

		/// <inheritdoc />
		protected void OnClosedCompleted()
		{
			_gameDataProvider?.AppDataProvider?.ConnectionRegion?.StopObserving(OnConnectionRegionChange);
			// Data.OnClose();
		}

		private void OpenConnectId()
		{
			// Data.OnConnectIdClicked();
		}

		private void OpenServerSelect()
		{
			if (!NetworkUtils.CheckAttemptNetworkAction()) return;

			// Data.OnServerSelectClicked();
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
				// ButtonOnClick = Data.LogoutClicked
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
				// ButtonOnClick = Data.OnDeleteAccountClicked
			};
			
			_services.GenericDialogService.OpenButtonDialog(title, desc, true, confirmButton);
		}

		private void OnBlockerButtonPressed()
		{
			// Data.OnClose();
		}
	}
}
