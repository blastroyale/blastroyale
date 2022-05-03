using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.NativeUi;
using FirstLight.Services;
using I2.Loc;
using MoreMountains.NiceVibrations;
using PlayFab;
using PlayFab.ClientModels;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class SettingsScreenPresenter : AnimatedUiPresenterData<ActionStruct>
	{
		[SerializeField] private TextMeshProUGUI _versionText;
		[SerializeField] private TextMeshProUGUI _fullNameText;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _blockerButton;
		[SerializeField] private Button _logoutButton;

		[SerializeField] private UiToggleButtonView _backgroundMusicToggle;
		[SerializeField] private UiToggleButtonView _hapticToggle;
		[SerializeField] private UiToggleButtonView _sfxToggle;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		private IDataService _dataService;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_dataService = MainInstaller.Resolve<IDataService>();

			_versionText.text = VersionUtils.VersionInternal;
			_fullNameText.text = string.Format(ScriptLocalization.General.UserId,
			                                   _gameDataProvider.AppDataProvider.NicknameId.Value);

			_closeButton.onClick.AddListener(Close);
			_blockerButton.onClick.AddListener(Close);
			_logoutButton.onClick.AddListener(OnLogoutClicked);

			_backgroundMusicToggle.onValueChanged.AddListener(OnBgmChanged);
			_sfxToggle.onValueChanged.AddListener(OnSfxChanged);
			_hapticToggle.onValueChanged.AddListener(OnHapticChanged);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			_backgroundMusicToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsBgmOn);
			_sfxToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsSfxOn);
			_hapticToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsHapticOn);
		}

		/// <inheritdoc />
		protected override void OnClosedCompleted()
		{
			Data.Execute();
		}

		private void OnBgmChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsBgmOn = value;
		}

		private void OnSfxChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsSfxOn = value;
		}

		private void OnHapticChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsHapticOn = value;
			MMVibrationManager.SetHapticsActive(_gameDataProvider.AppDataProvider.IsHapticOn);
		}

		private void OnLogoutClicked()
		{
			var title = string.Format(ScriptLocalization.MainMenu.LogoutConfirm);
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ScriptLocalization.General.OK,
				ButtonOnClick = LogoutConfirm
			};

			_services.GenericDialogService.OpenDialog(title, true, confirmButton);
		}

		private void LogoutConfirm()
		{
#if UNITY_EDITOR
			var unlink = new UnlinkCustomIDRequest
			{
				CustomId = PlayFabSettings.DeviceUniqueIdentifier
			};

			PlayFabClientAPI.UnlinkCustomID(unlink, OnUnlinkSuccess, OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkCustomIDResult result)
			{
				UnlinkComplete();
			}

			void OnUnlinkFail(PlayFabError error)
			{
				_services.AnalyticsService.CrashLog(error.ErrorMessage);

				var button = new AlertButton
				{
					Callback = Application.Quit,
					Style = AlertButtonStyle.Negative,
					Text = "Quit Game"
				};

				NativeUiService.ShowAlertPopUp(false, "Game Error", error.ErrorMessage, button);
			}
#elif UNITY_ANDROID
			var unlink = new UnlinkAndroidDeviceIDRequest
			{
				AndroidDeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};
			
			PlayFabClientAPI.UnlinkAndroidDeviceID(unlink,OnUnlinkSuccess,OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkAndroidDeviceIDResult result)
			{
				UnlinkComplete();
			}
#elif UNITY_IOS
			var unlink = new UnlinkIOSDeviceIDRequest
			{
				DeviceId = PlayFabSettings.DeviceUniqueIdentifier,
			};

			PlayFabClientAPI.UnlinkIOSDeviceID(unlink, OnUnlinkSuccess, OnUnlinkFail);
			
			void OnUnlinkSuccess(UnlinkIOSDeviceIDResult result)
			{
				UnlinkComplete();
			}
#endif

			void UnlinkComplete()
			{
				var authData = _dataService.GetData<AuthenticationSaveData>();
				authData.LastLoginEmail = "";
				authData.LinkedDevice = false;
				_dataService.SaveData<AuthenticationSaveData>();
			}
		}
	}
}