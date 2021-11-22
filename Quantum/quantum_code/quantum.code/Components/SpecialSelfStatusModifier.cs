namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialSelfStatusModifier"/>
	/// </summary>
	public static class SpecialSelfStatusModifier
	{
		public static bool Use(Frame f, EntityRef e, Special special)
		{
			var duration = special.PowerAmount;
			
			switch (special.SpecialType)
			{
				case SpecialType.RageSelfStatus:
					StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Rage, duration);
					return true;
				case SpecialType.InvisibilitySelfStatus:
					StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Invisibility, duration);
					return true;
				case SpecialType.ShieldSelfStatus:
					StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Shield, duration);
					return true;
			}
			
			return false;
		}
	}
}