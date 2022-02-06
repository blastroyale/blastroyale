using Photon.Deterministic;

namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Spell"/>
	/// </summary>
	public unsafe class SpellSystem : SystemMainThreadFilter<SpellSystem.SpellFilter>, ISignalOnComponentAdded<Spell>
	{
		public struct SpellFilter
		{
			public EntityRef Entity;
			public Spell* Spell;
		}

		/// <inheritdoc />
		public void OnAdded(Frame f, EntityRef entity, Spell* component)
		{
			if (f.TryGet<PlayerCharacter>(component->Attacker, out var attacker))
			{
				f.Events.OnPlayerAttackHit(attacker.Player, component->Attacker, entity, component->OriginalHitPosition);
			}
			
			HandleHealth(f, component->Attacker, entity, component->Attacker, false, (int) component->PowerAmount);
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref SpellFilter filter)
		{
			f.Remove<Spell>(filter.Entity);
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