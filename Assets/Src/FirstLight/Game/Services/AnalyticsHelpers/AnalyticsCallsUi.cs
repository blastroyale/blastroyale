using System;
using System.Collections.Generic;
using FirstLight.UiService;

namespace FirstLight.Game.Services.AnalyticsHelpers
{
	public static class UIAnalyticsButtonsNames
	{
		public static readonly string PlayAsGuest = "play_as_guest";
		public static readonly string DiscordLink = "discord_link";
		public static readonly string YoutubeLink = "youtube_link";
		public static readonly string InstagramLink = "instagram_link";
		public static readonly string TiktokLink = "tiktok_link";
		public static readonly string Login = "login";
	}
	
	/// <summary>
	/// Analytics helper class regarding UI events
	/// </summary>
	public class AnalyticsCallsUi : AnalyticsCalls
	{
		public AnalyticsCallsUi(IAnalyticsService analyticsService, IUiService uiService) : base(analyticsService)
		{
			uiService.ScreenStartOpening += t => ScreenView(t.ToString());
		}
		
		/// <summary>
		/// Logs when the user opens a screen
		/// </summary>
		/// <param name="screenName">A name that identifies the screen we opened</param>
		public void ScreenView(string screenName)
		{
			screenName = screenName.Replace("FirstLight.Game.Presenters.", "");
			screenName = screenName.Replace("Presenter", "");

			var data = new Dictionary<string, object>
			{
				{"screen_name", screenName}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.ScreenView, data);
		}

		/// <summary>
		/// Logs when the user clicks a button
		/// </summary>
		/// <param name="buttonName">A name that identifies the button we clicked</param>
		public void ButtonAction(string buttonName)
		{
			var data = new Dictionary<string, object>
			{
				{"button", buttonName}
			};
			
			_analyticsService.LogEvent(AnalyticsEvents.ButtonAction, data);
		}
	}
}