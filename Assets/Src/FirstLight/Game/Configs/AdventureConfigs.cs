using System;
using System.Collections.Generic;
using FirstLight.Game.Infos;
using Quantum;
using UnityEngine;

namespace FirstLight.Game.Configs
{
	[Serializable]
	public struct AdventureConfig
	{
		public int Id;
		public int Chapter;
		public int Stage;
		public GameId Map;
		public AdventureDifficultyLevel Difficulty;
		public int UnlockedAdventureRequirement;
		public int RecommendedPower;
		public List<Pair<GameId, uint>> FirstClearReward;
		public int WaveId;
		public int EnemiesDifficulty;
		public int BossDifficulty;
		public int CycleCratesTier;
		public int PlayersLimit;
		public int DestructiblesAmount;
		public int MaxPillsDropPerEnemyWave;
		public int TotalFightersLimit;
		public int DeathmatchKillCount;
	}

	/// <summary>
	/// Scriptable Object tool to import the <seealso cref="AdventureConfigs"/> sheet data
	/// </summary>
	[CreateAssetMenu(fileName = "AdventureConfigs", menuName = "ScriptableObjects/Configs/AdventureConfigs")]
	public class AdventureConfigs : ScriptableObject, IConfigsContainer<AdventureConfig>
	{
		[SerializeField] private List<AdventureConfig> _configs = new List<AdventureConfig>();

		// ReSharper disable once ConvertToAutoProperty
		/// <inheritdoc />
		public List<AdventureConfig> Configs
		{
			get => _configs;
			set => _configs = value;
		}
	}
}