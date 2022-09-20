using System;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using Button = UnityEngine.UI.Button;
using Random = UnityEngine.Random;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// This Presenter handles the Custom Game Creation Menu.
	/// </summary>
	public class RoomJoinCreateScreenPresenter : AnimatedUiPresenterData<RoomJoinCreateScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action CloseClicked;
			public Action PlayClicked;
		}

		[SerializeField, Required] private Button _backButton;
		[SerializeField, Required] private Button _createDeathmatchRoomButton;
		[SerializeField, Required] private Button _joinRoomButton;
		[SerializeField, Required] private Button _playtestButton;
		[SerializeField, Required] private TMP_Dropdown _gameModeSelection;
		[SerializeField, Required] private TMP_Dropdown _mapSelection;
		[SerializeField, Required] private TMP_Dropdown[] _mutatorsSelections;

		private IGameServices _services;

		private void Awake()
		{
			_services = MainInstaller.Resolve<IGameServices>();

			_gameModeSelection.onValueChanged.AddListener(FillMapSelectionList);
			
			FillGameModesSelectionList();
			FillMapSelectionList(0);
			FillMutatorsSelectionList();
			
			_backButton.onClick.AddListener(CloseRequested);
			_createDeathmatchRoomButton.onClick.AddListener(CreateRoomClicked);
			_joinRoomButton.onClick.AddListener(JoinRoomClicked);
			if (Debug.isDebugBuild)
			{
				_playtestButton.gameObject.SetActive(true);
				_playtestButton.onClick.AddListener(PlaytestClicked);
			}
			else
			{
				Destroy(_playtestButton);
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

			_services.GenericDialogService.OpenInputFieldDialog(ScriptLocalization.MainMenu.RoomJoinCode,
			                                                    "", confirmButton, true,
			                                                    TMP_InputField.ContentType.IntegerNumber);
		}

		private void OnRoomJoinClicked(string roomNameInput)
		{
			_services.MessageBrokerService.Publish(new PlayJoinRoomClickedMessage {RoomName = roomNameInput});
			Data.PlayClicked();
		}

		private void PlaytestClicked()
		{
			var gameModeConfig = ((GameModeDropdownMenuOption) _gameModeSelection.options[_gameModeSelection.value]).GameModeConfig;
			var mapConfig = ((MapDropdownMenuOption) _mapSelection.options[_mapSelection.value]).MapConfig;
			var message = new PlayCreateRoomClickedMessage
			{
				RoomName = GameConstants.Network.ROOM_NAME_PLAYTEST,
				GameModeConfig = gameModeConfig,
				MapConfig = mapConfig,
				Mutators = GetMutatorsList(),
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
				Mutators = GetMutatorsList()
			};

			_services.MessageBrokerService.Publish(message);
			Data.PlayClicked();
		}

		private string GetMutatorsList()
		{
			var mutators = "";

			for (var i = 0; i < _mutatorsSelections.Length; i++)
			{
				var mutatorMenuOption = ((MutatorDropdownMenuOption) _mutatorsSelections[i].options[_mutatorsSelections[i].value]);
				
				if (mutatorMenuOption.text.Length == 0)
				{
					continue;
				}
				
				mutators += mutatorMenuOption.MutatorConfig.Id + ",";
			}

			return mutators;
		}

		private void FillMutatorsSelectionList()
		{
			var mutatorConfigs = _services.ConfigsProvider.GetConfigsList<QuantumMutatorConfig>();

			foreach (var mutatorsSelection in _mutatorsSelections)
			{
				mutatorsSelection.options.Clear();
				mutatorsSelection.options.Add(new MutatorDropdownMenuOption("", new QuantumMutatorConfig()));

				foreach (var mutatorConfig in mutatorConfigs)
				{
					mutatorsSelection.options.Add(new MutatorDropdownMenuOption(mutatorConfig.Id, mutatorConfig));
				}

				mutatorsSelection.RefreshShownValue();
			}
		}
		
		private void FillMapSelectionList(int gameModeSelectionIndex)
		{
			_mapSelection.options.Clear();

			var gameModeConfig = ((GameModeDropdownMenuOption) _gameModeSelection.options[gameModeSelectionIndex]).GameModeConfig;

			foreach (var mapId in gameModeConfig.AllowedMaps)
			{
				var mapConfig = _services.ConfigsProvider.GetConfig<QuantumMapConfig>((int) mapId);
				if (!mapConfig.IsTestMap || Debug.isDebugBuild)
				{
					_mapSelection.options.Add(new MapDropdownMenuOption(mapId.GetTranslation(), mapConfig));
				}
			}

			_mapSelection.RefreshShownValue();
		}
		
		private void FillGameModesSelectionList()
		{
			_gameModeSelection.options.Clear();

			var gameModeConfigs = _services.ConfigsProvider.GetConfigsList<QuantumGameModeConfig>();

			foreach (var gameModeConfig in gameModeConfigs)
			{
				// TODO: Add "Is Debug Mode" into game mode configuration to avoid checking for "Testing"
				if (!Debug.isDebugBuild && gameModeConfig.Id == "Testing")
				{
					continue;
				}
				
				_gameModeSelection.options.Add(new GameModeDropdownMenuOption(gameModeConfig.Id, gameModeConfig));
			}

			_gameModeSelection.RefreshShownValue();
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
		
		private class MutatorDropdownMenuOption : TMP_Dropdown.OptionData
		{
			public QuantumMutatorConfig MutatorConfig { get; set; }

			public MutatorDropdownMenuOption(string text, QuantumMutatorConfig mutatorConfig) : base(text)
			{
				MutatorConfig = mutatorConfig;
			}
		}
	}
}