using System;
using System.Collections.Generic;
using Backend.Data.DataTypes;

namespace Backend.Data
{
	/// <summary>
	/// Contains all the data that scopes possible Id mapping in the game
	/// </summary>
	[Serializable]
	public class IdData
	{
		public uint UniqueIdCounter;
		public readonly Dictionary<UniqueId, string> GameIds = new Dictionary<UniqueId, string>();
	}
}