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
		public struct GameModeEntry
		{
			public string GameModeId;
			public MatchType MatchType;
			public List<string> Mutators;

			public GameModeEntry(string gameModeId, MatchType matchType, List<string> mutators)
			{
				GameModeId = gameModeId;
				MatchType = matchType;
				Mutators = mutators;
			}

			public override string ToString()
			{
				return $"{GameModeId}, {MatchType}, Mutators({string.Join(",", Mutators)})";
			}
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