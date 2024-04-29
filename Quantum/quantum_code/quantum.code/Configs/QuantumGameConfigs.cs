using System;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class QuantumGameConfig
	{
		public FP DisconnectedDestroySeconds;
		public uint DoubleKillTimeLimit;
		public int RoomLockKillCount;
		public int BotsNameCount;
		public FP PlayerRespawnTime;
		public FP PlayerForceRespawnTime;
		public FP GoToNextMatchForceTime;
		public FP ShrinkingDamageCooldown;
		public QuantumGameModePair<int> PlayerDefaultHealth;
		public QuantumGameModePair<FP> PlayerDefaultSpeed;
		public QuantumGameModePair<int> PlayerMaxShieldCapacity;
		public QuantumGameModePair<int> PlayerStartingShieldCapacity;
		public QuantumGameModePair<FP> CollectableCollectTime;
		public QuantumGameModePair<FP> PlayerAliveShieldDuration;
		public uint PlayerMaxEnergyLevel;
		public QuantumPair<ushort, ushort> MinMaxEnergyLevelRequirement;
		public uint MaxPlayerRanks;
		public FP RageStatusDamageMultiplier;
		public FP DeathDropHealthChance;
		public FP DeathDropLargeShieldChance;
		public FP DeathDropSmallShieldChance;
		public int TrophyEloRange;
		public FP TrophyEloK;
		public FP TrophyMinChange;
		public uint NftAssumedOwned;
		public uint MinNftForPoolSizeBonus;
		public FP EarningsAugmentationStrengthDropMod ;
		public FP EarningsAugmentationStrengthSteepnessMod;
		public uint NftUsageCooldownMinutes;
		public uint NftRequiredEquippedForPlay;
		public FP PlayerVisionRange;
		public FP AirdropPositionOffsetMultiplier;
		public FP AirdropRandomAreaMultiplier;
		public FP AirdropHeight;
		public FP MultiKillResetTime;
		public int BotsMaxDifficulty;
		public FP NftDurabilityDropDays;
		public FP NonNftDurabilityDropDays;
		public int BotsDifficultyTrophiesStep;
		public FP MightBaseValue;
		public FP MightRarityMultiplier;
		public FP MightLevelMultiplier;
		public FP TrophiesPerKill;
		public FP RoofDamageHeight;
		public FP RoofDamageDelay;
		public FP RoofDamageAmount;
		public FP RoofDamageCooldown;
		public bool HardAngleAim;
		public FP CollectableEquipmentPickupRadius;
		public FP ChanceToDropNoobOnKill;
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
