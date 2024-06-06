namespace FirstLight.Game.Services.Analytics.Events
{
	/// <summary>
	/// Triggered when the player levels up their battle pass
	/// </summary>
	public class BlastPassLevelUpEvent : FLEvent
	{
		public BlastPassLevelUpEvent(int bpSeason, int bpLevel) :
			base("blast_pass_level_up")
		{
			SetParameter(AnalyticsParameters.BP_SEASON, bpSeason);
			SetParameter(AnalyticsParameters.BP_LEVEL, bpLevel);
		}
	}
}