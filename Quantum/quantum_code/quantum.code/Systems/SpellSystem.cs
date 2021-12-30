namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Spell"/>
	/// </summary>
	public unsafe class SpellSystem : SystemMainThreadFilter<SpellSystem.SpellFilter>, ISignalAttackHit
	{
		public struct SpellFilter
		{
			public EntityRef Entity;
			public Spell* Spell;
		}

		/// <inheritdoc />
		public override void Update(Frame f, ref SpellFilter filter)
		{
			f.Signals.SpellHit(filter.Entity, filter.Spell);
			f.Remove<Spell>(filter.Entity);
		}

		/// <inheritdoc />
		public void AttackHit(Frame f, EntityRef playerEntity, EntityRef hitEntity, int amount)
		{
			var spell = new Spell
			{
				IsHealing = false,
				PowerAmount = (uint) amount,
				Attacker = playerEntity
			};
			
			// TODO: do damage

			f.Add(hitEntity, spell);
		}
	}
}