using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Services.RoomService
{
	public class PlayerProperties : PropertiesHolder
	{
		public QuantumProperty<bool> Spectator;
		public QuantumProperty<List<GameId>> Loadout;
		public QuantumProperty<bool> CoreLoaded;
		public QuantumProperty<string> TeamId;
		public QuantumProperty<Vector2> DropPosition;
		public QuantumProperty<int> Rank;

		public PlayerProperties()
		{
			Spectator = Create<bool>("spectator");
			Loadout = CreateEnumList<GameId>("loadout");
			CoreLoaded = Create<bool>("loaded");
			TeamId = Create<string>("team");
			DropPosition = Create<Vector2>("droppos");
			Rank = Create<int>("rank");
		}
	}
}