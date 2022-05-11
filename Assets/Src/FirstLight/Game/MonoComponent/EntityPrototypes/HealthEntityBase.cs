using FirstLight.Game.Messages;
using Quantum;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FirstLight.Game.MonoComponent.EntityPrototypes
{
	/// <inheritdoc />
	/// <remarks>
	/// Implement this class for entity prototypes with <seeref name="Health"/> during the game simulations
	/// </remarks>
	public abstract class HealthEntityBase : EntityBase
	{
		[SerializeField, Required] private Transform _healthBarAnchor;

		/// <summary>
		/// The <see cref="Transform"/> anchor values to attach the avatars health bar
		/// </summary>
		public Transform HealthBarAnchor => _healthBarAnchor;

		protected override void OnEntityInstantiated(QuantumGame game)
		{
			Services.MessageBrokerService.Publish(new HealthEntityInstantiatedMessage { Entity = EntityView, Game = game });
		}
		
		protected override void OnEntityDestroyed(QuantumGame game)
		{
			Services.MessageBrokerService.Publish(new HealthEntityDestroyedMessage { Entity = EntityView, Game = game });
		}
	}
}