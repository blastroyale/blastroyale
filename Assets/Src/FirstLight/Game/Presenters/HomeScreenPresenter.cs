using System;
using UnityEngine;
using FirstLight.Game.Configs;
using FirstLight.Game.Ids;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using I2.Loc;
using FirstLight.Game.Services;
using FirstLight.Game.Infos;
using FirstLight.Game.Messages;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using TMPro;
using UnityEngine.UI;
using Button = UnityEngine.UI.Button;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Home Screen.
	/// </summary>
	public class HomeScreenPresenter : AnimatedUiPresenterData<HomeScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action OnPlayButtonClicked;
			public Action OnSettingsButtonClicked;
			public Action OnLootButtonClicked;
			public Action OnHeroesButtonClicked;
			public Action OnSocialButtonClicked;
			public Action OnPlayRoomJoinCreateClicked;
			public Action OnNameChangeClicked;
			public Action OnGameModeClicked;
		}

		[SerializeField] private Button _playOnlineButton;
		[SerializeField] private Button _playOfflineDebugButton;
		[SerializeField] private Button _playRoom;
		[SerializeField] private Button _nameChangeButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _feedbackButton;
		[SerializeField] private Button _gameModeButton;
		[SerializeField] private NewFeatureUnlockedView _newFeaturesView;
		[SerializeField] private TextMeshProUGUI _selectedGameModeText;

		// Player Information / Trophy Road.
		[SerializeField] private PlayerProgressBarView _sliderPlayerLevelView;
		[SerializeField] private Button _trophyRoadButton;

		// Landscape Mode Buttons
		[SerializeField] private VisualStateButtonView _lootButton;
		[SerializeField] private VisualStateButtonView _heroesButton;
		[SerializeField] private VisualStateButtonView _cratesButton;
		[SerializeField] private VisualStateButtonView _shopButton;
		[SerializeField] private Button _discordButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		
		// TODO - remove when appropriate
		private IMainMenuServices _mainMenuServices;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_services = MainInstaller.Resolve<IGameServices>();

			_playOnlineButton.onClick.AddListener(OnPlayOnlineClicked);
			_playOfflineDebugButton.onClick.AddListener(OnPlayOfflineClicked);
			_playRoom.onClick.AddListener(OnPlayRoomlicked);

			_nameChangeButton.onClick.AddListener(OnNameChangeClicked);
			_settingsButton.onClick.AddListener(OnSettingsButtonClicked);
			_lootButton.Button.onClick.AddListener(OpenLootMenuUI);
			_heroesButton.Button.onClick.AddListener(OpenHeroesMenuUI);
			_feedbackButton.onClick.AddListener(LeaveFeedbackForm);
			_discordButton.onClick.AddListener(OpenDiscordLink);
			_gameModeButton.onClick.AddListener(OpenGameModeClicked);

			_playOfflineDebugButton.gameObject.SetActive(Debug.isDebugBuild);
			_newFeaturesView.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnOpened()
		{
			base.OnOpened();
			
			_selectedGameModeText.text = string.Format(ScriptLocalization.MainMenu.SelectedGameModeText,
				_gameDataProvider.AppDataProvider.SelectedGameMode.Value.ToString());
		}
		
		private void OnPlayOnlineClicked()
		{
			var message = new PlayRandomClickedMessage
			{
				IsOfflineMode = false,
			};

			_services.MessageBrokerService.Publish(message);
			Data.OnPlayButtonClicked();
		}
		
		private void OnPlayOfflineClicked()
		{
			var message = new PlayRandomClickedMessage
			{
				IsOfflineMode = true,
			};

			_services.MessageBrokerService.Publish(message);
			Data.OnPlayButtonClicked();
		}
		
		private void OnPlayRoomlicked()
		{
			Data.OnPlayRoomJoinCreateClicked();
		}

		private void OnNameChangeClicked()
		{
			Data.OnNameChangeClicked();
		}

		private void OnSettingsButtonClicked()
		{
			Data.OnSettingsButtonClicked();
		}

		private void OpenLootMenuUI()
		{
			Data.OnLootButtonClicked();
		}

		private void OpenHeroesMenuUI()
		{
			Data.OnHeroesButtonClicked();
		}

		private void OpenGameModeClicked()
		{
			Data.OnGameModeClicked();
		}
		
		private void OpenSocialMenuUI()
		{
			Data.OnSocialButtonClicked();
		}

		private void LeaveFeedbackForm()
		{
			Application.OpenURL(GameConstants.Links.FEEDBACK_FORM);
		}

		private void OpenDiscordLink()
		{
			Application.OpenURL(GameConstants.Links.DISCORD_SERVER);
		}

		private void UnlockSystemButton(UnlockSystem system)
		{
			if (system == UnlockSystem.Shop)
			{
				_shopButton.PlayUnlockedStateAnimation();
				_shopButton.UpdateState(true, true, false);
			}
		}

		private bool ButtonClickSystemCheck(UnlockSystem system)
		{
			var unlockLevel = _gameDataProvider.PlayerDataProvider.GetUnlockSystemLevel(system);

			if (_gameDataProvider.PlayerDataProvider.Level.Value < unlockLevel)
			{
				var unlockAtText =
					string.Format(ScriptLocalization.General.UnlockAtPlayerLevel, unlockLevel.ToString());

				_mainMenuServices.UiVfxService.PlayFloatingText(unlockAtText);

				return false;
			}

			var tagged = _gameDataProvider.PlayerDataProvider.SystemsTagged;

			if (!tagged.Contains(system))
			{
				tagged.Add(system);
			}

			return true;
		}
	}
}
