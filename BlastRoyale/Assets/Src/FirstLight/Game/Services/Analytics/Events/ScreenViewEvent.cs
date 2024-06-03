namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when a UI screen is opened.
	/// </summary>
	public class ScreenViewEvent : FLEvent
	{
		public ScreenViewEvent(string screenName) :
			base("screen_view")
		{
			SetParameter(AnalyticsParameters.SCREEN_NAME, screenName);
		}
	}
}