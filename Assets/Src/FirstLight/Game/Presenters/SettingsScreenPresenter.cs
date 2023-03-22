using System;
using System.Drawing;
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
using Button = UnityEngine.UIElements.Button;
using Color = UnityEngine.Color;

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
		[SerializeField, Required] private Button _blockerButton;
		[SerializeField, Required] private UiToggleButtonView _screenshakeToggle;
		[SerializeField, Required] private UiToggleButtonView _highFpsToggle;
		[SerializeField, Required] private DetailLevelToggleView _detailLevelView;
		[SerializeField, Required] private Button _serverSelectButton;
		[SerializeField, Required] private TextMeshProUGUI _selectedServerText;

		private ImageButton _closeScreenButton;
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		
		private LocalizedButton [] _localizedTabs;
		private VisualElement[] _localizedSelectors;
		private VisualElement[] _localizedContentBlocks;
		
		private Label _buildInfoLabel;
		private Button _faqButton;
		private Button _serverButton;

		// Account Toggle
		private Button _logoutButton;
		private Button _deleteAccountButton;
		private Button _connectIdButton;
		private Label _connectionStatusLabel;
		private Label _connectionNameText;

		// Radio Buttons
		
		// FPS Buttons
		private Button [] _fpsButtons;
		
		private enum SETTINGS_TOGGLE_FPS
		{
			Thirty = 0,
			Sixty = 1,
		}
		
		// Graphics Buttons
		private Button [] _graphicsButtons;
		
		private enum SETTINGS_TOGGLE_GRAPHICS
		{
			Low = 0,
			Medium = 1,
			High,
		}
		
		private const string UssSpriteSelected = "sprite-home__settings-tab-chosen";
		private const string UssSpriteUnselected = "sprite-home__settings-tab-back";

		private enum SETTINGS_TAB_CATEGORIES
		{
			SOUND = 0,
			CONTROLS = 1,
			GRAPHICS = 2,
			ACCOUNT = 3
		};

		private readonly Color _deselectedRadioButtonColor = new Color(0.08f, 0.07f, 0.14f, 1f);
		private readonly Color _selectedRadioButtonColor = new Color(0.94f, 0.29f, 0.47f, 1f);

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
			
			size = Enum.GetNames(typeof(SETTINGS_TOGGLE_FPS)).Length;

			_fpsButtons = new Button[size];
			
			size = Enum.GetNames(typeof(SETTINGS_TOGGLE_GRAPHICS)).Length;

			_graphicsButtons = new Button[size];
		}

		protected override void QueryElements(VisualElement root)
		{
			_closeScreenButton = root.Q<ImageButton>("CloseButton");
			_closeScreenButton.clicked += OnCloseClicked;
			
			// Build Info Text
			_buildInfoLabel= root.Q<Label>("BuildInfoLabel");
			_buildInfoLabel.text = VersionUtils.VersionInternal;

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

			TabSelected(SETTINGS_TAB_CATEGORIES.SOUND);
			
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
			
			// FPS Buttons
			_fpsButtons[(int)SETTINGS_TOGGLE_FPS.Thirty] = root.Q<Button>("30RadioButton");
			_fpsButtons[(int)SETTINGS_TOGGLE_FPS.Sixty] = root.Q<Button>("60RadioButton");
			_fpsButtons[(int) SETTINGS_TOGGLE_FPS.Thirty].clicked += OnFPSTogglePressed;
			_fpsButtons[(int) SETTINGS_TOGGLE_FPS.Sixty].clicked += OnFPSTogglePressed;
			SetFpsRadioButtons();
			
			// Graphics Buttons
			_graphicsButtons[(int)SETTINGS_TOGGLE_GRAPHICS.Low] = root.Q<Button>("LowRadioButton");
			_graphicsButtons[(int) SETTINGS_TOGGLE_GRAPHICS.Low].clicked += OnLowGraphicsPressed;
			_graphicsButtons[(int)SETTINGS_TOGGLE_GRAPHICS.Medium] = root.Q<Button>("MediumRadioButton");
			_graphicsButtons[(int) SETTINGS_TOGGLE_GRAPHICS.Medium].clicked += OnMediumGraphicsPressed;
			_graphicsButtons[(int)SETTINGS_TOGGLE_GRAPHICS.High] = root.Q<Button>("HighRadioButton");
			_graphicsButtons[(int) SETTINGS_TOGGLE_GRAPHICS.High].clicked += OnHighGraphicsPressed;
			SetGraphicsQualityToggles();
			
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

		private static void SetupToggle(LocalizedToggle toggle, Func<bool> getter, Action<bool> setter)
		{
			toggle.value = getter();
			toggle.RegisterCallback<ChangeEvent<bool>, Action<bool>>((e, s) => s(e.newValue), setter);
		}

		private void OnCloseClicked()
		{
			Debug.Log("Close Clicked");
			_gameDataProvider?.AppDataProvider?.ConnectionRegion?.StopObserving(OnConnectionRegionChange);
			Data.OnClose();
		}

		private void OnFPSTogglePressed()
		{
			if (_gameDataProvider.AppDataProvider.FpsTarget == GameConstants.Visuals.LOW_FPS_MODE_TARGET)
			{
				_gameDataProvider.AppDataProvider.FpsTarget = GameConstants.Visuals.HIGH_FPS_MODE_TARGET;
			}
			else
			{
				_gameDataProvider.AppDataProvider.FpsTarget = GameConstants.Visuals.LOW_FPS_MODE_TARGET;
			}

			SetFpsRadioButtons();
		}

		private void SetFpsRadioButtons()
		{
			if (_gameDataProvider.AppDataProvider.FpsTarget == GameConstants.Visuals.LOW_FPS_MODE_TARGET)
			{
				_fpsButtons[(int) SETTINGS_TOGGLE_FPS.Thirty].style.unityBackgroundImageTintColor = new StyleColor(_selectedRadioButtonColor);
				_fpsButtons[(int) SETTINGS_TOGGLE_FPS.Sixty].style.unityBackgroundImageTintColor = new StyleColor(_deselectedRadioButtonColor);
			}
			else
			{
				_fpsButtons[(int) SETTINGS_TOGGLE_FPS.Thirty].style.unityBackgroundImageTintColor = new StyleColor(_deselectedRadioButtonColor);
				_fpsButtons[(int) SETTINGS_TOGGLE_FPS.Sixty].style.unityBackgroundImageTintColor = new StyleColor(_selectedRadioButtonColor);
			}
		}

		private void OnLowGraphicsPressed()
		{
			_gameDataProvider.AppDataProvider.CurrentDetailLevel = GraphicsConfig.DetailLevel.Low;
			SetGraphicsQualityToggles();
		}
		
		private void OnMediumGraphicsPressed()
		{
			_gameDataProvider.AppDataProvider.CurrentDetailLevel = GraphicsConfig.DetailLevel.Medium;
			SetGraphicsQualityToggles();
		}
		
		private void OnHighGraphicsPressed()
		{
			_gameDataProvider.AppDataProvider.CurrentDetailLevel = GraphicsConfig.DetailLevel.High;
			SetGraphicsQualityToggles();
		}
		

		private void SetGraphicsQualityToggles()
		{
			foreach (var button in _graphicsButtons)
			{
				button.style.unityBackgroundImageTintColor = _deselectedRadioButtonColor;
			}

			switch (_gameDataProvider.AppDataProvider.CurrentDetailLevel)
			{
				case GraphicsConfig.DetailLevel.Low:

						_graphicsButtons[(int) SETTINGS_TOGGLE_GRAPHICS.Low].style.unityBackgroundImageTintColor =
						_selectedRadioButtonColor;
					
					break;
				
				case GraphicsConfig.DetailLevel.Medium:

					_graphicsButtons[(int) SETTINGS_TOGGLE_GRAPHICS.Medium].style.unityBackgroundImageTintColor =
						_selectedRadioButtonColor;
					
					break;
				
				case GraphicsConfig.DetailLevel.High:

					_graphicsButtons[(int) SETTINGS_TOGGLE_GRAPHICS.High].style.unityBackgroundImageTintColor =
						_selectedRadioButtonColor;
					
					break;
			}
		}

		private void OnGraphicsClicked()
		{
			TabSelected(SETTINGS_TAB_CATEGORIES.GRAPHICS);
		}
		
		private void OnControlsClicked()
		{
			TabSelected(SETTINGS_TAB_CATEGORIES.CONTROLS);
		}
		
		private void OnAccountClicked()
		{
			TabSelected(SETTINGS_TAB_CATEGORIES.ACCOUNT);
		}

		private void OnSoundTabClicked()
		{
			TabSelected(SETTINGS_TAB_CATEGORIES.SOUND);
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
				_connectionNameText.text = string.Format(ScriptLocalization.General.UserId, _gameDataProvider.AppDataProvider.DisplayName.Value);
			}
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
				else
				{
					_localizedTabs[i].AddToClassList(UssSpriteUnselected);
				}
			}
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			_versionText.text = VersionUtils.VersionInternal;
			
			_highFpsToggle.SetInitialValue(_gameDataProvider.AppDataProvider.FpsTarget == GameConstants.Visuals.LOW_FPS_MODE_TARGET);
			_detailLevelView.SetSelectedDetailLevel(_gameDataProvider.AppDataProvider.CurrentDetailLevel);

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

		/// <inheritdoc />
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

		private void OnHapticChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsHapticOn = value;
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
			// _services.AudioFxService.PlayClip2D(AudioId.ButtonClickForward);
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
