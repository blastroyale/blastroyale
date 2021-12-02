using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumGameConfig
	{
		public FP DisconnectedDestroySeconds;
		public uint DoubleKillTimeLimit;
		public int RoomLockKillCount;
		public FP MatchmakingTime;
		public int BotsNameCount;
		public FP PlayerRespawnTime;
		public FP PlayerForceRespawnTime;
		public FP GoToNextMatchForceTime;
		public FP ShrinkingSizeK;
		public FP ShrinkingBorderK;
		public int PlayerDefaultHealth;
		public FP PlayerDefaultSpeed;
		public int PlayerDefaultInterimArmour;
		public int CoinsPerRank;
		public int XpPerRank;
		public FP DeathSignificance ;
		public FP CoinsPerFragDeathRatio;
		public FP XpPerFragDeathRatio;
		public FP CollectableCollectTime;
		public FP PlayerAliveShieldDuration;
		public uint EquipmentLevelToPowerK;
		public uint EquipmentRarityToPowerK;
		public uint LootboxSlotsMaxNumber;
		public FP MinuteCostInHardCurrency;
		public uint MaxPlayerRanks;
		public FP RageStatusDamageMultiplier;
		public uint FusionBaseWeightPerType;
		public uint FusionWeightIncreasePerItem;
		public uint FusionWeightIncreasePerLevel;
		public int StatsPowerBaseValue;
		public FP StatsPowerRarityMultiplier;
		public FP StatsPowerLevelStepMultiplier;
		public int StatsHpBaseValue;
		public FP StatsHpRarityMultiplier;
		public FP StatsHpLevelStepMultiplier;
		public FP StatsSpeedBaseValue;
		public FP StatsSpeedRarityMultiplier;
		public FP StatsSpeedLevelStepMultiplier;
		public int StatsArmorBaseValue;
		public FP StatsArmorRarityMultiplier;
		public FP StatsArmorLevelStepMultiplier;
		public List<GameId> CratesCycle;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumGameConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumGameConfigs
	{
		public QuantumGameConfig QuantumConfig;
	}
}