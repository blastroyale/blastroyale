using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumDestructibleConfig
	{
		public GameId Id;
		public AssetRefEntityPrototype AssetRef;
		public AssetRefEntityPrototype ProjectileAssetRef;
		public uint Health;
		public FP SplashRadius;
		public uint PowerAmount;
		public FP DestructionLengthTime;
	}
	
	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumDestructibleConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumDestructibleConfigs
	{
		public List<QuantumDestructibleConfig> QuantumConfigs = new List<QuantumDestructibleConfig>();
	}
}