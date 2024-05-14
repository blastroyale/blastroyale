namespace Quantum
{
	/// <summary>
	/// This class handles behaviour for the <see cref="SpecialSelfStatusModifier"/>
	/// </summary>
	public static unsafe class SpecialSelfStatusModifier
	{
		public static bool Use(Frame f, EntityRef e, in Special special)
		{
			var duration = special.SpecialPower;
			
			switch (special.SpecialType)
			{
				case SpecialType.ShieldSelfStatus:
					StatusModifiers.AddStatusModifierToEntity(f, e, StatusModifierType.Immunity, duration);
					return true;
			}
			
			return false;
		}
	}
}