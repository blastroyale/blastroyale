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

		public List<GameModeEntry> FixedSlots;
		public List<GameModeEntry> RotationSlot1;
		public List<GameModeEntry> RotationSlot2;

		[Serializable]
		public struct GameModeEntry
		{
			public string GameModeId;
			public MatchType MatchType;
			public List<string> Mutators;
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