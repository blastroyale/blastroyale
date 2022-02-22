using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Spell"/>
	/// </summary>
	public unsafe class SpellSystem : SystemMainThreadFilter<SpellSystem.SpellFilter>
	{
		public struct SpellFilter
		{
			public EntityRef Entity;
			public Spell* Spell;
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
				f.Events.OnPlayerAttackHit(attacker.Player, filter.Spell->Attacker, filter.Entity, 
				                           filter.Spell->OriginalHitPosition);
			}
			
			HandleHealth(f, filter.Spell->Attacker, filter.Entity, filter.Spell->Attacker, 
			             false, (int) filter.Spell->PowerAmount);
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
				stats->ReduceHealth(f, targetHit, attacker, damage);
			}
		}
	}
}