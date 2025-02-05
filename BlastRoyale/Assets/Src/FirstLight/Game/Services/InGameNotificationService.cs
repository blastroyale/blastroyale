using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Configs;
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
	public class InGameNotificationService
	{
		private readonly UIService.UIService _uiService;

		private readonly List<InGameNotificationEntry> _messages = new ();
		private bool _isProcessingQueue;
		private CancellationTokenSource _currentNotificationToken;
		private InGameNotificationEntry _currentInGameNotificationMessage;

		public InGameNotificationService(UIService.UIService uiService)
		{
			_uiService = uiService;
		}

		public void QueueNotification(string message, InGameNotificationStyle style = InGameNotificationStyle.Info,
									  InGameNotificationDuration duration = InGameNotificationDuration.Normal)
		{
			if (_messages.Count > 0 && _messages[^1].Message == message) return; // Skip duplicates
			if (_isProcessingQueue && _currentInGameNotificationMessage.Message == message)
			{
				// If we are playing the current message already, lets play the popup effect again
				_currentNotificationToken.Cancel();
				_messages.Insert(0, new InGameNotificationEntry()
				{
					Duration = duration,
					Message = message,
					Style = style
				});
				return;
			}

			_messages.Add(new InGameNotificationEntry()
			{
				Duration = duration,
				Message = message,
				Style = style
			});
			ProcessQueue().Forget();
		}

		private async UniTaskVoid ProcessQueue()
		{
			if (_isProcessingQueue) return;

			_isProcessingQueue = true;
			while (_messages.Count > 0)
			{
				_currentNotificationToken = new CancellationTokenSource();
				_currentInGameNotificationMessage = _messages[0];
				_messages.RemoveAt(0);
				// TODO: Not the best since we always destroy and create the screen
				await _uiService.OpenScreen<NotificationPopupPresenter>(
					new NotificationPopupPresenter.StateData(_currentInGameNotificationMessage, _currentNotificationToken.Token));
				await _uiService.CloseScreen<NotificationPopupPresenter>();
			}

			_isProcessingQueue = false;
		}
	}

	public class InGameNotificationEntry
	{
		public string Message;
		public InGameNotificationStyle Style;
		public InGameNotificationDuration Duration;
	}

	public enum InGameNotificationDuration
	{
		Normal,
		Long,
	}

	public enum InGameNotificationStyle
	{
		Info,
		Error
	}
}