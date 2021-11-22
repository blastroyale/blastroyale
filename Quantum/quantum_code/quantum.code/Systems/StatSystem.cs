using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour of the <see cref="Modifier"/> active in the entities & all changes to the
	/// entities <see cref="Stats"/>
	/// </summary>
	public unsafe class StatSystem : SystemMainThreadFilter<StatSystem.StatsFilter>, 
	                                 ISignalOnComponentAdded<Stats>, ISignalOnComponentRemoved<Stats>, 
	                                 ISignalProjectileTargetHit, ISignalHazardTargetHit, ISignalSpellHit
	{
		public struct StatsFilter
		{
			public EntityRef Entity;
			public Stats* Stats;
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Stats* component)
		{
			component->Modifiers = f.AllocateList<Modifier>(32);
		}

		/// <inheritdoc />
		public void OnRemoved(Frame f, EntityRef entity, Stats* component)
		{
			f.FreeList(component->Modifiers);

			component->Modifiers = default;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref StatsFilter filter)
		{
			var list = f.ResolveList(filter.Stats->Modifiers);

			for (var i = list.Count - 1; i > -1 ; i--)
			{
				if (f.Time > list[i].EndTime)
				{
					filter.Stats->RemoveModifier(f, list[i]);
					list.RemoveAt(i);
				}
			}

			if (filter.Stats->CurrentStatusModifierType == StatusModifierType.None)
			{
				return;
			}
			
			if (f.Time > filter.Stats->CurrentStatusModifierEndTime)
			{
				StatusModifiers.FinishCurrentStatusModifier(f, filter.Entity);
			}
		}
		
		/// <inheritdoc />
		public void ProjectileTargetHit(Frame f, ProjectileHitData* data)
		{
			var projectileData = f.Get<Projectile>(data->Projectile).Data;
			var power = (int) projectileData.PowerAmount;
			
			// If it's a direct hit with explosive projectile then we do an increased damage
			// proportionally to the splash explosion diameter
			if (projectileData.SplashRadius > 0)
			{
				power *= (int) (projectileData.SplashRadius + projectileData.SplashRadius);
			}
			
			HandleHealth(f, projectileData.Attacker, data->TargetHit, data->Projectile, projectileData.IsHealing, power);
		}
		
		/// <inheritdoc />
		public void HazardTargetHit(Frame f, HazardHitData* data)
		{
			var hazard = f.Get<Hazard>(data->Hazard);
			
			HandleHealth(f, hazard.Attacker, data->TargetHit, data->Hazard,hazard.IsHealing, (int) hazard.PowerAmount);
		}

		/// <inheritdoc />
		public void SpellHit(Frame f, EntityRef entity, Spell* spell)
		{
			HandleHealth(f, spell->Attacker, entity, spell->Attacker, spell->IsHealing, (int) spell->PowerAmount);
		}
		
		private void HandleHealth(Frame f, EntityRef attacker, EntityRef targetHit, EntityRef hitSource, bool isHealing, int powerAmount)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(targetHit, out var stats) || powerAmount == 0)
			{
				return;
			}
			
			var armour = f.Get<Stats>(targetHit).Values[(int) StatType.Armour].StatValue;
			var damage = FPMath.Max(powerAmount - armour, 0).AsInt;
			
			if (isHealing)
			{
				stats->GainHealth(f, targetHit, attacker, powerAmount);
			}
			else if(damage > 0)
			{
				stats->ReduceHealth(f, targetHit, attacker, hitSource, damage);
			}
		}
	}
}