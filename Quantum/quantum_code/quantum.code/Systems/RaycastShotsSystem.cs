using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="RaycastShots"/> after physics hit processing
	/// </summary>
	public unsafe class RaycastShotsSystem : SystemMainThreadFilter<RaycastShotsSystem.RaycastShotFilter>, 
	                                        ISignalOnComponentAdded<RaycastShots>, ISignalOnComponentRemoved<RaycastShots>
	{ 
		private const QueryOptions _hitQuery = QueryOptions.HitDynamics | QueryOptions.HitKinematics | QueryOptions.HitStatics;
		
		public struct RaycastShotFilter
		{
			public EntityRef Entity;
			public RaycastShots* RaycastShots;
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, RaycastShots* component)
		{
			component->LinecastQueries = f.AllocateList<int>();
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, RaycastShots* component)
		{
			f.FreeList(component->LinecastQueries);

			component->LinecastQueries = default;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref RaycastShotFilter filter)
		{
			var linecastList = f.ResolveList(filter.RaycastShots->LinecastQueries);

			foreach (var linecast in linecastList)
			{
				var hits = f.Physics3D.GetQueryHits(linecast);

				if (hits.Count == 0 || hits[0].Entity == filter.RaycastShots->Attacker)
				{
					continue;
				}
				
				var spell = Spell.CreateInstant(f, hits[0].Entity, filter.RaycastShots->Attacker, filter.RaycastShots->Attacker,
				                                filter.RaycastShots->PowerAmount, hits[0].Point, filter.RaycastShots->TeamSource);

				if (filter.RaycastShots->SplashRadius > FP._0)
				{
					QuantumHelpers.ProcessAreaHit(f, filter.RaycastShots->SplashRadius, spell);
				}
				else
				{
					QuantumHelpers.ProcessHit(f, spell);
				}
			
				f.Add<EntityDestroyer>(filter.Entity);

				if (!filter.RaycastShots->CanHitSameTarget)
				{
					break;
				}
			}
			
		}
	}
}