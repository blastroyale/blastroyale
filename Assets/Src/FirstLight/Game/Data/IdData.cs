using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Quantum;

namespace FirstLight.Game.Data
{
	/// <summary>
	/// Contains all the data that scopes possible Id mapping in the game
	/// </summary>
	[Serializable]
	public class IdData
	{
		public UniqueId UniqueIdCounter;
		public Dictionary<UniqueId, GameId> GameIds = new Dictionary<UniqueId, GameId>();

		public override int GetHashCode()
		{
			int hash = 17;
			hash = hash * 23 + UniqueIdCounter.GetHashCode();
			foreach (var e in GameIds)
				hash = hash * 23 + e.Key.GetHashCode() + e.Value.GetHashCode();
			return hash;
		}
	}
}