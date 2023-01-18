using System.Collections.Generic;
using JetBrains.Annotations;
using Quantum;

namespace FirstLight.Game.Data.DataTypes
{
	/// <summary>
	/// Generic representation of an item in-game
	/// </summary>
	public struct ItemData
	{
		public GameId Id;
		public int Amount;
		[CanBeNull] public object ItemObject;
	}
}