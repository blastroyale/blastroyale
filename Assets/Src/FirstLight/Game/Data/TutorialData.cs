using System;
using System.Collections.Generic;

namespace FirstLight.Game.Data
{
	[Flags]
	public enum TutorialStep : ushort
	{
		NONE = 0,
		PLAYED_MATCH = 1 << 1,
		PLAYED_SECOND_MATCH = 1 << 2,
		TUTORIAL_BP = 1 << 3,
	}
	
	/// <summary>
	/// Represents the liveops state of this player
	/// </summary>
	[Serializable]
	public class TutorialData
	{
		public TutorialStep TutorialSteps;

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + TutorialSteps.GetHashCode();
			return hash;
		}
	}
}