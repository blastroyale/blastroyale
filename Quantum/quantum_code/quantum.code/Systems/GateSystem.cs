using System.Collections.Generic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the changes in a <see cref="Destructible"/> entity
	/// </summary>
	public unsafe class GateSystem : SystemMainThreadFilter<GateSystem.GateFilter>,
									 ISignalPlayerDead
	{
		public struct GateFilter
		{
			public EntityRef Entity;
			public Gate* Gate;
			public PhysicsCollider3D* Collider;
		}

		public void PlayerDead(Frame f, PlayerRef playerDead, EntityRef entityDead)
		{
			var playersAlive = AlivePlayerCount(f);
			foreach (var pair in f.Unsafe.GetComponentBlockIterator<Gate>())
			{
				var gate = pair.Component;
				if (playersAlive <= gate->PlayersAlive)
				{
					f.Events.OnGateStartOpening(pair.Entity, gate->OpeningTime);
					
					gate->TimeToOpen = f.Time + gate->OpeningTime;
					gate->IsOpening = true;
				}
			}
		}

		private int AlivePlayerCount(Frame f)
		{
			return f.ComponentCount<AlivePlayerCharacter>();
		}

		public override void Update(Frame f, ref GateFilter filter)
		{
			if (!filter.Gate->IsOpening || f.Time < filter.Gate->TimeToOpen)
			{
				return;
			}
			
			filter.Collider->IsTrigger = true;
		}
	}
}