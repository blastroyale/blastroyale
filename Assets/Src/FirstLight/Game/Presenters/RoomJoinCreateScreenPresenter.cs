using System;
using FirstLight.Game.Services;
using UnityEngine;
using FirstLight.Game.Utils;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using I2.Loc;
using Quantum;
using Sirenix.OdinInspector;
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
		
		[SerializeField, Required] private Button _closeButton;
		[SerializeField, Required] private Button _createDeathmatchRoomButton;
		[SerializeField, Required] private Button _createBattleRoyaleRoomButton;
		[SerializeField, Required] private Button _joinRoomButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();
			
			_createBattleRoyaleRoomButton.gameObject.SetActive(Debug.isDebugBuild);

			_closeButton.onClick.AddListener(Close);
			_createDeathmatchRoomButton.onClick.AddListener(CreateDeathmatchRoom);
			_createBattleRoyaleRoomButton.onClick.AddListener(CreateBattleRoyaleRoom);
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

		private void CreateDeathmatchRoom()
		{
			CreateRoomClicked(GameMode.Deathmatch);
		}

		private void CreateBattleRoyaleRoom()
		{
			CreateRoomClicked(GameMode.BattleRoyale);
		}

		private void CreateRoomClicked(GameMode gameMode)
		{
			// Room code should be short and easily shareable, visible on the UI. Up to 6 trailing 0s
			var roomName = Random.Range(100000, 999999).ToString("F0");
			var message = new PlayCreateRoomClickedMessage
			{
				RoomName = roomName,
				GameMode = gameMode
			};

			_services.MessageBrokerService.Publish(message);
			Data.PlayClicked();
		}
	}
}