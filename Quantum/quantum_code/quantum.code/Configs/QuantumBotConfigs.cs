using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumBotConfig
	{
		public BotBehaviourType BehaviourType;
		public GameMode GameMode;
		public uint Difficulty;
		public FP DecisionInterval;
		public FP LookForTargetsToShootAtInterval;
		public FP VisionRangeSqr;
		public FP LowArmourSensitivity;
		public FP LowHealthSensitivity;
		public FP LowAmmoSensitivity;
		public FP ChanceToSeekWeapons;
		public FP ChanceToSeekEnemies;
		public FP ChanceToSeekReplenishSpecials;
		public FP ChanceToSeekRage;
		public FP ChanceToAbandonTarget;
		public FP CloseFightIntolerance;
		public FP WanderRadius;
		public uint AccuracySpreadAngle;
		public FP ChanceToUseSpecial;
		public FP SpecialAimingDeviation;
		public FP ShrinkingCircleRiskTolerance;
		public FP ChanceToSeekChests;
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