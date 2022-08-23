using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Spell"/>
	/// </summary>
	public unsafe class SpellSystem : SystemMainThreadFilter<SpellSystem.SpellFilter>, ISignalOnComponentAdded<Spell>, ISignalOnComponentRemoved<Spell>
	{
		public struct SpellFilter
		{
			public EntityRef Entity;
			public Spell* Spell;
		}

		/// <inheritdoc />
		public override void OnDisabled(Frame f)
		{
			foreach (var spell in f.GetComponentIterator<Spell>())
			{
				f.Destroy(spell.Entity);
			}
			foreach (var stat in f.GetComponentIterator<Stats>())
			{
				f.ResolveList(stat.Component.SpellEffects).Clear();
			}
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref SpellFilter filter)
		{
			if (f.Time > filter.Spell->EndTime)
			{
				f.Remove<Spell>(filter.Entity);
			}
			
			if (f.Time < filter.Spell->NextHitTime)
			{
				return;
			}

			filter.Spell->NextHitTime += filter.Spell->NextHitTime < FP.SmallestNonZero
				                             ? f.Time + filter.Spell->Cooldown
				                             : filter.Spell->Cooldown;
			
			if (f.TryGet<PlayerCharacter>(filter.Spell->Attacker, out var attacker))
			{
				f.Events.OnPlayerAttackHit(attacker.Player, filter.Spell->Attacker, filter.Spell->Victim, 
				                           filter.Spell->OriginalHitPosition);
			}
			
			HandleHealth(f, *filter.Spell, false);
		}
		
		private void HandleHealth(Frame f, Spell spell, bool isHealing)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(spell.Victim, out var stats) || spell.PowerAmount == 0 ||
			    f.Has<EntityDestroyer>(spell.Victim))
			{
				return;
			}
			
			if (isHealing)
			{
				stats->GainHealth(f, spell.Victim, spell);
			}
			else
			{
				stats->ReduceHealth(f, spell.Victim, spell);
			}
		}

		public void OnAdded(Frame f, EntityRef entity, Spell* component)
		{
			f.Events.OnSpellAdded(entity, *component);
		}

		public void OnRemoved(Frame f, EntityRef entity, Spell* component)
		{
			f.Events.OnSpellRemoved(entity, *component);
		}
	}
}