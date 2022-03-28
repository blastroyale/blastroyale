using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Input
{
	
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif

	/// <summary>
	/// This processor takes a Vector3 input (XY pos + delta), and runs a standard deadzone calculation
	/// plus a "delta passthrough" that ignores the deadzone if delta is above a threshold 
	/// </summary>
	public class DeadzoneWithDeltaProcessor : InputProcessor<Vector3>
	{
		public float DeadzoneMin = 0.115f;
		public float DeadzoneMax = 0.925f;
		public float DeltaMagnitudeMin = 1f;

#if UNITY_EDITOR
		static DeadzoneWithDeltaProcessor()
		{
			Initialize();
		}
#endif

		[RuntimeInitializeOnLoadMethod]
		static void Initialize()
		{
			InputSystem.RegisterProcessor<DeadzoneWithDeltaProcessor>();
		}

		/// <summary>
		/// Processes the input for the passed Vector3 - deadzone + delta passthrough calculation
		/// </summary>
		/// <param name="value">XY = Position, Z = delta diff since last frame</param>
		public override Vector3 Process(Vector3 value, InputControl control)
		{
			Vector2 deltaFromCenter = new Vector2(value.x, value.y);
			float deltaSinceLastFrame = value.z;
			
			if (deltaFromCenter.magnitude < DeadzoneMin && deltaSinceLastFrame < DeltaMagnitudeMin)
			{
				deltaFromCenter = Vector2.zero;
			}
			else if (deltaFromCenter.magnitude > DeadzoneMax)
			{
				deltaFromCenter = deltaFromCenter.normalized;
			}

			return new Vector3(deltaFromCenter.x, deltaFromCenter.y, deltaSinceLastFrame);
		}
	}
}