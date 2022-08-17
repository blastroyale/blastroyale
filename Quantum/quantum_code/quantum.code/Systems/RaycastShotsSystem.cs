using System.Collections.Generic;
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

		private readonly List<EntityRef> _hitsDone = new List<EntityRef>();

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
			
			_hitsDone.Clear();

			foreach (var linecast in linecastList)
			{
				var hits = f.Physics3D.GetQueryHits(linecast);

				if (hits.Count == 0 || hits[0].Entity == filter.RaycastShots->Attacker ||
				    !filter.RaycastShots->CanHitSameTarget && _hitsDone.Contains(hits[0].Entity))
				{
					continue;
				}
				
				// Systems should be stateless to avoid desyncs. This list is used to avoid garbage every frame.
				// The data is only used on the same context of the update loop and not between loops so is safe
				// from desyncs and state rollbacks
				_hitsDone.Add(hits[0].Entity);
				
				var spell = Spell.CreateInstant(f, hits[0].Entity, filter.RaycastShots->Attacker, filter.RaycastShots->Attacker,
				                                filter.RaycastShots->PowerAmount, filter.RaycastShots->KnockbackAmount, hits[0].Point, filter.RaycastShots->TeamSource);

				if (filter.RaycastShots->SplashRadius > FP._0)
				{
					QuantumHelpers.ProcessAreaHit(f, filter.RaycastShots->SplashRadius, spell);
					f.Events.OnRaycastShotExplosion(filter.RaycastShots->WeaponConfigId, spell.OriginalHitPosition);
				}
				else
				{
					QuantumHelpers.ProcessHit(f, spell);
				}
			
				f.Add<EntityDestroyer>(filter.Entity);
			}
			
			linecastList.Clear();
		}
	}
}