using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace FirstLight.Game.Input
{
	/// <summary>
	/// Onscreen controls representation for Unity's input system.
	/// </summary>
#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoad]
#endif
	[InputControlLayout(displayName = "Onscreen Controls", stateType = typeof(OnScreenControlsState))]
	public class OnScreenControlsDevice : InputDevice
	{
		public Vector2Control LeftJoystickDirection { get; private set; }
		public ButtonControl LeftJoystickPointerDown { get; private set; }
		public Vector2Control RightJoystickDirection { get; private set; }
		public ButtonControl RightJoystickPointerDown { get; private set; }
		public Vector2Control SpecialAimDirection { get; private set; }
		public ButtonControl Special0PointerDown { get; private set; }
		public ButtonControl Special1PointerDown { get; private set; }
		
		public static OnScreenControlsDevice Current { get; internal set; }

		/// <inheritdoc />
		public override void MakeCurrent()
		{
			base.MakeCurrent();
			
			Current = this;
		}

		/// <inheritdoc />
		protected override void OnRemoved()
		{
			base.OnRemoved();

			if (Current == this)
			{
				Current = null;
			}
		}

		static OnScreenControlsDevice()
		{
			InputSystem.RegisterLayout<OnScreenControlsDevice>();
		}

		protected override void FinishSetup()
		{
			base.FinishSetup();
			
			LeftJoystickDirection = GetChildControl<Vector2Control>("LeftJoystickDirection");
			LeftJoystickPointerDown = GetChildControl<ButtonControl>("LeftJoystickPointerDown");
			
			RightJoystickDirection = GetChildControl<Vector2Control>("RightJoystickDirection");
			RightJoystickPointerDown = GetChildControl<ButtonControl>("RightJoystickPointerDown");
			
			SpecialAimDirection = GetChildControl<Vector2Control>("SpecialAimDirection");
			Special0PointerDown = GetChildControl<ButtonControl>("Special0PointerDown");
			Special1PointerDown = GetChildControl<ButtonControl>("Special1PointerDown");
		}
		
		/// <summary>
		/// Empty init on load to trigger static constructor.
		/// </summary>
		[RuntimeInitializeOnLoadMethod]
		private static void InitializeInPlayer() {}
	}
	
	
	/// <summary>
	/// Onscreen controls state layout for Unity's input system.
	/// </summary>
	[StructLayout(LayoutKind.Explicit, Size = 40)]
	public struct OnScreenControlsState : IInputStateTypeInfo
	{
		public FourCC format => new FourCC('O', 'S', 'C', 'D');

		[InputControl(layout = "Button"), FieldOffset(0)]
		public float LeftJoystickPointerDown;
		[InputControl(layout = "Vector2"), FieldOffset(4)]
		public Vector2 LeftJoystickDirection;
		
		[InputControl(layout = "Button"), FieldOffset(12)]
		public float RightJoystickPointerDown;
		[InputControl(layout = "Vector2"), FieldOffset(16)]
		public Vector2 RightJoystickDirection;
		
		[InputControl(layout = "Vector2"), FieldOffset(24)]
		public Vector2 SpecialAimDirection;
		[InputControl(layout = "Button"), FieldOffset(32)]
		public float Special0PointerDown;
		[InputControl(layout = "Button"), FieldOffset(36)]
		public float Special1PointerDown;
	}
}