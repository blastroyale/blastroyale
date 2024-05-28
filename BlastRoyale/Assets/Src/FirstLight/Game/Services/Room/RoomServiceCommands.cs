using ExitGames.Client.Photon;
using FirstLight.Game.Commands;
using Photon.Realtime;

namespace FirstLight.Game.Services.RoomService
{
	public class RoomServiceCommands : IOnEventCallback
	{
		private RoomService _service;

		public RoomServiceCommands(RoomService service)
		{
			_service = service;
			_service._networkService.QuantumClient.AddCallbackTarget(this);
		}


		// This method receives all photon events, but is only used for our custom in-game events
		public void OnEvent(EventData photonEvent)
		{
			// DebugEvent(photonEvent);
			if (photonEvent.Code == (byte) QuantumCustomEvents.KickPlayer)
			{
				OnKickPlayerEventReceived((int) photonEvent.CustomData, photonEvent.Sender);
			}
		}

		internal bool SendKickCommand(Player playerToKick)
		{
			var eventOptions = new RaiseEventOptions() {Receivers = ReceiverGroup.All};
			return _service._networkService.QuantumClient.OpRaiseEvent((byte) QuantumCustomEvents.KickPlayer, playerToKick.ActorNumber,
				eventOptions,
				SendOptions.SendReliable);
		}

		private void OnKickPlayerEventReceived(int userIdToLeave, int senderIndex)
		{
			if (_service.CurrentRoom.LocalPlayer.ActorNumber != userIdToLeave ||
				!_service.InRoom || _service.CurrentRoom.MasterClientId != senderIndex)
			{
				return;
			}

			_service.LeaveRoom();
			_service.InvokeLocalPlayerKicked();
		}
	}
}