using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.UIElements.Events
{
	public class JoystickEvent : EventBase<JoystickEvent>
	{
		public Vector2 Direction { get; set; }

		public static JoystickEvent GetPooled(Vector2 direction)
		{
			var e = GetPooled();
			e.Direction = direction;
			return e;
		}
	}
}