using System;
using System.Collections.Generic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumDiagonalshotConfig
	{
		public GameId Id;
		public uint BaseAmount;
		public uint BaseAngle;
		public uint AmountLevelUpStep;
		public uint AngleLevelUpStep;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumDiagonalshotConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumDiagonalshotConfigs
	{
		public List<QuantumDiagonalshotConfig> QuantumConfigs = new List<QuantumDiagonalshotConfig>();
	}
}