using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial class QuantumStatConfig
	{
		public StatType StatType;
		public FP BaseValue;
		public FP RarityMultiplier;
		public FP LevelStepMultiplier;
		public bool CeilToInt;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumStatConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumStatConfigs
	{
		public List<QuantumStatConfig> QuantumConfigs = new List<QuantumStatConfig>();

		private Dictionary<StatType, QuantumStatConfig> _dictionary = null;

		public Dictionary<StatType, QuantumStatConfig> Dictionary => _dictionary;
			
		public override void Loaded(IResourceManager resourceManager, Native.Allocator allocator)
		{
			var dictionary = new Dictionary<StatType, QuantumStatConfig>();
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				dictionary.Add(QuantumConfigs[i].StatType, QuantumConfigs[i]);
			}
			_dictionary = dictionary;
		}
		
		/// <summary>
		/// Requests the <see cref="QuantumBaseEquipmentStatConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumStatConfig GetConfig(StatType type)
		{
			return _dictionary[type];
		}
	}
}