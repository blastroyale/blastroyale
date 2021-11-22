using FirstLight.Game.Utils;
using Quantum;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component changes the playing state of the current timeline when the number of <see cref="DummyCharacter"/>
	/// alive in the world are the same defined by this mono component
	/// </summary>
	public class FtueCheckDummiesKilledMonoComponent : MonoBehaviour
	{
		[SerializeField] private PlayableDirector _timeline;
		[SerializeField] private int _dummiesMaxCount;

		private void Awake()
		{
			QuantumEvent.Subscribe<EventOnDummyCharacterKilled>(this, OnDummyCharacterKilled);
		}

		private void OnDummyCharacterKilled(EventOnDummyCharacterKilled callback)
		{
			if (callback.Game.Frames.Verified.ComponentCount<DummyCharacter>() - 1 <= _dummiesMaxCount)
			{
				_timeline.playableGraph.PlayTimeline();
			}
		}
	}
}