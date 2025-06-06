using FirstLight.Game.Domains.VFX;
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
	public class ParabolicVfxMonoBehaviour : VfxMonoBehaviour
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
			Vector3 force = Vector3.zero;
			
			if (flyTime > 0)
			{
				force = -_rigidbody.linearVelocity;
				
				var forceDirection = targetPosition - transform.position;
				var yForce = Mathf.Abs(Physics.gravity.y) * Mathf.Pow(flyTime, 2) / 2;

				force.x += forceDirection.x / flyTime;
				force.y += (forceDirection.y + yForce) / flyTime;
				force.z += forceDirection.z / flyTime;
			}
			
			_rigidbody.AddForce(force, ForceMode.VelocityChange);

			Despawner(flyTime).Forget();
		}
	}
}