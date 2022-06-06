using System;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.Game.Views.MainMenuViews;
using I2.Loc;
using MoreMountains.NiceVibrations;
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
		}
		
		[SerializeField, Required] private TextMeshProUGUI _versionText;
		[SerializeField, Required] private TextMeshProUGUI _fullNameText;
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private Button _blockerButton;
		[SerializeField, Required] private Button _logoutButton;
		[SerializeField, Required] private UiToggleButtonView _backgroundMusicToggle;
		[SerializeField, Required] private UiToggleButtonView _hapticToggle;
		[SerializeField, Required] private UiToggleButtonView _sfxToggle;
		[SerializeField, Required] private DetailLevelToggleView _detailLevelView;
		
		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_versionText.text = VersionUtils.VersionInternal;
			_fullNameText.text = string.Format(ScriptLocalization.General.UserId,
			                                   _gameDataProvider.AppDataProvider.NicknameId.Value);

			_closeButton.onClick.AddListener(Close);
			_blockerButton.onClick.AddListener(Close);
			_logoutButton.onClick.AddListener(OnLogoutClicked);

			_backgroundMusicToggle.onValueChanged.AddListener(OnBgmChanged);
			_sfxToggle.onValueChanged.AddListener(OnSfxChanged);
			_hapticToggle.onValueChanged.AddListener(OnHapticChanged);
			_detailLevelView.ValueChanged += OnDetailLevelChanged;
			
			_backgroundMusicToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsBgmOn);
			_sfxToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsSfxOn);
			_hapticToggle.SetInitialValue(_gameDataProvider.AppDataProvider.IsHapticOn);
			_detailLevelView.SetSelectedDetailLevel(_gameDataProvider.AppDataProvider.CurrentDetailLevel);
		}
		
		/// <inheritdoc />
		protected override void OnClosedCompleted()
		{
			Data.OnClose();
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
		}

		private void OnDetailLevelChanged(AppData.DetailLevel detailLevel)
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
	}
}