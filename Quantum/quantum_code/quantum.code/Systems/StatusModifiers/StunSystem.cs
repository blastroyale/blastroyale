namespace Quantum.Systems
{
	/// <summary>
	/// Handles specifics for Stun status modifier
	/// </summary>
	public unsafe class StunSystem : SystemSignalsOnly, 
	                                 ISignalOnComponentAdded<Stun>, ISignalStatusModifierCancelled
	{
		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Stun* component)
		{
			// Stop the movement
			if (f.Unsafe.TryGetPointer<NavMeshPathfinder>(entity, out var navMeshPathfinder))
			{
				navMeshPathfinder->Stop(f, entity, true);
			}

			if (f.Unsafe.TryGetPointer<BotCharacter>(entity, out var bot))
			{
				bot->Target = EntityRef.None;
			}
			
			if(f.Unsafe.TryGetPointer<AIBlackboardComponent>(entity, out var bbComponent))
			{
				bbComponent->Set(f, Constants.STUN_DURATION_KEY, f.Get<Stats>(entity).CurrentStatusModifierDuration);
			}

			if (f.Unsafe.TryGetPointer<HFSMAgent>(entity, out var agent))
			{
				HFSMManager.TriggerEvent(f, &agent->Data, entity, Constants.STUNNED_EVENT);
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