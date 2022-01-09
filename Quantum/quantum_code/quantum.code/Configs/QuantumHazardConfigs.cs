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
		public FP InitialDelay;
		public FP Lifetime;
		public FP Interval;
		public uint PowerAmount;
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