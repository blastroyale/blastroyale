using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// TODO: DELETE THE HAZARD SYSTEM
	/// </summary>
	public unsafe class HazardSystem : SystemMainThreadFilter<HazardSystem.HazardFilter>
	{
		public struct HazardFilter
		{
			public EntityRef Entity;
			public Hazard* Hazard;
			public Transform3D* Transform;
		}
		
		/// <inheritdoc />
		public override void Update(Frame f, ref HazardFilter filter)
		{
			var hazard = filter.Hazard;
			
			if (f.Time > hazard->EndTime)
			{
				f.Add<EntityDestroyer>(filter.Entity);
			}
			
			if (f.Time < hazard->NextTickTime)
			{
				return;
			}

			var spell = new Spell
			{
				Id = Spell.DefaultId,
				Attacker = hazard->Attacker,
				Cooldown = FP._0,
				EndTime = FP._0,
				NextHitTime = FP._0,
				OriginalHitPosition = filter.Transform->Position,
				PowerAmount = hazard->PowerAmount,
				SpellSource = filter.Entity,
				TeamSource = hazard->TeamSource,
				Victim = default,
				KnockbackAmount = hazard->Knockback,
				PercentHealthDamage = hazard->PercentHealthDamage
			};
			
			hazard->NextTickTime += hazard->NextTickTime == FP._0 ? f.Time + hazard->Interval : hazard->Interval;
			
			QuantumHelpers.ProcessAreaHit(f, hazard->Radius, spell, hazard->MaxHitCount, OnHit);
		}

		private void OnHit(Frame f, Spell spell)
		{
			var source = f.Get<Hazard>(spell.SpellSource);
			
			if (source.StunDuration > FP._0)
			{
				StatusModifiers.AddStatusModifierToEntity(f, spell.Victim, StatusModifierType.Stun, source.StunDuration);
			}
			
			f.Events.OnHazardHit(spell.Attacker, spell.Victim, source, spell.OriginalHitPosition);
		}
	}
}