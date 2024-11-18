using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	/// <summary>
	/// A presenter for general popups that show a message to the user.
	/// </summary>
	[UILayer(UILayer.Notifications)]
	public class NotificationPopupPresenter : UIPresenterData<NotificationPopupPresenter.StateData>
	{
		public class StateData
		{
			public readonly InGameNotificationEntry Message;
			public readonly CancellationToken CancellationToken;

			public StateData(InGameNotificationEntry message, CancellationToken cancellationToken)
			{
				Message = message;
				CancellationToken = cancellationToken;
			}
		}

		private const float CLOSE_DELAY = 2f;
		private const float CLOSE_DELAY_LONG = 6f;
		private const string USS_NOTIFICATION = "notification";
		private const string USS_NOTIFICATION_ERROR_MODIFIER = USS_NOTIFICATION + "--error";

		private Label _messageLabel;

		protected override void QueryElements()
		{
			_messageLabel = Root.Q<Label>("NotificationLabel").Required();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_messageLabel.text = Data.Message.Message;
			if (Data.Message.Style == InGameNotificationStyle.Error)
			{
				_messageLabel.AddToClassList(USS_NOTIFICATION_ERROR_MODIFIER);
			}
			else
			{
				_messageLabel.RemoveModifiers();
			}

			return base.OnScreenOpen(reload);
		}

		protected override async UniTask OnScreenClose()
		{
			try
			{
				var delay = Data.Message.Duration == InGameNotificationDuration.Normal ? CLOSE_DELAY : CLOSE_DELAY_LONG;
				await UniTask.WaitForSeconds(delay, cancellationToken: Data.CancellationToken);
			}
			catch (OperationCanceledException)
			{
			}
		}
	}
}