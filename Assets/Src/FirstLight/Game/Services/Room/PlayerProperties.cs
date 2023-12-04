using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.RoomService
{
	public class PlayerProperties : PropertiesHolder
	{
		public readonly QuantumProperty<bool> Spectator;
		public readonly QuantumProperty<List<GameId>> Loadout;
		public readonly QuantumProperty<bool> CoreLoaded;
		public readonly QuantumProperty<string> TeamId;
		public readonly QuantumProperty<Vector2> DropPosition;
		public readonly QuantumProperty<int> Rank;
		public readonly QuantumProperty<byte> ColorIndex;

		public PlayerProperties()
		{
			Spectator = Create<bool>("spectator");
			Loadout = CreateEnumList<GameId>("loadout");
			CoreLoaded = Create<bool>("loaded");
			TeamId = Create<string>("team");
			DropPosition = Create<Vector2>("droppos");
			Rank = Create<int>("rank");
			ColorIndex = Create<byte>("color");
		}
	}
}