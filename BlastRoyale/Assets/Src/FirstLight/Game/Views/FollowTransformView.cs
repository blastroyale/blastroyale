using UnityEngine;

namespace FirstLight.Game.Views
{
	/// <summary>
	/// Follows a target transform around.
	/// </summary>
	public class FollowTransformView : MonoBehaviour
	{
		private Transform _target;

		private void Update()
		{
			if (_target != null)
			{
				transform.position = _target.position;
			}
		}

		/// <summary>
		/// Sets the target to follow. Set to null to stop following.
		/// </summary>
		public void SetTarget(Transform target)
		{
			_target = target;
		}
	}
}