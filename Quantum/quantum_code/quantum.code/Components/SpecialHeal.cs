namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialHeal"/>
	/// </summary>
	public static unsafe class SpecialHeal
	{
		public static bool Use(Frame f, EntityRef e, PlayerRef player, in Special special)
		{
			if (!f.Exists(e) || f.Has<DeadPlayerCharacter>(e)
				|| !f.Unsafe.TryGetPointer<Stats>(e, out var stats))
			{
				return false;
			}
			
			var spell = new Spell {PowerAmount = (uint) special.SpecialPower};
			stats->GainHealth(f, e, &spell);
			
			return true;
		}
	}
}