using System;
using System.Collections.Generic;
using FirstLight.Game.Configs;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using I2.Loc;
using Quantum;
using TMPro;
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
		
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _createDeathmatchRoomButton;
		[SerializeField] private Button _joinRoomButton;
		[SerializeField] private TMP_Dropdown _mapSelection;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			FillMapSelectionList();

			_closeButton.onClick.AddListener(Close);
			_createDeathmatchRoomButton.onClick.AddListener(CreateRoomClicked);
			_joinRoomButton.onClick.AddListener(JoinRoomClicked);
		}

		protected override void Close()
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

		private void CreateRoomClicked()
		{
			// Room code should be short and easily shareable, visible on the UI. Up to 6 trailing 0s
			var roomName = Random.Range(100000, 999999).ToString("F0");

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
			
			var configs = _services.ConfigsProvider.GetConfigsDictionary<MapConfig>();

			foreach (var config in configs.Values)
			{
				if (Debug.isDebugBuild)
				{
					var roomName = config.Map + " - " + config.GameMode + (config.IsTestMap ? " (Test)" : "");
					_mapSelection.options.Add(new DropdownMenuOption(roomName, config));
				}
				else if (config.GameMode == GameMode.Deathmatch && !config.IsTestMap)
				{
					_mapSelection.options.Add(new DropdownMenuOption(config.Map.GetTranslation(), config));
				}

			}
		}

		private class DropdownMenuOption : TMP_Dropdown.OptionData
		{
			public MapConfig MapConfig { get; set; }
			public DropdownMenuOption(string text, MapConfig mapConfig) : base(text)
			{
				MapConfig = mapConfig;
			}
		}
	}
}