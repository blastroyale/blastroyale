using System;
using UnityEngine;
using FirstLight.Game.Configs;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using I2.Loc;
using FirstLight.Game.Services;
using FirstLight.Game.Infos;
using FirstLight.Game.Messages;
using FirstLight.Game.Views.MainMenuViews;
using Quantum;
using Sirenix.OdinInspector;
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
			public Action OnPlayRoomJoinCreateClicked;
			public Action OnNameChangeClicked;
		}

		[SerializeField, Required] private GameObject _battleRoyaleButtonRoot;
		[SerializeField, Required] private Button _playBattleRoyaleRandom;
		[SerializeField, Required] private Button _playBattleRoyaleOffline;
		[SerializeField, Required] private Button _playBattleRoyaleTestMap;
		[SerializeField, Required] private Button _playDeathmatchRandom;
		[SerializeField, Required] private Button _playDeathmatchOffline;
		[SerializeField, Required] private Button _playRoom;
		[SerializeField, Required] private Button _nameChangeButton;
		[SerializeField, Required] private Button _settingsButton;
		[SerializeField, Required] private Button _feedbackButton;
		[SerializeField, Required] private NewFeatureUnlockedView _newFeaturesView;

		// Player Information / Trophy Road.
		[SerializeField, Required] private PlayerProgressBarView _sliderPlayerLevelView;
		[SerializeField, Required] private Button _trophyRoadButton;

		// Landscape Mode Buttons
		[SerializeField, Required] private VisualStateButtonView _lootButton;
		[SerializeField, Required] private VisualStateButtonView _heroesButton;
		[SerializeField, Required] private VisualStateButtonView _cratesButton;
		[SerializeField, Required] private VisualStateButtonView _shopButton;
		[SerializeField, Required] private Button _discordButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;
		
		// TODO - remove when appropriate
		private IMainMenuServices _mainMenuServices;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_mainMenuServices = MainMenuInstaller.Resolve<IMainMenuServices>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_battleRoyaleButtonRoot.gameObject.SetActive(Debug.isDebugBuild);

			_playRoom.onClick.AddListener(OnPlayRoomlicked);
			_playDeathmatchRandom.onClick.AddListener(OnPlayDeathmatchClicked);
			_playDeathmatchOffline.onClick.AddListener(OnPlayDeathmatchOfflineClicked);
			_playBattleRoyaleRandom.onClick.AddListener(OnPlayBattleRoyaleClicked);
			_playBattleRoyaleOffline.onClick.AddListener(OnPlayBattleRoyaleOfflineClicked);
			_playBattleRoyaleTestMap.onClick.AddListener(OnPlayBattleRoyaleTestMapClicked);
			
			_nameChangeButton.onClick.AddListener(OnNameChangeClicked);
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
			var message = new PlayRandomClickedMessage
			{
				IsOfflineMode = false,
				GameMode = GameMode.BattleRoyale
			};

			_services.MessageBrokerService.Publish(message);
			Data.OnPlayButtonClicked();
		}

		private void OnPlayBattleRoyaleOfflineClicked()
		{
			var message = new PlayRandomClickedMessage
			{
				IsOfflineMode = true,
				GameMode = GameMode.BattleRoyale
			};

			_services.MessageBrokerService.Publish(message);
			Data.OnPlayButtonClicked();
		}
		
		private void OnPlayBattleRoyaleTestMapClicked()
		{
			var message = new PlayMapClickedMessage
			{
				IsOfflineMode = true,
				MapId = GetFirstTestMapId()
			};

			_services.MessageBrokerService.Publish(message);
			Data.OnPlayButtonClicked();
		}

		private int GetFirstTestMapId()
		{
			var configs = _services.ConfigsProvider.GetConfigsDictionary<MapConfig>();

			foreach(var config in configs)
			{
				if(config.Value.IsTestMap)
				{
					return config.Value.Id;
				}
			}

			return 0;
		}

		private void OnPlayDeathmatchClicked()
		{
			var message = new PlayRandomClickedMessage
			{
				IsOfflineMode = false,
				GameMode = GameMode.Deathmatch
			};

			_services.MessageBrokerService.Publish(message);
			Data.OnPlayButtonClicked();
		}

		private void OnPlayDeathmatchOfflineClicked()
		{
			var message = new PlayRandomClickedMessage
			{
				IsOfflineMode = true,
				GameMode = GameMode.Deathmatch
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