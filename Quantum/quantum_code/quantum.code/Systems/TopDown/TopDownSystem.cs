using Photon.Deterministic;
using Quantum.Systems;

namespace Quantum
{
	public unsafe struct TopDownSystemFilter
	{
		public EntityRef Entity;
		public PlayerCharacter* Player;
		public AlivePlayerCharacter* Alive;
		public TopDownController* Controller;
	}
	
	public unsafe class TopDownSystem : SystemMainThreadFilter<TopDownSystemFilter>, ISignalOnNavMeshMoveAgent, ISignalOnNavMeshWaypointReached
	{
		public override void Update(Frame f, ref TopDownSystemFilter filter)
		{
			filter.Controller->Move(f, filter.Entity, filter.Controller->MoveDirection);
		}

		public void OnNavMeshMoveAgent(Frame f, EntityRef entity, FPVector2 desiredDirection)
		{
			if (f.Unsafe.TryGetPointer<TopDownController>(entity, out var kcc))
			{
				kcc->MoveDirection = desiredDirection.Normalized;
			}
		}

		public void OnNavMeshWaypointReached(Frame f, EntityRef entity, FPVector3 waypoint, Navigation.WaypointFlag waypointFlags,
											 ref bool resetAgent)
		{
			if (f.Unsafe.TryGetPointer<TopDownController>(entity, out var kcc))
			{
				kcc->MoveDirection = FPVector2.Zero;
			} 
		}
	}
}