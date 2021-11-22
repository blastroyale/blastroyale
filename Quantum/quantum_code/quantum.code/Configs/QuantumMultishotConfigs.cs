using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumMultishotConfig
	{
		public GameId Id;
		public uint BaseAmount;
		public FP BaseShootTimeGap;
		public uint AmountLevelUpStep;
		public FP ShootTimeGapLevelUpStep;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMultishotConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumMultishotConfigs
	{
		public List<QuantumMultishotConfig> QuantumConfigs = new List<QuantumMultishotConfig>();
	}
}