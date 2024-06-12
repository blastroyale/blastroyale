using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Presenters;

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

		public void QueueNotification(string message)
		{
			_messages.Enqueue(message);
			ProcessQueue().Forget();
		}

		private async UniTaskVoid ProcessQueue()
		{
			if (_isProcessingQueue) return;

			_isProcessingQueue = true;
			while (_messages.Count > 0)
			{
				// TODO: Not the best since we always destroy and create the screen
				FLog.Info("PACO NotificationService ProcessQueueStart");
				await _uiService.OpenScreen<NotificationPopupPresenter>(new NotificationPopupPresenter.StateData(_messages.Dequeue()));
				await _uiService.CloseScreen<NotificationPopupPresenter>();
				FLog.Info("PACO NotificationService ProcessQueueEnd");
			}

			_isProcessingQueue = false;
		}
	}
}