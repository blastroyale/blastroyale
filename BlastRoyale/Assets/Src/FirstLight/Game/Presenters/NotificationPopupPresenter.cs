using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
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
			public readonly string Message;
			public readonly CancellationToken CancellationToken;

			public StateData(string message, CancellationToken cancellationToken)
			{
				Message = message;
				CancellationToken = cancellationToken;
			}
		}

		private const float CLOSE_DELAY = 2f;

		private Label _messageLabel;

		protected override void QueryElements()
		{
			_messageLabel = Root.Q<Label>("NotificationLabel").Required();
		}

		protected override UniTask OnScreenOpen(bool reload)
		{
			_messageLabel.text = Data.Message;
			return base.OnScreenOpen(reload);
		}

		protected override async UniTask OnScreenClose()
		{
			try
			{

				await UniTask.WaitForSeconds(CLOSE_DELAY, cancellationToken: Data.CancellationToken);
			}
			catch (OperationCanceledException)
			{
			}
		}
	}
}