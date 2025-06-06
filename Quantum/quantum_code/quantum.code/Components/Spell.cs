using System;
using Photon.Deterministic;

namespace Quantum
{
	public unsafe partial struct Spell
	{
		public const byte DefaultId = 0;
		public const byte ShrinkingCircleId = 1;
		public const byte HeightDamageId = 2;
		public const byte KnockedOut = 3;
		public const byte InstantKill = 3;
		public const byte InstantKillWithoutKnockingOut = 4;

		/// <summary>
		/// Checks if this is a instant hit spell type
		/// </summary>
		public bool IsInstantaneous => EndTime < FP.SmallestNonZero || Cooldown < FP.SmallestNonZero;

		/// <summary>
		/// Creates an instant hit <see cref="Spell"/> based on the given data
		/// </summary>
		public static Spell CreateInstant(Frame f, EntityRef victim, EntityRef attacker, EntityRef spellSource,
										  uint powerAmount, uint knockbackAmount, FPVector2 position)
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
				TeamSource = f.Unsafe.GetPointer<Targetable>(attacker)->Team,
				ShotNumber = 0
			};
		}

		/// <summary>
		/// Sometimes we just simply want to deal damage.
		/// To avoid having to create a new spell everytime we deal damage, we can use SingleHit
		/// function to speed things up a bit.
		/// </summary>
		public unsafe void DoHit(Frame f)
		{
			if (!f.Unsafe.TryGetPointer<Stats>(Victim, out var stats) || PowerAmount == 0)
			{
				return;
			}

			var s = this;
			var finalDmg = PowerAmount;
			
			// If the damage is higher than Shields and HP combined then we reduce it to match
			if (stats->CurrentShield + stats->CurrentHealth < PowerAmount)
			{
				finalDmg = (uint)(stats->CurrentShield + stats->CurrentHealth);
			}

			PowerAmount = finalDmg;

			if (Id == ShrinkingCircleId)
			{
				f.Events.OnShrinkingCircleDmg(Victim, finalDmg);
			}

			stats->ReduceHealth(f, Victim, &s);
		}

		/// <summary>
		/// Creates an instant hit <see cref="Spell"/> based on the given data
		/// </summary>
		public static Spell CreateInstant(Frame f, EntityRef victim, EntityRef attacker, EntityRef spellSource,
										  uint powerAmount, uint knockbackAmount, FPVector2 position, Int32 team)
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
				ShotNumber = 0
			};
		}

		/// <summary>
		/// Creates an instant hit <see cref="Spell"/> based on the given data
		/// </summary>
		public static Spell CreateInstant(Frame f, EntityRef victim, EntityRef attacker, EntityRef spellSource,
										  uint powerAmount, uint knockbackAmount, FPVector2 position, Int32 team, byte shotNumber)
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
				ShotNumber = shotNumber
			};
		}
		
		public bool IsInstantKill()
		{
			return Id == InstantKillWithoutKnockingOut || Id == InstantKill;
		}
	}
}