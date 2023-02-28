using System;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Shows the matchmaking status on the HomeScreen
	/// </summary>
	public class MatchmakingStatusView : IUIView
	{
		private VisualElement _container;
		private Label _timeLabel;

		private ValueAnimation<float> _timer;

		public event Action CloseClicked;

		public void Attached(VisualElement element)
		{
			_container = element;
			_timeLabel = element.Q<Label>("Time").Required();
			element.Q<ImageButton>("MatchmakingCloseButton").clicked += () => CloseClicked?.Invoke();
		}

		public void SubscribeToEvents()
		{
		}

		public void UnsubscribeFromEvents()
		{
		}

		public void Show(bool show)
		{
			_container.EnableInClassList("matchmaking-container--hidden", !show);

			_timer?.Stop();
			_timer?.Recycle();

			if (show)
			{
				// A bit of a hacky way to get a timer going. Only problem with this is that it will stop after 1h. Should be plenty
				_timer = _timeLabel.experimental.animation
					.Start(0, 60 * 60, 60 * 60 * 1000,
						(e, t) => { ((Label) e).text = $"{TimeSpan.FromSeconds(t):mm\\:ss}"; })
					.Ease(Easing.Linear);
			}
		}
	}
}