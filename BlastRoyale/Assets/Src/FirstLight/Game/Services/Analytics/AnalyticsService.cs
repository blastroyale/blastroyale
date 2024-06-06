using Firebase.Analytics;
using FirstLight.Game.Logic;
using FirstLight.Game.Services.Analytics.Events;
using FirstLight.Game.Utils;

namespace FirstLight.Game.Services.Analytics
{
	/// <summary>
	/// The analytics service is an endpoint in the game to log custom events to Game's analytics console
	/// </summary>
	public interface IAnalyticsService
	{
		/// <inheritdoc cref="AnalyticsCallsMatch"/>
		public AnalyticsCallsMatch MatchCalls { get; }

		/// <inheritdoc cref="AnalyticsCallsUi"/>
		public AnalyticsCallsUi UiCalls { get; }

		/// <inheritdoc cref="AnalyticsCallsTutorial"/>
		public AnalyticsCallsTutorial TutorialCalls { get; }

		/// <summary>
		/// Logs an analytics event.
		/// </summary>
		/// <param name="e"></param>
		void LogEvent(FLEvent e);
	}

	/// <inheritdoc />
	public class AnalyticsService : IAnalyticsService
	{
		public AnalyticsCallsMatch MatchCalls { get; }
		public AnalyticsCallsTutorial TutorialCalls { get; }
		public AnalyticsCallsUi UiCalls { get; }
		public AnalyticsCallLeveling LevelingCalls { get; }

		public AnalyticsService(IGameServices services, IGameDataProvider gameDataProvider, UIService.UIService uiService)
		{
			MatchCalls = new AnalyticsCallsMatch(this, services);
			TutorialCalls = new AnalyticsCallsTutorial(this);
			UiCalls = new AnalyticsCallsUi(this, uiService);
			LevelingCalls = new AnalyticsCallLeveling(this, services, gameDataProvider);
		}

		public void LogEvent(FLEvent e)
		{
			if (!ATTrackingUtils.IsTrackingAllowed()) return;

			// Unity
			Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(e);

			// Firebase
			FirebaseAnalytics.LogEvent(e.ToFirebaseEventName(), e.ToFirebaseParameters());
		}
	}
}