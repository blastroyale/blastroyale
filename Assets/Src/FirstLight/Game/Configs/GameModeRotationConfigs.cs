using System;
using System.Collections.Generic;
using FirstLight.Game.Ids;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct GameModeRotationConfig
	{
		public long RotationStartTimeTicks;
		public uint RotationSlotDuration;

		public List<SlotWrapper> Slots;

		[Serializable]
		public struct GameModeEntry : IEquatable<GameModeEntry>
		{
			public string GameModeId;
			public MatchType MatchType;
			public List<string> Mutators;
			public bool Squads;

			public GameModeEntry(string gameModeId, MatchType matchType, List<string> mutators, bool isSquads)
			{
				GameModeId = gameModeId;
				MatchType = matchType;
				Mutators = mutators;
				Squads = isSquads;
			}

			public override string ToString()
			{
				return $"{GameModeId}, {MatchType}{(Squads ? ", Squads" : "")}, Mutators({string.Join(",", Mutators)})";
			}

			public bool Equals(GameModeEntry other)
			{
				return GameModeId == other.GameModeId && MatchType == other.MatchType && Equals(Mutators, other.Mutators) && Squads == other.Squads;
			}

			public override bool Equals(object obj)
			{
				return obj is GameModeEntry other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(GameModeId, (int) MatchType, Mutators, Squads);
			}
			
			public static bool operator !=(GameModeEntry obj1, GameModeEntry obj2) => !(obj1.Equals(obj2));
			public static bool operator ==(GameModeEntry obj1, GameModeEntry obj2) => obj1.Equals(obj2);
		}

		[Serializable]
		public struct SlotWrapper
		{
			public List<GameModeEntry> Entries;

			public int Count => Entries.Count;

			public GameModeEntry this[int key]
			{
				get => Entries[key];
				set => Entries[key] = value;
			}
		}
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="QuantumGameModeConfig"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "GameModeRotationConfigs",
	                 menuName = "ScriptableObjects/Configs/GameModeRotationConfigs")]
	public class GameModeRotationConfigs : ScriptableObject, ISingleConfigContainer<GameModeRotationConfig>
	{
		[SerializeField] private GameModeRotationConfig _config;

		public GameModeRotationConfig Config
		{
			get => _config;
			set => _config = value;
		}
	}
}