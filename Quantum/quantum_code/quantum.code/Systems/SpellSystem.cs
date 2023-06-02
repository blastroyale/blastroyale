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
			foreach (var spell in f.Unsafe.GetComponentBlockIterator<Spell>())
			{
				f.Destroy(spell.Entity);
			}
			foreach (var stat in f.Unsafe.GetComponentBlockIterator<Stats>())
			{
				f.ResolveList(stat.Component->SpellEffects).Clear();
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
			
			filter.Spell->DoHit(f);
		}

		public void OnAdded(Frame f, EntityRef entity, Spell* component)
		{
		}

		public void OnRemoved(Frame f, EntityRef entity, Spell* component)
		{
		}
	}
}