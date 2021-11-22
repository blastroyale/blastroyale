namespace Quantum.Systems
{
	/// <summary>
	/// This system handles all the behaviour for the <see cref="Spell"/>
	/// </summary>
	public unsafe class SpellSystem : SystemMainThreadFilter<SpellSystem.SpellFilter>, ISignalPlayerAttackHit
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
		public void PlayerAttackHit(Frame f, PlayerRef player, EntityRef playerEntity, EntityRef hitEntity)
		{
			var spell = new Spell
			{
				IsHealing = false,
				PowerAmount = (uint) f.Get<Stats>(playerEntity).GetStatData(StatType.Power).StatValue.AsInt,
				Attacker = playerEntity
			};

			f.Add(hitEntity, spell);
		}
	}
}