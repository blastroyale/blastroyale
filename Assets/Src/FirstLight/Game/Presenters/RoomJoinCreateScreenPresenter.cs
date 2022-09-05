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
	/// This Presenter handles the Shop Menu.
	/// </summary>
	public class RoomJoinCreateScreenPresenter : AnimatedUiPresenterData<RoomJoinCreateScreenPresenter.StateData>
	{
		public struct StateData
		{
			public Action CloseClicked;
			public Action PlayClicked;
		}
		
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private Button _createDeathmatchRoomButton;
		[SerializeField, Required] private Button _joinRoomButton;
		[SerializeField, Required] private Button _playtestButton;
		[SerializeField, Required] private TMP_Dropdown _mapSelection;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			FillMapSelectionList();
			
			_services.GameModeService.SelectedGameMode.Observe((_, _) => FillMapSelectionList());

			_closeButton.onClick.AddListener(CloseRequested);
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
			                                                    "", confirmButton, true, TMP_InputField.ContentType.IntegerNumber);
		}

		private void OnRoomJoinClicked(string roomNameInput)
		{
			_services.MessageBrokerService.Publish(new PlayJoinRoomClickedMessage{ RoomName = roomNameInput });
			Data.PlayClicked();
		}
		
		private void PlaytestClicked()
		{
			var mapConfig = ((DropdownMenuOption) _mapSelection.options[_mapSelection.value]).MapConfig;
			var message = new PlayCreateRoomClickedMessage
			{
				RoomName = GameConstants.Network.ROOM_NAME_PLAYTEST,
				MapConfig = mapConfig,
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
			var mapConfig = ((DropdownMenuOption) _mapSelection.options[_mapSelection.value]).MapConfig;
			var message = new PlayCreateRoomClickedMessage
			{
				RoomName = roomName,
				MapConfig = mapConfig
			};

			_services.MessageBrokerService.Publish(message);
			Data.PlayClicked();
		}

		private void FillMapSelectionList()
		{
			_mapSelection.options.Clear();
			
			var configs = _services.ConfigsProvider.GetConfigsDictionary<QuantumMapConfig>();

			foreach (var config in configs.Values)
			{
				if (config.GameModes.Contains(_services.GameModeService.SelectedGameMode.Value.Id) && 
				         (!config.IsTestMap || Debug.isDebugBuild))
				{
					_mapSelection.options.Add(new DropdownMenuOption(config.Map.GetTranslation(), config));
				}
			}

			_mapSelection.RefreshShownValue();
		}

		private class DropdownMenuOption : TMP_Dropdown.OptionData
		{
			public QuantumMapConfig MapConfig { get; set; }
			public DropdownMenuOption(string text, QuantumMapConfig mapConfig) : base(text)
			{
				MapConfig = mapConfig;
			}
		}
	}
}