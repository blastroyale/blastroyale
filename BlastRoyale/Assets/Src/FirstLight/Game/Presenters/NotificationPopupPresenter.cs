using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine.UIElements;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Notifications)]
	public class NotificationPopupPresenter : UIPresenterData<NotificationPopupPresenter.StateData>
	{
		public class StateData
		{
			public readonly string Message;

			public StateData(string message)
			{
				Message = message;
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
			await UniTask.WaitForSeconds(CLOSE_DELAY);
		}
	}
}