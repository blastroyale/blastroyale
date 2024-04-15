using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Input
{
#if UNITY_EDITOR
	[UnityEditor.InitializeOnLoad]
#endif
	public class CameraRotationInputProcessor : InputProcessor<Vector2>
	{
#if UNITY_EDITOR
		static CameraRotationInputProcessor()
		{
			Initialize();
		}
#endif

		[RuntimeInitializeOnLoadMethod]
		static void Initialize()
		{
			InputSystem.RegisterProcessor<CameraRotationInputProcessor>();
		}

		public override Vector2 Process(Vector2 value, InputControl control)
		{
			value = Rotate(value, -FLGCamera.Instance.MainCamera.transform.rotation.eulerAngles.y);

			return value;
		}

		private static Vector2 Rotate(Vector2 v, float degreesCounterClockwise)
		{
			var radians = degreesCounterClockwise * Mathf.Deg2Rad;
			var cos = Mathf.Cos(radians);
			var sin = Mathf.Sin(radians);
			float x = v.x, y = v.y;
			v.x = cos * x - sin * y;
			v.y = sin * x + cos * y;

			return v;
		}
	}
}