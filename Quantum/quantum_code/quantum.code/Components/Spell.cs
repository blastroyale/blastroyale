using System;
using Photon.Deterministic;

namespace Quantum
{
	public partial struct Spell
	{
		public const uint DefaultId = 0;
		public const uint ShrinkingCircleId = 1;
		
		/// <summary>
		/// Checks if this is a instant hit spell type
		/// </summary>
		public bool IsInstantaneous => EndTime < FP.SmallestNonZero || Cooldown < FP.SmallestNonZero;
		
		/// <summary>
		/// Creates an instant hit <see cref="Spell"/> based on the given data
		/// </summary>
		public static Spell CreateInstant(Frame f, EntityRef victim, EntityRef attacker, EntityRef spellSource, 
		                                  uint powerAmount, uint knockbackAmount, FPVector3 position, bool percentHealthDamage = false)
		{
			return new Spell
			{
				Id = DefaultId,
				Victim = victim,
				Attacker = attacker,
				SpellSource = spellSource,
				Cooldown = FP._0,
				EndTime = FP._0,
				NextHitTime = FP._0,
				OriginalHitPosition = position,
				PowerAmount = powerAmount,
				KnockbackAmount = knockbackAmount,
				TeamSource = f.Get<Targetable>(attacker).Team,
				PercentHealthDamage = percentHealthDamage
			};
		}
		
		/// <summary>
		/// Creates an instant hit <see cref="Spell"/> based on the given data
		/// </summary>
		public static Spell CreateInstant(Frame f, EntityRef victim, EntityRef attacker, EntityRef spellSource, 
		                               uint powerAmount, uint knockbackAmount, FPVector3 position, Int32 team, bool percentHealthDamage = false)
		{
			return new Spell
			{
				Id = DefaultId,
				Victim = victim,
				Attacker = attacker,
				SpellSource = spellSource,
				Cooldown = FP._0,
				EndTime = FP._0,
				NextHitTime = FP._0,
				OriginalHitPosition = position,
				PowerAmount = powerAmount,
				KnockbackAmount = knockbackAmount,
				TeamSource = team,
				PercentHealthDamage = percentHealthDamage
			};
		}
	}
}