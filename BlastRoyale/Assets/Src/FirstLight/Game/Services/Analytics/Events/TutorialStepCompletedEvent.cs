namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when a tutorial step is completed. Duh.
	/// </summary>
	public class TutorialStepCompletedEvent : FLEvent
	{
		public TutorialStepCompletedEvent(int currentStepNumber, string currentStepName) :
			base("tutorial_step_completed")
		{
			SetParameter(AnalyticsParameters.TUTORIAL_STEP_NUMBER, currentStepNumber);
			SetParameter(AnalyticsParameters.TUTORIAL_STEP_NAME, currentStepName);
		}
	}
}