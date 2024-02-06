using frame8.Logic.Misc.Other.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FirstLight.Game.Input
{
#if UNITY_EDITOR
	[InitializeOnLoad]
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
			value = value.Rotate(-FLGCamera.Instance.MainCamera.transform.rotation.eulerAngles.y);

			return value;
		}
	}
}