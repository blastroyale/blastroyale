using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This Mono component binds INotification notifications from a timeline with a unity event
	/// </summary>
	public class TimelineBindMonoComponent : MonoBehaviour, INotificationReceiver
	{
		[SerializeField] private UnityEvent<Playable, INotification, object> _receivers;
		
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			_receivers?.Invoke(origin, notification, context);
		}
	}
}