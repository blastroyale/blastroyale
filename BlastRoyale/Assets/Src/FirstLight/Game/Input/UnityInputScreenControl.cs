using UnityEngine;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

namespace FirstLight.Game.Input
{
	/// <summary>
	/// Point of access to feed data into Unity's new Input System.
	/// </summary>
	public class UnityInputScreenControl : OnScreenControl
	{
		[InputControl]
		[SerializeField]
		private string _controlPath;
		protected override string controlPathInternal
		{
			get => _controlPath;
			set => _controlPath = value;
		}

		/// <summary>
		/// Send an input to the control path specified in the inspector.
		/// </summary>
		public new void SendValueToControl<T>(T v) where T : struct
		{
			base.SendValueToControl(v);
		}
	}
}