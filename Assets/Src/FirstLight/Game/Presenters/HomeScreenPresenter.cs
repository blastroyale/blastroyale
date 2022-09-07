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
using Sirenix.OdinInspector;
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
			public Action OnLeaderboardClicked;
			public Action OnBattlePassClicked;
		}

		[SerializeField, Required] private Button _playOnlineButton;
		[SerializeField, Required] private Button _playRoom;
		[SerializeField, Required] private Button _nameChangeButton;
		[SerializeField, Required] private Button _settingsButton;
		[SerializeField, Required] private Button _feedbackButton;
		[SerializeField, Required] private Button _gameModeButton;
		[SerializeField, Required] private Button _leaderboardButton;
		[SerializeField, Required] private Button _battlePassButton;
		[SerializeField, Required] private NewFeatureUnlockedView _newFeaturesView;
		[SerializeField, Required] private TextMeshProUGUI _selectedGameModeText;
		[SerializeField, Required] private TextMeshProUGUI _selectedGameModeTimerText;

		// Landscape Mode Buttons
		[SerializeField, Required] private VisualStateButtonView _lootButton;
		[SerializeField, Required] private VisualStateButtonView _heroesButton;
		[SerializeField, Required] private Button _marketplaceButton;
		[SerializeField, Required] private Button _discordButton;

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
			_marketplaceButton.onClick.AddListener(OpenMarketplaceLink);
			_feedbackButton.onClick.AddListener(LeaveFeedbackForm);
			_discordButton.onClick.AddListener(OpenDiscordLink);
			_leaderboardButton.onClick.AddListener(OpenLeaderboardUI);
			_gameModeButton.onClick.AddListener(OpenGameModeClicked);
			_battlePassButton.onClick.AddListener(OpenBattlePassScreen);

			_leaderboardButton.gameObject.SetActive(FeatureFlags.LEADERBOARD_ACCESSIBLE);
			_marketplaceButton.gameObject.SetActive(Debug.isDebugBuild);
			_newFeaturesView.gameObject.SetActive(false);
		}

		private void Update()
		{
			var selectedGameModeInfo = _services.GameModeService.SelectedGameMode.Value;
			if (selectedGameModeInfo.FromRotation)
			{
				var timeLeft = selectedGameModeInfo.EndTime - DateTime.UtcNow;
				_selectedGameModeTimerText.text = timeLeft.ToString(@"hh\:mm\:ss");
			}
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

		private void OpenLeaderboardUI()
		{
			Data.OnLeaderboardClicked();
		}

		private void OpenGameModeClicked()
		{
			Data.OnGameModeClicked();
		}

		private void OpenSocialMenuUI()
		{
			Data.OnSocialButtonClicked();
		}

		private void OpenBattlePassScreen()
		{
			Data.OnBattlePassClicked();
		}

		private void LeaveFeedbackForm()
		{
			Application.OpenURL(GameConstants.Links.FEEDBACK_FORM);
		}

		private void OpenDiscordLink()
		{
			Application.OpenURL(GameConstants.Links.DISCORD_SERVER);
		}

		private void RefreshGameModeButton()
		{
			var gameMode = _services.GameModeService.SelectedGameMode.Value;
			_selectedGameModeTimerText.gameObject.SetActive(gameMode.FromRotation);
			_selectedGameModeText.text = string.Format(ScriptLocalization.MainMenu.SelectedGameModeValue,
			                                           gameMode.MatchType.GetTranslation().ToUpper(),
			                                           gameMode.Id.ToUpper());
		}
	}
}