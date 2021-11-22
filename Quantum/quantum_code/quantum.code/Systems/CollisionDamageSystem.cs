using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles hazards that are responsible for the collision damage from enemies
	/// </summary>
	public unsafe class CollisionDamageSystem : SystemMainThreadFilter<CollisionDamageSystem.CollisionDamageFilter>, 
	                                            ISignalOnComponentRemoved<CollisionDamage>
	{
		public struct CollisionDamageFilter
		{
			public EntityRef Entity;
			public CollisionDamage* CollisionDamage;
			public Transform3D* EnemyTransform3D;
		}
		
		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, CollisionDamage* component)
		{
			f.Add<EntityDestroyer>(component->Hazard);
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref CollisionDamageFilter filter)
		{
			if (!f.Unsafe.TryGetPointer<Transform3D>(filter.CollisionDamage->Hazard, out var hazardTransform))
			{
				return;
			}

			var newPosition = filter.EnemyTransform3D->Position;
			hazardTransform->Position = newPosition + FPVector3.Up * Constants.ACTOR_AS_TARGET_Y_OFFSET;
		}
	}
}