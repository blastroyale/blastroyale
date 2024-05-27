using FirstLight.Game.Services.Tutorial;

namespace FirstLight.Game.StateMachines
{
	public interface ITutorialSequence
	{
		/// <summary>
		/// Name of the tutorial section
		/// </summary>
		public string SectionName { get; set; }
		
		/// <summary>
		/// Current iteration for the tutorial section (manually set, CRITICAL for analytics)
		/// </summary>
		public int SectionVersion { get; set; }
		
		/// <summary>
		/// Current step in the tutorial sequence
		/// </summary>
		public TutorialClientStep CurrentStep { get; set; }

		/// <summary>
		/// Resets the tutorial in case something happens
		/// </summary>
		public void Reset();

		/// <summary>
		/// Sends current step analytics, and updates to a new step
		/// </summary>
		public void EnterStep(TutorialClientStep newStep);
	}
}