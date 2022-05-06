using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using I2.Loc;
using MoreMountains.NiceVibrations;

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

		[SerializeField] private UiToggleButtonView _backgroundMusicToggle;
		[SerializeField] private UiToggleButtonView _hapticToggle;
		[SerializeField] private UiToggleButtonView _sfxToggle;
		[SerializeField] private UiToggleButtonView _highResModeToggle;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_versionText.text = VersionUtils.VersionInternal;
			_fullNameText.text = string.Format(ScriptLocalization.General.UserId, _gameDataProvider.AppDataProvider.NicknameId.Value);
			
			_closeButton.onClick.AddListener(Close);
			_blockerButton.onClick.AddListener(Close);

			_backgroundMusicToggle.onValueChanged.AddListener(OnBgmChanged);
			_sfxToggle.onValueChanged.AddListener(OnSfxChanged);
			_hapticToggle.onValueChanged.AddListener(OnHapticChanged);
			_highResModeToggle.onValueChanged.AddListener(OnHighResModeChanged);
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

		private void OnHighResModeChanged(bool value)
		{
			_gameDataProvider.AppDataProvider.IsHighResModeEnabled = value;
			_gameDataProvider.AppDataProvider.SetDynamicResolutionMode(value);
		}
	}
}