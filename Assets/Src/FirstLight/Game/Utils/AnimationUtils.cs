using Cinemachine;
using UnityEngine;

namespace FirstLight.Game.Utils
{
	/// <summary>
	/// Helper methods that work with animation.
	/// </summary>
	public static class AnimationUtils
	{
		
		/// <summary>
		/// Snaps the camera to it's target position / rotation (by doing an internal update which
		/// makes the camera think 10 seconds have passed).
		///
		/// It's a hacky way to force the camera to evaluate the blend to the next follow target (so we snap to it).
		/// </summary>
		public static void SnapCamera(this ICinemachineCamera cam)
		{
			cam.UpdateCameraState(Vector3.up, 10f);
		}
	}
}