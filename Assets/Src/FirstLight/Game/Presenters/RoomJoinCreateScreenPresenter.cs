using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Services.Party;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Custom Game Creation Menu.
	/// </summary>
	public class RoomJoinCreateScreenPresenter : UiToolkitPresenterData<RoomJoinCreateScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action CloseClicked;
			public Action BackClicked;
			public Action PlayClicked;
		}

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;
		private List<QuantumGameModeConfig> _quantumGameModeConfigs;
		private List<QuantumMapConfig> _quantumMapConfigs;
		private List<string> _usedMutators;
		private LocalizedDropDown _gameModeDropDown;
		private LocalizedDropDown _mapDropDown;
		private LocalizedDropDown[] _mutatorModeDropDown;
		private LocalizedSliderInt _botDifficultyDropDown;
		private Button _joinRoomButton;
		private Button _playtestButton;
		private Button _createRoomButton;
		private LocalizedDropDown _weaponLimitDropDown;

		private static List<MutatorType> _weaponLimiterMutators = new List<MutatorType>
		{
			MutatorType.HammerTime,
			MutatorType.PistolsOnly,
			MutatorType.SMGsOnly,
			MutatorType.MinigunsOnly,
			MutatorType.ShotgunsOnly,
			MutatorType.SnipersOnly,
			MutatorType.RPGsOnly
		};

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			var header = root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked += Data.BackClicked;
			header.homeClicked += Data.CloseClicked;
			_playtestButton = root.Q<Button>("PlaytestButton");
			_playtestButton.clicked += PlaytestClicked;
			_playtestButton.SetEnabled(_services.GameBackendService.IsDev());

			_joinRoomButton = root.Q<Button>("JoinButton");
			_joinRoomButton.clicked += JoinRoomClicked;

			_createRoomButton = root.Q<Button>("CreateButton");
			_createRoomButton.clicked += CreateRoomClicked;

			_gameModeDropDown = root.Q<LocalizedDropDown>("GameMode").Required();
			_gameModeDropDown.RegisterValueChangedCallback(GameModeDropDownChanged);
			_mapDropDown = root.Q<LocalizedDropDown>("Map").Required();
			_mutatorModeDropDown = new LocalizedDropDown[2];
			_mutatorModeDropDown[0] = root.Q<LocalizedDropDown>("Mutator1").Required();
			_mutatorModeDropDown[0].value = ScriptLocalization.MainMenu.None;
			_mutatorModeDropDown[0].RegisterValueChangedCallback(MutatorDropDownChanged);
			_mutatorModeDropDown[1] = root.Q<LocalizedDropDown>("Mutator2").Required();
			_mutatorModeDropDown[1].value = ScriptLocalization.MainMenu.None;
			_mutatorModeDropDown[1].RegisterValueChangedCallback(MutatorDropDownChanged);
			_botDifficultyDropDown = root.Q<LocalizedSliderInt>("BotDifficulty").Required();
			_weaponLimitDropDown = root.Q<LocalizedDropDown>("WeaponLimiter").Required();

			FillGameModesSelectionList();
			FillMapSelectionList(0);
			FillMutatorsSelectionList();
			FillBotDifficultySelectionList();
			FillWeaponLimitSelectionList();
			SetPreviouslyUsedValues();
		}

		private void GameModeDropDownChanged(ChangeEvent<string> evt)
		{
			FillMapSelectionList(_gameModeDropDown.index);
			_mapDropDown.index = 0;
		}

		private void MutatorDropDownChanged(ChangeEvent<string> evt)
		{
			FillMutatorsSelectionList();
		}

		private void SetPreviouslyUsedValues()
		{
			var lastUsedOptions = _gameDataProvider.AppDataProvider.LastCustomGameOptions;
			if (lastUsedOptions != null)
			{
				if (_gameModeDropDown.choices.Count > lastUsedOptions.GameModeIndex)
				{
					_gameModeDropDown.SetValueWithoutNotify(_gameModeDropDown.choices[lastUsedOptions.GameModeIndex]);
					FillMapSelectionList(_gameModeDropDown.index);
				}

				if (lastUsedOptions.Mutators?.Count > 0)
				{
					SetMutatorsList(lastUsedOptions.Mutators);
				}

				if (lastUsedOptions.MapIndex < _quantumMapConfigs.Count)
				{
					_mapDropDown.index = lastUsedOptions.MapIndex;
				}

				_botDifficultyDropDown.value = DifficultyToSlide(lastUsedOptions.BotDifficulty);
				
				var presentWeaponLimiter = _weaponLimitDropDown.choices.FirstOrDefault(o => o == lastUsedOptions.WeaponLimiter);
				if (presentWeaponLimiter != null)
				{
					_weaponLimitDropDown.value = lastUsedOptions.WeaponLimiter;
				}
			}
		}

		private void JoinRoomClicked()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.MainMenu.RoomJoinButton,
				ButtonOnClick = OnRoomJoinClicked
			};
			// TODO - open with IntegerNumber content type when technology evolves to support that
			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITShared.info,
				ScriptLocalization.MainMenu.RoomJoinCode,
				"", confirmButton, true, null, TouchScreenKeyboardType.NumberPad);
		}

		private void OnRoomJoinClicked(string roomNameInput)
		{
			_services.MessageBrokerService.Publish(new PlayJoinRoomClickedMessage { RoomName = roomNameInput });
			Data.PlayClicked();
		}

		private CustomGameOptions GetChosenOptions()
		{
			return new CustomGameOptions()
			{
				GameModeIndex = _gameModeDropDown.index,
				Mutators = GetMutatorsList(),
				MapIndex = _mapDropDown.index,
				BotDifficulty = SlideToDifficulty(_botDifficultyDropDown.value),
				WeaponLimiter = _weaponLimitDropDown.value
			};
		}

		private void PlaytestClicked()
		{
			var gameModeConfig = _quantumGameModeConfigs[_gameModeDropDown.index];
			var mapConfig = _quantumMapConfigs[_mapDropDown.index];
			var message = new PlayCreateRoomClickedMessage
			{
				RoomName = GameConstants.Network.ROOM_NAME_PLAYTEST,
				GameModeConfig = gameModeConfig,
				MapConfig = mapConfig,
				CustomGameOptions = GetChosenOptions(),
				JoinIfExists = true
			};
			_services.MessageBrokerService.Publish(message);
			Data.PlayClicked();
		}

		private void CreateRoomClicked()
		{
			// Room code should be short and easily shareable, visible on the UI. Up to 6 trailing 0s
			// This should correspond to GameConstants.Data.ROOM_NAME_CODE_LENGTH
			var roomName = Random.Range(0, 999999).ToString("000000");
			var gameModeConfig = _quantumGameModeConfigs[_gameModeDropDown.index];
			var mapConfig = _quantumMapConfigs[_mapDropDown.index];

			var message = new PlayCreateRoomClickedMessage
			{
				RoomName = roomName,
				GameModeConfig = gameModeConfig,
				MapConfig = mapConfig,
				CustomGameOptions = GetChosenOptions()
			};
			_services.MessageBrokerService.Publish(message);
			Data.PlayClicked();
		}

		private List<String> GetMutatorsList()
		{
			var mutators = new List<String>();
			for (var i = 0; i < _mutatorModeDropDown.Length; i++)
			{
				var mutatorMenuOption = _mutatorModeDropDown[i].value;

				if (mutatorMenuOption == null || mutatorMenuOption.Equals(ScriptLocalization.MainMenu.None))
				{
					continue;
				}

				mutators.Add(mutatorMenuOption);
			}

			return mutators;
		}

		private void SetMutatorsList(List<string> mutators)
		{
			var usedMutators = new List<string>(mutators);
			foreach (var mutatorDropdown in _mutatorModeDropDown)
			{
				var presentMutator = mutatorDropdown.choices.FirstOrDefault(o => usedMutators.Contains(o));

				if (presentMutator != null)
				{
					usedMutators.Remove(presentMutator);
					mutatorDropdown.value = presentMutator;
				}
			}
		}

		private List<string> GetSelectedMutators()
		{
			var selectedMutators = new List<string>();
			foreach (var mutatorDropdown in _mutatorModeDropDown)
			{
				var presentMutator = mutatorDropdown.value;

				if (presentMutator != null)
				{
					selectedMutators.Add(presentMutator);
					mutatorDropdown.value = presentMutator;
				}
			}

			return selectedMutators;
		}

		private void FillMutatorsSelectionList()
		{
			var selectedMutators = GetSelectedMutators();
			var mutatorConfigs = _services.ConfigsProvider.GetConfigsList<QuantumMutatorConfig>();

			foreach (var mutatorsSelection in _mutatorModeDropDown)
			{
				mutatorsSelection.choices.Clear();
				var menuChoices = new List<string>();
				menuChoices.Add(ScriptLocalization.MainMenu.None);

				foreach (var mutatorConfig in mutatorConfigs)
				{
					if (_weaponLimiterMutators.Contains(mutatorConfig.Type))
					{
						continue;
					}
					
					if (!selectedMutators.Contains(mutatorConfig.Id))
					{
						menuChoices.Add(mutatorConfig.Id);
					}
				}

				mutatorsSelection.choices = menuChoices;
			}
		}

		private void FillMapSelectionList(int gameModeSelectionIndex)
		{
			var gameModeConfig = _quantumGameModeConfigs[gameModeSelectionIndex];
			var menuChoices = new List<string>();

			_quantumMapConfigs = new List<QuantumMapConfig>();

			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>((int)mapId);
				if (!mapConfig.IsTestMap || Debug.isDebugBuild)
				{
					_quantumMapConfigs.Add(mapConfig);
					menuChoices.Add(mapId.GetLocalization());
				}
			}

			_mapDropDown.choices = menuChoices;
		}

		private void FillGameModesSelectionList()
		{
			var menuChoices = new List<string>();
			var gameModeConfigs = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>();
			_quantumGameModeConfigs = new List<QuantumGameModeConfig>();
			foreach (var gameModeConfig in gameModeConfigs)
			{
				if (gameModeConfig.IsDebugOnly && !Debug.isDebugBuild)
				{
					continue;
				}

				_quantumGameModeConfigs.Add(gameModeConfig);
				menuChoices.Add(gameModeConfig.Id);
			}

			_gameModeDropDown.choices = menuChoices;
		}

		private void FillBotDifficultySelectionList()
		{
			var difficulties = _services.ConfigsProvider.GetConfig<BotDifficultyConfigs>();
			// Plus 1 because the first value is the default one (not changing difficulty)
			_botDifficultyDropDown.highValue = difficulties.Configs.Count-1;
			_botDifficultyDropDown.lowValue = -1;
			_botDifficultyDropDown.value = 0;
		}

		private int SlideToDifficulty(int slideValue)
		{
			// Default value: do not change difficulty
			if (slideValue == -1)
			{
				return -1;

			}
			var difficulties = _services.ConfigsProvider.GetConfig<BotDifficultyConfigs>();

			for (var i = 0; i < difficulties.Configs.Count; i++)
			{
				if (slideValue == i)
				{
					return (int)difficulties.Configs[i].BotDifficulty;
				}
			}

			return 0;
		}

		private int DifficultyToSlide(int difficulty)
		{
			// Default value: do not change difficulty
			if (difficulty == -1) return -1;
			var difficulties = _services.ConfigsProvider.GetConfig<BotDifficultyConfigs>();

			for (var i = 0; i < difficulties.Configs.Count; i++)
			{
				if (difficulties.Configs[i].BotDifficulty == difficulty)
				{
					return i;
				}
			}

			return 0;
		}
		
		private void FillWeaponLimitSelectionList()
		{
			var mutatorConfigs = _services.ConfigsProvider.GetConfigsList<QuantumMutatorConfig>();

			_weaponLimitDropDown.choices.Clear();
			var menuChoices = new List<string>();
			menuChoices.Add(ScriptLocalization.MainMenu.None);

			foreach (var mutatorConfig in mutatorConfigs)
			{
				if (_weaponLimiterMutators.Contains(mutatorConfig.Type))
				{
					menuChoices.Add(mutatorConfig.Id);
				}
			}

			_weaponLimitDropDown.choices = menuChoices;
			_weaponLimitDropDown.index = 0;
		}
	}
}