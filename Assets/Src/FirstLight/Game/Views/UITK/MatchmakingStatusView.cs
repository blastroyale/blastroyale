using System;
using FirstLight.FLogger;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.UiService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Shows the matchmaking status on the HomeScreen
	/// </summary>
	public class MatchmakingStatusView : IUIView
	{
		private const string UssContainerHidden = "matchmaking-container--hidden";

		private VisualElement _container;
		private Label _timeLabel;

		private long _startTime;

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
			_container.EnableInClassList(UssContainerHidden, !show);

			if (show)
			{
				_startTime = 0;
				_timeLabel.schedule.Execute(ts =>
				{
					if (_startTime == 0L) _startTime = ts.now;

					_timeLabel.text = $"{TimeSpan.FromMilliseconds(_startTime - ts.start):mm\\:ss}";
				}).Every(200); // Triggered every 200ms so the second counting is smooth and consistent.
			}
		}
	}
}