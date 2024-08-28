namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the changes in a <see cref="Destructible"/> entity
	/// </summary>
	public unsafe class GateSystem : SystemMainThreadFilter<GateSystem.GateFilter>,
									 ISignalTriggerActivated
	{
		public struct GateFilter
		{
			public EntityRef Entity;
			public Gate* Gate;
			public PhysicsCollider2D* Collider;
		}
		
		public override void Update(Frame f, ref GateFilter filter)
		{
			if (!filter.Gate->IsOpening || f.Time < filter.Gate->TimeToOpen)
			{
				return;
			}
			
			filter.Collider->IsTrigger = true;
		}

		public void TriggerActivated(Frame f, EntityRef target, TriggerData triggerData)
		{
			foreach (var pair in f.Unsafe.GetComponentBlockIterator<Gate>())
			{
				var gate = pair.Component;
				
				if (pair.Entity == target)
				{
					f.Events.OnGateStartOpening(pair.Entity, gate->OpeningTime);
					
					gate->TimeToOpen = f.Time + gate->OpeningTime;
					gate->IsOpening = true;
				}
			}
		}
	}
}