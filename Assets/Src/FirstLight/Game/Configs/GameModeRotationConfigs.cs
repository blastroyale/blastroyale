using System;
using System.Collections.Generic;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct GameModeRotationConfig
	{
		public long StartTimeTicks;
		public uint SlotDuration;

		public List<RotationEntry> Entries;

		[Serializable]
		public struct RotationEntry
		{
			public string GameModeId;
			// TODO: Modifiers
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