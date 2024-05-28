using System.Collections.Generic;
using UnityEngine;

namespace FirstLight.Game.MonoComponent
{
	/// <summary>
	/// This MonoComponent acts as a container of all <see cref="Rigidbody"/> inside of this GameObject
	/// </summary>
	public class RigidbodyContainerMonoComponent : MonoBehaviour
	{
		[SerializeField] private List<Rigidbody> _rigidbodies = new List<Rigidbody>();
		
		private void OnValidate()
		{
			var rigidbodies = GetComponentsInChildren<Rigidbody>(true);

			foreach (var body in rigidbodies)
			{
				_rigidbodies.Add(body);
			}
		}

		/// <summary>
		/// Set's the rigidbody to the given <paramref name="state"/>
		/// </summary>
		public void SetState(bool state)
		{
			foreach (var body in _rigidbodies)
			{
				body.detectCollisions = state;
				body.isKinematic = !state;

				if (!state)
				{
					body.velocity = Vector3.zero;
					body.angularVelocity = Vector3.zero;
					body.transform.localPosition = Vector3.zero;
				}
			}
		}

		/// <summary>
		/// Adds a force to all bodies in this container
		/// </summary>
		public void AddForce(Vector3 direction, ForceMode forceMode)
		{
			foreach (var body in _rigidbodies)
			{
				body.detectCollisions = true;
				body.isKinematic = false;
				
				body.AddForce(direction, forceMode);
			}
		}
	}
}