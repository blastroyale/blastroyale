using System;
using FirstLight.Game.Ids;
using FirstLight.Game.Services;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using FirstLight.Game.Logic;
using FirstLight.Game.Messages;
using I2.Loc;
using MoreMountains.NiceVibrations;
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
		[SerializeField] private Button _createRoomButton;
		[SerializeField] private Button _joinRoomButton;

		private IGameDataProvider _gameDataProvider;
		private IGameServices _services;

		private void Awake()
		{
			_gameDataProvider = MainInstaller.Resolve<IGameDataProvider>();
			_services = MainInstaller.Resolve<IGameServices>();

			_closeButton.onClick.AddListener(Close);
			_createRoomButton.onClick.AddListener(CreateRoomClicked);
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
			                                                    "", confirmButton, true);
		}

		private void OnRoomJoinClicked(string roomNameInput)
		{
			Data.PlayClicked.Invoke();
			_services.MessageBrokerService.Publish(new RoomJoinClickedMessage(){ RoomName = roomNameInput });
		}

		private void CreateRoomClicked()
		{
			// Room code should be short and easily shareable, visible on the UI. Up to 6 trailing 0s
			string roomName = Random.Range(0, 999999).ToString("N6");

			Data.PlayClicked.Invoke();
			_services.MessageBrokerService.Publish(new RoomCreateClickedMessage(){ RoomName = roomName });
		}
	}
}