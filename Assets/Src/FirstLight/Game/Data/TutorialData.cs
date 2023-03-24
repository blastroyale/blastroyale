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
		META_GUIDE_AND_MATCH = 1 << 2,
		POST_MATCH_GUIDE = 1 << 3,
		TUTORIAL_BP = 1 << 4
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