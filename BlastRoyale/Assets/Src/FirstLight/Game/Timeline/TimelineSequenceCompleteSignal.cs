using UnityEngine.Timeline;

namespace FirstLight.Game.Signals
{
	/// <summary>
	/// This Signal is sent when a section of a Timeline sequence is complete. 
	/// However, this signal is not to be used when the entire Timeline sequence is complete, as that is handled by <seealso cref="DirectorCompleteSignal"/>
	/// </summary>
	public class TimelineSequenceCompleteSignal : SignalEmitter
	{
	}
}