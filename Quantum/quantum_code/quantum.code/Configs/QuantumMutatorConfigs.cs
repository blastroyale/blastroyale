using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public enum MutatorType{
		SpecialsCooldowns,
		Speed,
		HealthPerSeconds
	}

	public enum MutatorId
	{
		QuickSpecials,
		SpeedUp,
		HealthyAir
	}
	
	[Serializable]
	public partial struct QuantumMutatorConfig
	{
		public MutatorId Id;
		public MutatorType Type;
		public string Param1;
		public string Param2;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMutatorConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumMutatorConfigs
	{
		public List<QuantumMutatorConfig> QuantumConfigs = new List<QuantumMutatorConfig>();
	}
}