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
		public FP CasualMatchmakingTime;
		public FP RankedMatchmakingTime;
		public int RankedMatchmakingMinPlayers;
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
		public uint MaxPlayerRanks;
		public FP RageStatusDamageMultiplier;
		public FP DeathDropHealthChance;
		public FP DeathDropLargeShieldChance;
		public FP DeathDropSmallShieldChance;
		public int TrophyEloRange;
		public int TrophyEloK;
		public uint NftAssumedOwned;
		public uint MinNftForEarnings;
		public FP AdjectiveRarityEarningsMod;
		public uint NftUsageCooldownMinutes;
		public uint NftRequiredEquippedForPlay;
		public FP PlayerVisionRange;
		public FP AirdropPositionOffsetMultiplier;
		public FP AirdropRandomAreaMultiplier;
		public FP AirdropHeight;
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
