namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when the player completes tha blast pass.
	/// </summary>
	public class BlastPassCompletedEvent : FLEvent
	{
		public BlastPassCompletedEvent(int bpSeason) :
			base("blast_pass_completed")
		{
			SetParameter(AnalyticsParameters.BP_SEASON, bpSeason);
		}
	}
}