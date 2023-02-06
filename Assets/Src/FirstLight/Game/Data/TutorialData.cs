using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace FirstLight.Game.Data
{
	[Flags]
	public enum TutorialSection : ushort
	{
		NONE = 0,
		FIRST_GUIDE_MATCH = 1 << 1,
		SECOND_GUIDE_MATCH = 1 << 2,
		BP_EQUIPMENT_GUIDE = 1 << 3
	}
	
	/// <summary>
	/// Represents the liveops state of this player
	/// </summary>
	[Serializable]
	public class TutorialData
	{
		public TutorialSection _tutorialSections;

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + _tutorialSections.GetHashCode();
			return hash;
		}
	}
}