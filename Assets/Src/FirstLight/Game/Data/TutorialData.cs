using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace FirstLight.Game.Data
{
	[Flags]
	public enum TutorialSection : ushort
	{
		NONE = 0,
		/// <summary>
		/// Initial FTUE map where player learns movement etc
		/// </summary>
		FTUE_MAP = 1 << 1,
		
		/// <summary>
		/// After ftue map, waiting for player to click "PLAY" on main menu
		/// </summary>
		FIRST_MATCH = 1 << 2,
	}
	
	/// <summary>
	/// Represents the tutorial data state of this player
	/// </summary>
	[Serializable]
	public class TutorialData
	{
		public TutorialSection TutorialSections;

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + TutorialSections.GetHashCode();
			return hash;
		}
	}
}