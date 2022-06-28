namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialSelfStatusModifier"/>
	/// </summary>
	public static class SpecialSelfStatusModifier
	{
		public static bool Use(Frame f, EntityRef e, Special special)
		{
			var duration = special.SpecialPower;
			
			switch (special.SpecialType)
			{
				case SpecialType.ShieldSelfStatus:
					StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Shield, duration);
					return true;
			}
			
			return false;
		}
	}
}