using FirstLight.Game.Services.Analytics.Events;

namespace FirstLight.Game.Services.Analytics
{
	public static class UIAnalyticsButtonsNames
	{
		public static readonly string PlayAsGuest = "play_as_guest";
		public static readonly string DiscordLink = "discord_link";
		public static readonly string YoutubeLink = "youtube_link";
		public static readonly string InstagramLink = "instagram_link";
		public static readonly string TiktokLink = "tiktok_link";
	}

	/// <summary>
	/// Analytics helper class regarding UI events
	/// </summary>
	public class AnalyticsCallsUi : AnalyticsCalls
	{
		public AnalyticsCallsUi(IAnalyticsService analyticsService, UIService.UIService uiService) : base(analyticsService)
		{
			uiService.OnScreenOpened += ScreenView;
		}

		private void ScreenView(string screenName, string layerName)
		{
			screenName = screenName.Replace("FirstLight.Game.Presenters.", "");
			screenName = screenName.Replace("Presenter", "");

			_analyticsService.LogEvent(new ScreenViewEvent(screenName));
		}

		/// <summary>
		/// Logs when the user clicks a button
		/// </summary>
		/// <param name="buttonName">A name that identifies the button we clicked</param>
		public void ButtonAction(string buttonName)
		{
			_analyticsService.LogEvent(new ButtonActionEvent(buttonName));
		}
	}
}