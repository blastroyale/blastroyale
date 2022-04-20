using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using I2.Loc;
using MoreMountains.NiceVibrations;
using Sirenix.OdinInspector;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class SettingsScreenPresenter : AnimatedUiPresenterData<ActionStruct>
	{
		[SerializeField, Required] private TextMeshProUGUI _versionText;
		[SerializeField, Required] private TextMeshProUGUI _fullNameText;
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private Button _blockerButton;
		[SerializeField, Required] private UiToggleButtonView _backgroundMusicToggle;
		[SerializeField, Required] private UiToggleButtonView _hapticToggle;
		[SerializeField, Required] private UiToggleButtonView _sfxToggle;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			_versionText.text = VersionUtils.VersionInternal;
			_fullNameText.text = string.Format(ScriptLocalization.General.UserId, _gameDataProvider.PlayerDataProvider.NicknameId.Value);
			
			_closeButton.onClick.AddListener(Close);
			_blockerButton.onClick.AddListener(Close);

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
	}
}