using System;
using System.Collections.Generic;

namespace FirstLight.Game.Data
{

	/// <summary>
	/// Represents the liveops state of this player
	/// </summary>
	[Serializable]
	public class LiveopsData
	{
		public List<int> TriggeredActions = new List<int>();
		
		public override int GetHashCode()
		{
			int hash = 17;
			foreach (var action in TriggeredActions)
			{
				hash = hash * 23 + action.GetHashCode();
			}
			return hash;
		}
	}
}