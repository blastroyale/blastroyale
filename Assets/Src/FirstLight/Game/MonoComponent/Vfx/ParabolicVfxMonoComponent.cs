using FirstLight.Game.Ids;
using FirstLight.Services;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.Vfx
{
	/// <inheritdoc cref="Vfx"/>
	/// <remarks>
	/// This vfx plays a parabolic visual and will despawn when reaching the target
	/// </remarks>
	[RequireComponent(typeof(Rigidbody))]
	public class ParabolicVfxMonoComponent : Vfx<VfxId>
	{
		[SerializeField, Required] private Rigidbody _rigidbody;

		private void OnValidate()
		{
			_rigidbody = _rigidbody == null ? GetComponent<Rigidbody>() : _rigidbody;
		}

		/// <summary>
		/// Starts the parabolic vfx to play during the given <paramref name="flyTime"/> to reach the given <paramref name="targetPosition"/>
		/// </summary>
		public void StartParabolic(Vector3 targetPosition, float flyTime)
		{
			var forceDirection = targetPosition - transform.position;
			var force = -_rigidbody.velocity;
			var yForce = Mathf.Abs(Physics.gravity.y) * Mathf.Pow(flyTime, 2) / 2;

			force.x += forceDirection.x / flyTime;
			force.y += (forceDirection.y + yForce) / flyTime;
			force.z += forceDirection.z / flyTime;

			_rigidbody.AddForce(force, ForceMode.VelocityChange);

			Despawner(flyTime);
		}
	}
}