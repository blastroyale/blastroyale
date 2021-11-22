using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// Handles specifics for Stun status modifier
	/// </summary>
	public unsafe class StunSystem : SystemSignalsOnly, 
	                                 ISignalOnComponentAdded<Stun>, 
	                                 ISignalProjectileTargetHit, ISignalStatusModifierCancelled
	{
		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Stun* component)
		{
			// Stop the movement
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(entity, out var navMeshPathfinder))
			{
				navMeshPathfinder->Stop(f, entity, true);
			}
			
			var agent = f.Unsafe.GetPointer<HFSMAgent>(entity);
			var bbComponent = f.Unsafe.GetPointer<AIBlackboardComponent>(entity);
			
			bbComponent->Set(f, Constants.STUN_DURATION_BB_KEY, f.Get<Stats>(entity).CurrentStatusModifierDuration);
			HFSMManager.TriggerEvent(f, &agent->Data, entity, Constants.STUNNED_EVENT);
		}
		
		/// <inheritdoc />
		public void ProjectileTargetHit(Frame f, ProjectileHitData* data)
		{
			var projectileData = f.Get<Projectile>(data->Projectile).Data;
			
			if (projectileData.StunDuration > FP._0 && !data->IsStaticHit && f.Has<HFSMAgent>(data->TargetHit) &&
			    f.TryGet<Stats>(data->TargetHit, out var stats) && stats.CurrentHealth > 0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, data->TargetHit, StatusModifierType.Stun, projectileData.StunDuration);
			}
		}

		/// <inheritdoc />
		public void StatusModifierCancelled(Frame f, EntityRef entity, StatusModifierType type)
		{
			if (type == StatusModifierType.Stun && f.Unsafe.TryGetPointer<HFSMAgent>(entity, out var agent))
			{
				HFSMManager.TriggerEvent(f, &agent->Data, entity, Constants.STUN_CANCELLED_EVENT);
			}
		}
	}
}