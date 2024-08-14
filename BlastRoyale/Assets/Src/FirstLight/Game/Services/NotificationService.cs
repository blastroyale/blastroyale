using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data.DataTypes;
using FirstLight.Game.Presenters;
using FirstLight.Game.Utils;
using Unity.Services.Friends;
using Unity.Services.Friends.Notifications;

namespace FirstLight.Game.Services
{
	/// <summary>
	/// Handles showing "async" notifications to the player.
	/// </summary>
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

			if (message.MessageType == FriendMessage.FriendMessageType.Cancel)
			{
				if (_uiService.IsScreenOpen<InvitePopupPresenter>())
				{
					var screen = _uiService.GetScreen<InvitePopupPresenter>();
					if (screen.LobbyCode == message.LobbyID)
					{
						_uiService.CloseScreen<InvitePopupPresenter>().Forget();
						return;
					}
				}
			}

			// We skip inviting to party if the player already has an invite open
			if (_uiService.IsScreenOpen<InvitePopupPresenter>()) return;
			var services = MainInstaller.ResolveServices();
			switch (message.MessageType)
			{
				case FriendMessage.FriendMessageType.Invite:
					if (!services.GameSocialService.GetCurrentPlayerActivity().CanReceiveInvite())
					{
						FriendsService.Instance.MessageAsync(e.UserId, FriendMessage.CreateDecline(message.LobbyID, message.InviteType)).AsUniTask().Forget();
						return;
					}

					_uiService.OpenScreen<InvitePopupPresenter>(new InvitePopupPresenter.StateData
					{
						Type = message.InviteType,
						SenderID = e.UserId,
						LobbyCode = message.LobbyID
					}).Forget();
					break;
				case FriendMessage.FriendMessageType.Decline:
					var callbacks = message.InviteType == FriendMessage.FriendInviteType.Match ? services.FLLobbyService.CurrentMatchCallbacks : services.FLLobbyService.CurrentPartyCallbacks;
					callbacks.TriggerInviteDeclined(e.UserId);
					break;
				case FriendMessage.FriendMessageType.Cancel:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
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