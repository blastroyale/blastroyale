using System;
using System.Collections.Generic;

namespace FirstLight.Game.Ids
{
	public enum GuidId
	{
		Main,
		UI_Ftue_Controls_Hud,
		UI_Left_Joystick,
		UI_Left_Handle_Joystick,
		UI_Right_Joystick,
		UI_Right_Handle_Joystick,
		UI_Special_0,
		UI_Special_1,
		PlayerCharacter
	}
	
	/// <summary>
	/// Avoids boxing for Dictionary
	/// </summary>
	public class GuidIdComparer : IEqualityComparer<GuidId>
	{
		public bool Equals(GuidId x, GuidId y)
		{
			return x == y;
		}

		public int GetHashCode(GuidId obj)
		{
			return (int)obj;
		}
	}
}