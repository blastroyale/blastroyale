using FirstLight.Game.Services.Analytics;
using FirstLight.Game.Utils;
using FirstLight.UIService;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	/// <summary>
	/// Handles socials buttons logic.
	/// </summary>
	public class SocialsView : UIView
	{
		protected override void Attached()
		{
			var services = MainInstaller.ResolveServices();

			Element.Q<Button>("DiscordButton").Required().clicked += () =>
			{
				services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.DiscordLink);
				Application.OpenURL(GameConstants.Links.DISCORD_SERVER);
			};

			Element.Q<Button>("YoutubeButton").Required().clicked += () =>
			{
				services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.YoutubeLink);
				Application.OpenURL(GameConstants.Links.YOUTUBE_LINK);
			};

			Element.Q<Button>("InstagramButton").Required().clicked += () =>
			{
				services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.InstagramLink);
				Application.OpenURL(GameConstants.Links.INSTAGRAM_LINK);
			};

			Element.Q<Button>("TiktokButton").Required().clicked += () =>
			{
				services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.TiktokLink);
				Application.OpenURL(GameConstants.Links.TIKTOK_LINK);
			};
			
			Element.Q<Button>("TwitterButton").Required().clicked += () =>
			{
				services.AnalyticsService.UiCalls.ButtonAction(UIAnalyticsButtonsNames.TwitterLink);
				Application.OpenURL(GameConstants.Links.TWITTER_LINK);
			};
		}
	}
}