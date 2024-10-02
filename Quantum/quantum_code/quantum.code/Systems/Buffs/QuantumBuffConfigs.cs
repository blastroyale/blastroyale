using System;
using System.Collections.Generic;
using System.Linq;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class BuffConfig
	{
		public BuffId Id;
		public List<BuffModifierConfig> Modifiers = new List<BuffModifierConfig>();
	}

	[Serializable]
	public class BuffModifierConfig
	{
		public BuffOperator Op;
		public FP Value;
		public BuffStat Stat;
	}
	
	[Serializable]
	public class BuffSource
	{
		public BuffId Buff;
		public SimulationItemConfig Source;
	}

	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public unsafe partial class QuantumBuffConfigs 
	{
		public List<BuffConfig> Buffs = new List<BuffConfig>();

		public List<BuffSource> Sources = new List<BuffSource>();
	}
}