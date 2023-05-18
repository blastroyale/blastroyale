using System;
using System.Collections.Generic;
using System.Linq;
using FirstLight.Game.Configs;
using FirstLight.Game.Data;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
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
			public Action PlayClicked;
		}
		
		
		[SerializeField, Required] private TMP_Dropdown _gameModeSelection;
		[SerializeField, Required] private TMP_Dropdown _mapSelection;
		[SerializeField, Required] private TMP_Dropdown[] _mutatorsSelections;

		private IGameServices _services;
		private IGameDataProvider _gameDataProvider;

		private List<QuantumGameModeConfig> _quantumGameModeConfigs;
		private LocalizedDropDown _gameModeDropDown;
		private LocalizedDropDown _mapDropDown;
		private LocalizedDropDown [] _mutatorModeDropDown;
		private Button _joinRoomButton;
		private Button _playtestButton;
		private Button _createRoomButton;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
		}

		protected override void QueryElements(VisualElement root)
		{
			var header = root.Q<ScreenHeaderElement>("Header").Required();
			header.backClicked += Data.CloseClicked;
			header.homeClicked += Data.CloseClicked;

			_playtestButton = root.Q<Button>("PlaytestButton");
			_playtestButton.clicked += PlaytestClicked;
			_playtestButton.SetEnabled(Debug.isDebugBuild);
			
			_joinRoomButton = root.Q<Button>("JoinButton");
			_joinRoomButton.clicked += JoinRoomClicked;
			
			_createRoomButton = root.Q<Button>("CreateButton");
			_createRoomButton.clicked += CreateRoomClicked;
			
			_gameModeDropDown = root.Q<LocalizedDropDown>("GameMode").Required();
			// _gameModeSelection.onValueChanged.AddListener(FillMapSelectionList);
			_mapDropDown = root.Q<LocalizedDropDown>("Map").Required();

			_mutatorModeDropDown = new LocalizedDropDown[2];
			_mutatorModeDropDown[0] = root.Q<LocalizedDropDown>("Mutator1").Required();
			_mutatorModeDropDown[1] = root.Q<LocalizedDropDown>("Mutator2").Required();
			
			FillGameModesSelectionList();
			FillMapSelectionList(0);
			FillMutatorsSelectionList();

			SetPreviouslyUsedValues();
		}

		private void SetPreviouslyUsedValues()
		{
			var lastUsedOptions = _gameDataProvider.AppDataProvider.LastCustomGameOptions;
			if (lastUsedOptions != null)
			{
				
				if (_gameModeSelection.options.Count > lastUsedOptions.GameModeIndex)
				{
					_gameModeSelection.value = lastUsedOptions.GameModeIndex;
					_gameModeDropDown.index = lastUsedOptions.GameModeIndex;
				}
				
				if (lastUsedOptions.Mutators?.Count > 0)
				{
					SetMutatorsList(lastUsedOptions.Mutators);
				}

				if (lastUsedOptions.MapIndex < _mapSelection.options.Count)
				{
					// _mapSelection.value = lastUsedOptions.MapIndex;
					_mapDropDown.index = lastUsedOptions.MapIndex;
				}
				
			}
		}

		private void CloseRequested()
		{
			Close(true);
		}

		protected override void Close(bool destroy)
		{
			Data.CloseClicked.Invoke();
		}

		private void JoinRoomClicked()
		{
			var confirmButton = new GenericDialogButton<string>
			{
				ButtonText = ScriptLocalization.MainMenu.RoomJoinButton,
				ButtonOnClick = OnRoomJoinClicked
			};

			// TODO - open with IntegerNumber content type when technology evolves to support that
			_services.GenericDialogService.OpenInputDialog(ScriptLocalization.UITShared.info, ScriptLocalization.MainMenu.RoomJoinCode,
			                                                    "", confirmButton, true, null, TouchScreenKeyboardType.NumberPad);
		}

		private void OnRoomJoinClicked(string roomNameInput)
		{
			_services.MessageBrokerService.Publish(new PlayJoinRoomClickedMessage {RoomName = roomNameInput});
			Data.PlayClicked();
		}

		private CustomGameOptions GetChosenOptions()
		{
			return new CustomGameOptions()
			{
				GameModeIndex = _gameModeSelection.value,
				Mutators = GetMutatorsList(),
				MapIndex = _mapSelection.value
			};
		}

		private void PlaytestClicked()
		{
			var a = _gameModeSelection.options[_gameModeSelection.value];
			var gameModeConfig = ((GameModeDropdownMenuOption) _gameModeSelection.options[_gameModeSelection.value]).GameModeConfig;
			var mapConfig = ((MapDropdownMenuOption) _mapSelection.options[_mapSelection.value]).MapConfig;
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
			var gameModeConfig = ((GameModeDropdownMenuOption) _gameModeSelection.options[_gameModeSelection.value]).GameModeConfig;
			var mapConfig = ((MapDropdownMenuOption) _mapSelection.options[_mapSelection.value]).MapConfig;

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

			for (var i = 0; i < _mutatorsSelections.Length; i++)
			{
				var mutatorMenuOption = _mutatorsSelections[i].options[_mutatorsSelections[i].value];
				
				if (mutatorMenuOption.text.Equals("None"))
				{
					continue;
				}
				
				mutators.Add(mutatorMenuOption.text);
			}

			return mutators;
		}
		
		private void SetMutatorsList(List<string> mutators)
		{
			var usedMutators = new List<string>(mutators);
			foreach (var mutatorDropdown in _mutatorsSelections)
			{
				var presentMutator = mutatorDropdown.options.FirstOrDefault(o => usedMutators.Contains(o.text));
				if (presentMutator != null)
				{
					usedMutators.Remove(presentMutator.text);
					mutatorDropdown.value = mutatorDropdown.options.IndexOf(presentMutator);
				}
			}
		}

		private void FillMutatorsSelectionList()
		{
			var mutatorConfigs = _services.ConfigsProvider.GetConfigsList<QuantumMutatorConfig>();

			foreach (var mutatorsSelection in _mutatorModeDropDown)
			{
				var menuChoices = new List<string>();
				
				foreach (var mutatorConfig in mutatorConfigs)
				{
					menuChoices.Add(mutatorConfig.Id);
				}

				mutatorsSelection.choices = menuChoices;
			}

			/*
			foreach (var mutatorsSelection in _mutatorsSelections)
			{
				mutatorsSelection.options.Clear();
				mutatorsSelection.options.Add(new TMP_Dropdown.OptionData("None"));

				foreach (var mutatorConfig in mutatorConfigs)
				{
					mutatorsSelection.options.Add(new TMP_Dropdown.OptionData(mutatorConfig.Id));
				}

				mutatorsSelection.RefreshShownValue();
			}
			*/
		}
		
		private void FillMapSelectionList(int gameModeSelectionIndex)
		{
			var gameModeConfig =  _quantumGameModeConfigs[gameModeSelectionIndex];
			var menuChoices = new List<string>();
			
			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) mapId);
				if (!mapConfig.IsTestMap || Debug.isDebugBuild)
				{
					_mapSelection.options.Add(new MapDropdownMenuOption(mapId.GetLocalization(), mapConfig));
					menuChoices.Add(mapId.GetLocalization());
				}
			}

			_mapSelection.RefreshShownValue();
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

		private class GameModeDropdownMenuOption : TMP_Dropdown.OptionData
		{
			public QuantumGameModeConfig GameModeConfig { get; set; }

			public GameModeDropdownMenuOption(string text, QuantumGameModeConfig gameModeConfig) : base(text)
			{
				GameModeConfig = gameModeConfig;
			}
		}
		
		private class MapDropdownMenuOption : TMP_Dropdown.OptionData
		{
			public QuantumMapConfig MapConfig { get; set; }

			public MapDropdownMenuOption(string text, QuantumMapConfig mapConfig) : base(text)
			{
				MapConfig = mapConfig;
			}
		}
	}
}