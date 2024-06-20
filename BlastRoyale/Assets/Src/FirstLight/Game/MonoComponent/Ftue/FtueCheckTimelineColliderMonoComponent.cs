using FirstLight.Game.MonoComponent.EntityViews;
using FirstLight.Game.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;

namespace FirstLight.Game.MonoComponent.Ftue
{
	/// <summary>
	/// This FTUE mono component changes the playing state of the current timeline when collided by the player
	/// </summary>
	[RequireComponent(typeof(Collider))]
	public class FtueCheckTimelineColliderMonoComponent : MonoBehaviour
	{
		[SerializeField, Required] private PlayableDirector _timeline;
		[SerializeField] private bool _newPlayingState = true;

		private void OnTriggerEnter(Collider other)
		{
			if (!other.gameObject.TryGetComponent<PlayerCharacterViewMonoComponent>(out _))
			{
				return;
			}
			
			if (_newPlayingState)
			{
				_timeline.playableGraph.PlayTimeline();
			}
			else
			{
				_timeline.playableGraph.StopTimeline();
			}
		}
	}
}