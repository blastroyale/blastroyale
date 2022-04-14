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
			public Action OnShopButtonClicked;
			public Action OnLootButtonClicked;
			public Action OnHeroesButtonClicked;
			public Action OnCratesButtonClicked;
			public Action OnSocialButtonClicked;
			public Action OnTrophyRoadClicked;
			public Action OnRoomJoinCreateClicked;
		}

		[SerializeField] private GameObject _regularButtonRoot;
		[SerializeField] private GameObject _tournamentButtonRoot;
		[SerializeField] private Button _playBattleRoyaleButton;
		[SerializeField] private Button _playOfflineButton;
		[SerializeField] private Button _playDevButton;
		[SerializeField] private Button _playTournamentDeathmatchRandom;
		[SerializeField] private Button _playTournamentDeathmatchOffline;
		[SerializeField] private Button _playTournamentDeathmatchRoom;
		[SerializeField] private Button _settingsButton;
		[SerializeField] private Button _feedbackButton;
		[SerializeField] private NewFeatureUnlockedView _newFeaturesView;

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

		private void Start()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_regularButtonRoot.gameObject.SetActive(Debug.isDebugBuild);

			_playTournamentDeathmatchRandom.onClick.AddListener(OnPlayDeathmatchClicked);
			_playTournamentDeathmatchOffline.onClick.AddListener(OnPlayDeathmatchOfflineClicked);
			_playTournamentDeathmatchRoom.onClick.AddListener(OnRoomJoinCreatelicked);

			_playDevButton.onClick.AddListener(OnPlayBattleRoyaleDevClicked);
			_playBattleRoyaleButton.onClick.AddListener(OnPlayBattleRoyaleClicked);
			_playOfflineButton.onClick.AddListener(OnPlayBattleRoyaleOfflineClicked);
			
			_settingsButton.onClick.AddListener(OnSettingsButtonClicked);
			_lootButton.Button.onClick.AddListener(OpenLootMenuUI);
			_heroesButton.Button.onClick.AddListener(OpenHeroesMenuUI);
			_cratesButton.Button.onClick.AddListener(OpenCratesMenuUI);
			_shopButton.Button.onClick.AddListener(OpenShopMenuUI);
			_feedbackButton.onClick.AddListener(LeaveFeedbackForm);
			_discordButton.onClick.AddListener(OpenDiscordLink);
			_trophyRoadButton.onClick.AddListener(OnTrophyRoadButtonClicked);

			_newFeaturesView.gameObject.SetActive(false);
			_sliderPlayerLevelView.OnLevelUpXpSliderCompleted.AddListener(OnXpSliderAnimationCompleted);
		}

		private void OnDestroy()
		{
			Services?.MessageBrokerService?.UnsubscribeAll(this);
		}

		private void OnXpSliderAnimationCompleted(uint previousLevel, uint newLevel)
		{
			var unlockSystems = _gameDataProvider.PlayerDataProvider.GetUnlockSystems(newLevel, previousLevel + 1);

			foreach (var system in unlockSystems)
			{
				_newFeaturesView.QueueNewSystemPopUp(system, UnlockSystemButton);
			}
		}
		
		private void OnPlayBattleRoyaleClicked()
		{
			var runnerConfigs = Services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			runnerConfigs.IsOfflineMode = false;
			runnerConfigs.IsDevMode = false;
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.BattleRoyale;

			Data.OnPlayButtonClicked();
			_services.MessageBrokerService.Publish(new RoomRandomClickedMessage());
		}

		private void OnPlayBattleRoyaleDevClicked()
		{
			var runnerConfigs = Services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			runnerConfigs.IsOfflineMode = false;
			runnerConfigs.IsDevMode = true;
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.BattleRoyale;

			Data.OnPlayButtonClicked();
			_services.MessageBrokerService.Publish(new RoomRandomClickedMessage());
		}

		private void OnPlayBattleRoyaleOfflineClicked()
		{
			var runnerConfigs = Services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			runnerConfigs.IsOfflineMode = true;
			runnerConfigs.IsDevMode = false;
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.BattleRoyale;

			Data.OnPlayButtonClicked();
			_services.MessageBrokerService.Publish(new RoomRandomClickedMessage());
		}

		private void OnPlayDeathmatchClicked()
		{
			var runnerConfigs = Services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			runnerConfigs.IsOfflineMode = false;
			runnerConfigs.IsDevMode = false;
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.Deathmatch;

			Data.OnPlayButtonClicked();
			_services.MessageBrokerService.Publish(new RoomRandomClickedMessage());
		}

		private void OnPlayDeathmatchOfflineClicked()
		{
			var runnerConfigs = Services.ConfigsProvider.GetConfig<QuantumRunnerConfigs>();

			runnerConfigs.IsOfflineMode = true;
			runnerConfigs.IsDevMode = false;
			_gameDataProvider.AppDataProvider.SelectedGameMode.Value = GameMode.Deathmatch;

			Data.OnPlayButtonClicked();
			_services.MessageBrokerService.Publish(new RoomRandomClickedMessage());
		}

		private void OnRoomJoinCreatelicked()
		{
			Data.OnRoomJoinCreateClicked();
			_services.MessageBrokerService.Publish(new RoomJoinCreateClickedMessage());
		}

		private void OnTrophyRoadButtonClicked()
		{
			Data.OnTrophyRoadClicked();
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

		private void OpenCratesMenuUI()
		{
			if (!ButtonClickSystemCheck(UnlockSystem.Crates))
			{
				return;
			}

			Data.OnCratesButtonClicked();
		}

		private void OpenShopMenuUI()
		{
			if (!ButtonClickSystemCheck(UnlockSystem.Shop))
			{
				return;
			}

			Data.OnShopButtonClicked();
		}

		private void OpenSocialMenuUI()
		{
			Data.OnSocialButtonClicked();
		}

		private void LeaveFeedbackForm()
		{
			Application.OpenURL(GameConstants.FEEDBACK_FORM_LINK);
		}

		private void OpenDiscordLink()
		{
			Application.OpenURL(GameConstants.DISCORD_SERVER_LINK);
		}

		private void UnlockSystemButton(UnlockSystem system)
		{
			if (system == UnlockSystem.Fusion || system == UnlockSystem.Enhancement)
			{
				_lootButton.PlayUnlockedStateAnimation();
				_lootButton.UpdateState(true, true, false);
				_lootButton.UpdateShinyState();
			}
			else if (system == UnlockSystem.Shop)
			{
				_shopButton.PlayUnlockedStateAnimation();
				_shopButton.UpdateState(true, true, false);
			}
			else if (system == UnlockSystem.Crates)
			{
				UpdateCratesButtonState();
				_cratesButton.PlayUnlockedStateAnimation();
				_cratesButton.UpdateShinyState();
			}
		}

		private void UpdateCratesButtonState()
		{
			var time = Services.TimeService.DateTimeUtcNow;
			var unlockLevel = _gameDataProvider.PlayerDataProvider.GetUnlockSystemLevel(UnlockSystem.Crates);
			var tagged = _gameDataProvider.PlayerDataProvider.SystemsTagged;
			var info = _gameDataProvider.LootBoxDataProvider.GetLootBoxInventoryInfo();
			var emphasizeCrates = !info.LootBoxUnlocking.HasValue && info.GetSlotsFilledCount() > 0;

			foreach (var box in info.TimedBoxSlots)
			{
				if (box.HasValue && box.Value.GetState(time) == LootBoxState.Unlocked)
				{
					emphasizeCrates = true;
					break;
				}
			}

			_cratesButton.UpdateState(_sliderPlayerLevelView.Level >= unlockLevel,
			                          !tagged.Contains(UnlockSystem.Crates), emphasizeCrates);
		}

		private void UpdateButtonStates()
		{
			var unlocked = _gameDataProvider.PlayerDataProvider.GetUnlockSystems(_sliderPlayerLevelView.Level);
			var tagged = _gameDataProvider.PlayerDataProvider.SystemsTagged;
			var lootNew = unlocked.Contains(UnlockSystem.Fusion) && !tagged.Contains(UnlockSystem.Fusion) ||
			              unlocked.Contains(UnlockSystem.Enhancement) && !tagged.Contains(UnlockSystem.Enhancement);

			_sliderPlayerLevelView.UpdateProgressView();
			_lootButton.UpdateState(true, lootNew, false);
			_shopButton.UpdateState(unlocked.Contains(UnlockSystem.Shop), false, false);
			if (unlocked.Contains(UnlockSystem.Crates))
			{
				UpdateCratesButtonState();
			}

			this.LateCall(1, _lootButton.UpdateShinyState);
			this.LateCall(2, _cratesButton.UpdateShinyState);
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