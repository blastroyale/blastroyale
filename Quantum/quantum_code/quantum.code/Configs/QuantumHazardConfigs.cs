using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumHazardConfig
	{
		public GameId Id;
		public FP Radius;
		public FP ActivationDelay;
		public FP Lifetime;
		public FP Interval;
		public uint Damage;
		public bool IsHealing;
		public FP AimHelpingRadius;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumHazardConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumHazardConfigs
	{
		public List<QuantumHazardConfig> QuantumConfigs = new List<QuantumHazardConfig>();
	}
}