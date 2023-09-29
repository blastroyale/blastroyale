using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.Configs.Utils
{
	[Serializable]
	public struct TransformParams
	{
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;
	}

	[Serializable]
	public struct AnimatorParams
	{
		[Required] public bool ApplyRootMotion;
		[Required] public AnimatorUpdateMode UpdateMode;
		[Required] public AnimatorCullingMode CullingMode;
		[Required] public RuntimeAnimatorController Controller;
	}
}