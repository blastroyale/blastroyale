using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="RaycastShots"/> pre physics processing
	/// </summary>
	public unsafe class PreRaycastShotsSystem : SystemMainThreadFilter<PreRaycastShotsSystem.RaycastShotsFilter>
	{ 
		private const QueryOptions _hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
		
		public struct RaycastShotsFilter
		{
			public EntityRef Entity;
			public RaycastShots* RaycastShot;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref RaycastShotsFilter filter)
		{
			var shot = filter.RaycastShot;
			var linecastList = f.ResolveList(shot->LinecastQueries);
			var speed = shot->Speed;
			var deltaTime = f.Time - shot->StartTime;
			var previousTime = shot->PreviousTime - shot->StartTime;

			var angleCount = shot->NumberOfShots + 1;
			var angleStep = shot->AttackAngle / (FP)angleCount;
			var angle = -(int) shot->AttackAngle / FP._2;

			if (shot->IsInstantShot || deltaTime > shot->Range / speed)
			{
				speed = FP._1;
				deltaTime = shot->Range / speed;
				
				f.Add<EntityDestroyer>(filter.Entity);
			}
			
			for (var i = 0; i < angleCount-1; i++)
			{
				angle += angleStep;

				var direction = FPVector2.Rotate(shot->Direction, angle * FP.Deg2Rad).XOY * speed;
				var previousPosition = shot->SpawnPosition + direction * previousTime;
				var currentPosition = shot->SpawnPosition + direction * deltaTime;
				var query = f.Physics3D.AddLinecastQuery(previousPosition, currentPosition, true,
				                                         f.TargetAllLayerMask, _hitQuery);

				linecastList.Add(query);
			}
			
			filter.RaycastShot->PreviousTime = f.Time;
		}
	}
}