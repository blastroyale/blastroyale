using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumShrinkingCircleConfig
	{
		public int Step;

		public FP DelayTime;
		public FP WarningTime;
		public FP ShrinkingTime;
		public FP ShrinkingSizeK;

		public FP AirdropChance;
		public QuantumPair<FP, FP> AirdropStartTimeRange;
		public FP AirdropDropDuration;
		public GameId AirdropChest;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumShrinkingCircleConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumShrinkingCircleConfigs
	{
		public List<QuantumShrinkingCircleConfig> QuantumConfigs = new List<QuantumShrinkingCircleConfig>();
	}
}