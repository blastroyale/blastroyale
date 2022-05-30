using System;
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
		public FP ShrinkingDamageCooldown;
		public uint ShrinkingDamage;
		public QuantumGameModePair<int> PlayerDefaultHealth;
		public QuantumGameModePair<FP> PlayerDefaultSpeed;
		public QuantumGameModePair<int> PlayerMaxShieldCapacity;
		public QuantumGameModePair<int> PlayerStartingShieldCapacity;
		public int CoinsPerRank;
		public int XpPerRank;
		public FP DeathSignificance;
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
		public int StatsPowerBaseValue;
		public FP StatsPowerRarityMultiplier;
		public FP StatsPowerLevelStepMultiplier;
		public FP StatsPowerGradeStepMultiplier;
		public int StatsHpBaseValue;
		public FP StatsHpRarityMultiplier;
		public FP StatsHpLevelStepMultiplier;
		public FP StatsHpGradeStepMultiplier;
		public FP StatsSpeedBaseValue;
		public FP StatsSpeedRarityMultiplier;
		public FP StatsSpeedLevelStepMultiplier;
		public FP StatsSpeedGradeStepMultiplier;
		public int StatsArmorBaseValue;
		public FP StatsArmorRarityMultiplier;
		public FP StatsArmorLevelStepMultiplier;
		public FP StatsArmorGradeStepMultiplier;
		public FP DeathDropHealthChance;
		public FP DeathDropLargeShieldChance;
		public FP DeathDropSmallShieldChance;
		public int TrophyEloRange;
		public int TrophyEloK;
		public int MinOffhandWeaponPoolSize;
		public uint NftAssumedOwned;
		public uint MinNftForEarnings;
		public FP AdjectiveRarityEarningsMod;
		public uint LoadoutSlots;
		public uint NftUsageCooldownMinutes;
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