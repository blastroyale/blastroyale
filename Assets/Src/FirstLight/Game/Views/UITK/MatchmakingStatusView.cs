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
				_timeLabel.schedule.Execute(ts =>
				{
					_timeLabel.text = $"{TimeSpan.FromMilliseconds(ts.now - ts.start):mm\\:ss}";
				}).Every(200); // Triggered every 200ms so the second counting is smooth and consistent.
			}
		}
	}
}