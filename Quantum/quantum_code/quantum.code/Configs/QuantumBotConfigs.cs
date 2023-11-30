using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class QuantumBotConfig
	{
		public BotBehaviourType BehaviourType;
		public string GameMode;
		public uint Difficulty;
		public FP DecisionInterval;
		public FP LookForTargetsToShootAtInterval;
		public FP VisionRangeSqr;
		public uint AccuracySpreadAngle;
		public FP ChanceToUseSpecial;
		public FP SpecialAimingDeviation;
		public uint LoadoutGearNumber;
		public EquipmentRarity LoadoutRarity;
		public FP MaxAimingRange;
		public FP MovementSpeedMultiplier;
		public FP MaxDistanceToTeammateSquared;
		public FP DamageDoneMultiplier;
		public FP DamageTakenMultiplier;
		public FP MinSpecialCooldown;
		public FP MaxSpecialCooldown;
		public FP MinRunFromZoneTime;
		public FP MaxRunFromZoneTime;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumBotConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumBotConfigs
	{
		public List<QuantumBotConfig> QuantumConfigs = new List<QuantumBotConfig>();
	}
}