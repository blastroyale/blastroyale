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
	}
}