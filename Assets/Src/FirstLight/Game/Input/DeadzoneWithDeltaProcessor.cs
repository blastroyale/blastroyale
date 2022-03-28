using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Input
{
	
#if UNITY_EDITOR
	[InitializeOnLoad]
#endif

	/// <summary>
	/// This processor takes a Vector2 input, and zeros it if the magnitude is not above the minimum threshold.
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

		// X, Y = POSITION (NORMALIZED)
		// Z = DELTA SINCE LAST FRAME (calculated in runtime scripts)
		public override Vector3 Process(Vector3 value, InputControl control)
		{
			Vector2 deltaFromCenter = new Vector2(value.x, value.y);
			float deltaSinceLastFrame = value.z;
			
			// Deadzone will be ignored if deltaSinceLast frame 
			if (deltaFromCenter.magnitude < DeadzoneMin && deltaSinceLastFrame < DeltaMagnitudeMin)
			{
				deltaFromCenter = Vector2.zero;
			}
			else if (deltaFromCenter.magnitude > DeadzoneMax)
			{
				deltaFromCenter = deltaFromCenter.normalized;
			}

			return new Vector4(deltaFromCenter.x, deltaFromCenter.y, deltaSinceLastFrame);
		}
	}
}