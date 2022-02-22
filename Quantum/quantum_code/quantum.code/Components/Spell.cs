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
		                               uint powerAmount, FPVector3 position)
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
				TeamSource = f.Get<Targetable>(attacker).Team
			};
		}
	}
}