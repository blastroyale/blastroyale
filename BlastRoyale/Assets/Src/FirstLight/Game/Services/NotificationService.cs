using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using Unity.Services.Friends;
using Unity.Services.Friends.Exceptions;
using Unity.Services.Friends.Notifications;
using Unity.Services.Lobbies;

namespace FirstLight.Game.Services
{
	public class NotificationService
	{
		private readonly UIService.UIService _uiService;

		private readonly Queue<string> _messages = new ();
		private bool _isProcessingQueue;

		public NotificationService(UIService.UIService uiService)
		{
			_uiService = uiService;
		}

		public void Init()
		{
			FriendsService.Instance.MessageReceived += OnFriendMessageReceived;
		}

		public void QueueNotification(string message)
		{
			_messages.Enqueue(message);
			ProcessQueue().Forget();
		}

		private void OnFriendMessageReceived(IMessageReceivedEvent e)
		{
			var message = e.GetAs<FriendMessage>();

			// We skip inviting to party if the player already has an invite open
			if (_uiService.IsScreenOpen<PartyInvitePopupPresenter>()) return;

			// TODO mihak: Only allow this if the player is in main menu

			_uiService.OpenScreen<PartyInvitePopupPresenter>(new PartyInvitePopupPresenter.StateData
			{
				SenderID = e.UserId,
				PartyCode = message.LobbyID
			}).Forget();
		}

		private async UniTaskVoid ProcessQueue()
		{
			if (_isProcessingQueue) return;

			_isProcessingQueue = true;
			while (_messages.Count > 0)
			{
				// TODO: Not the best since we always destroy and create the screen
				await _uiService.OpenScreen<NotificationPopupPresenter>(new NotificationPopupPresenter.StateData(_messages.Dequeue()));
				await _uiService.CloseScreen<NotificationPopupPresenter>();
			}

			_isProcessingQueue = false;
		}
	}
}