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
			var linecastList = f.ResolveList(filter.RaycastShot->LinecastQueries);
			var speed = shot->Speed;
			var deltaTime = f.Time - shot->StartTime;
			var previousTime = shot->PreviousTime - shot->StartTime;
			
			// We increase number of shots on 1 to count angleStep for gaps rather than for shots
			var angleStep = shot->AttackAngle / (FP)(shot->NumberOfShots + 1);
			var angle = -(int)shot->AttackAngle / FP._2;
			angle += shot->AccuracyModifier;

			if (shot->IsInstantShot || deltaTime > shot->Range / speed)
			{
				speed = FP._1;
				deltaTime = shot->Range / speed;
				
				f.Add<EntityDestroyer>(filter.Entity);
			}
			
			linecastList.Clear();
			
			for (var i = 0; i < shot->NumberOfShots; i++)
			{
				angle += angleStep;

				var direction = FPVector2.Rotate(shot->Direction, angle * FP.Deg2Rad).XOY * speed;
				var previousPosition = shot->SpawnPosition + direction * previousTime;
				var currentPosition = shot->SpawnPosition + direction * deltaTime;
				var query = f.Physics3D.AddLinecastQuery(previousPosition, currentPosition, true,
				                                         f.Context.TargetAllLayerMask, _hitQuery);

				linecastList.Add(query);
			}
			
			filter.RaycastShot->PreviousTime = f.Time;
		}
	}
}