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
		public string GameModeId1;
		public MatchType MatchType1;
		public string GameModeId2;
		public MatchType MatchType2;
		
		public long RotationStartTimeTicks;
		public uint RotationSlotDuration;

		public List<RotationEntry> RotationEntries;

		[Serializable]
		public struct RotationEntry
		{
			public string GameModeId;
			public List<string> MutatorIds;
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