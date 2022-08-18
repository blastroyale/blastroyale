using System;
using UnityEngine;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using I2.Loc;
using FirstLight.Game.Services;
using FirstLight.Game.Messages;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using TMPro;
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
			public Action OnServerSelectClicked;
		}

		[SerializeField] private Button _playOnlineButton;
		[SerializeField] private Button _playRoom;
		[SerializeField] private Button _nameChangeButton;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _feedbackButton;
		[SerializeField] private Button _gameModeButton;
		[SerializeField] private Button _serverSelectButton;
		[SerializeField] private NewFeatureUnlockedView _newFeaturesView;
		[SerializeField] private TextMeshProUGUI _selectedGameModeText;

		// Landscape Mode Buttons
		[SerializeField] private VisualStateButtonView _lootButton;
		[SerializeField] private VisualStateButtonView _heroesButton;
		[SerializeField] private Button _marketplaceButton;
		[SerializeField] private Button _discordButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_playOnlineButton.onClick.AddListener(OnPlayOnlineClicked);
			_playRoom.onClick.AddListener(OnPlayRoomlicked);

			_nameChangeButton.onClick.AddListener(OnNameChangeClicked);
			_settingsButton.onClick.AddListener(OnSettingsButtonClicked);
			_lootButton.Button.onClick.AddListener(OpenLootMenuUI);
			_heroesButton.Button.onClick.AddListener(OpenHeroesMenuUI);
			_marketplaceButton.gameObject.SetActive(Debug.isDebugBuild);
			_feedbackButton.onClick.AddListener(LeaveFeedbackForm);
			_discordButton.onClick.AddListener(OpenDiscordLink);
			_serverSelectButton.onClick.AddListener(OpenServerSelect);
			
			_gameModeButton.onClick.AddListener(OpenGameModeClicked);
			_gameModeButton.gameObject.SetActive(Debug.isDebugBuild);

			_newFeaturesView.gameObject.SetActive(false);
		}

		private void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		protected override void OnOpened()
		{
			base.OnOpened();

			RefreshGameModeButton();
		}

		private void OnPlayOnlineClicked()
		{
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

		private void OpenMarketplaceLink()
		{
			Application.OpenURL(GameConstants.Links.MARKETPLACE_URL);
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
		
		private void OpenServerSelect()
		{
			Data.OnServerSelectClicked();
		}

		private void RefreshGameModeButton()
		{
			var matchType = _gameDataProvider.AppDataProvider.SelectedMatchType.Value.ToString().ToUpper();
			var gameMode = _gameDataProvider.AppDataProvider.SelectedGameMode.Value.ToString().ToUpper();
			_selectedGameModeText.text = string.Format(ScriptLocalization.MainMenu.SelectedGameModeValue, matchType, gameMode);
		}
	}
}