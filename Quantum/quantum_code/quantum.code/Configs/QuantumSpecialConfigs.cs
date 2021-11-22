using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumSpecialConfig
	{
		public GameId Id;
		public SpecialType SpecialType;
		public IndicatorVfxId Indicator;
		public uint BaseCharges;
		public uint MaxCharges;
		public FP InitialCooldown;
		public FP Cooldown;
		public FP SplashRadius;
		public uint PowerAmount;
		public FP Speed;
		public GameId ExtraId;
		public FP MinRange;
		public FP MaxRange;

		public bool IsAimable => MaxRange > FP._0;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumSpecialConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumSpecialConfigs
	{
		public List<QuantumSpecialConfig> QuantumConfigs = new List<QuantumSpecialConfig>();
	}
}