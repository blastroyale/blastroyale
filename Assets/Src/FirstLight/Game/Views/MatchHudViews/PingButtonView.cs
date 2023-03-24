using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace FirstLight.Game.Views.MatchHudViews
{
	/// <summary>
	/// Handles the connection to the input system for the Ping button.
	/// </summary>
	public class PingButtonView:  OnScreenControl, IPointerUpHandler
	{
		[InputControl(layout = "Vector2")]
		[SerializeField]
		private string _controlPath;

		protected override string controlPathInternal
		{
			get => _controlPath;
			set => _controlPath = value;
		}

		public void OnPointerUp(PointerEventData eventData)
		{
			SendValueToControl(eventData.position);
		}
	}
}