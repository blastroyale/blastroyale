using UnityEngine.Timeline;

namespace FirstLight.Game.Signals
{
	/// <summary>
	/// This Signal is sent when a Timeline has completed in it's entirety.
	/// This signal is not to be used when a subsection of a Timeline sequence is complete, as that is handled by <seealso cref="TimelineSequenceCompleteSignal"/>
	/// </summary>
	public class DirectorCompleteSignal : SignalEmitter
	{
	}
}