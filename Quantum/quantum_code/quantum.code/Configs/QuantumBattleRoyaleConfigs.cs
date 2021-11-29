using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumBattleRoyaleConfig
	{
		public int Step;
		public FP DelayTime;
		public FP WarningTime;
		public FP ShringkingTime;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumBattleRoyaleConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumBattleRoyaleConfigs
	{
		public List<QuantumBattleRoyaleConfig> QuantumConfigs = new List<QuantumBattleRoyaleConfig>();
	}
}