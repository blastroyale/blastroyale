using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumFrontshotConfig
	{
		public GameId Id;
		public uint BaseAmount;
		public FP Spread;
		public uint AmountLevelUpStep;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumFrontshotConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumFrontshotConfigs
	{
		public List<QuantumFrontshotConfig> QuantumConfigs = new List<QuantumFrontshotConfig>();
	}
}