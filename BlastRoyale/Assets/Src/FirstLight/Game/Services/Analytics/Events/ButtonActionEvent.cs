namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when a specific button is pressed
	/// </summary>
	public class ButtonActionEvent : FLEvent
	{
		public ButtonActionEvent(string buttonName) :
			base("button_action")
		{
			SetParameter(AnalyticsParameters.BUTTON_NAME, buttonName);
		}
	}
}