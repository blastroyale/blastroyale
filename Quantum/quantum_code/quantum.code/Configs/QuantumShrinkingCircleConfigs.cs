using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumShrinkingCircleConfig
	{
		public int Step;
		public FP DelayTime;
		public FP WarningTime;
		public FP ShringkingTime;
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