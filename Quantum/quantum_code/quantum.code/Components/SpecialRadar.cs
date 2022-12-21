namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialRadar"/>
	/// </summary>
	public static class SpecialRadar
	{
		public static bool Use(Frame f, EntityRef e, PlayerRef player, Special special)
		{
			if (!f.Exists(e) || f.Has<DeadPlayerCharacter>(e))
			{
				return false;
			}

			var duration = special.SpecialPower;
			var range = special.SpecialPower;
			f.Events.OnRadarUsed(player, duration, range);

			return true;
		}
	}
}