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
		private object _lock = new object();
		
		public List<QuantumStatConfig> QuantumConfigs = new List<QuantumStatConfig>();

		private Dictionary<StatType, QuantumStatConfig> _dictionary = null;

		/// <summary>
		/// The dictionary of stats in a <see cref="IReadOnlyDictionary{TKey,TValue}"/> format to avoid potential
		/// external manipulations
		/// </summary>
		public IReadOnlyDictionary<StatType, QuantumStatConfig> Dictionary
		{
			get
			{
				if (_dictionary == null)
				{
					lock (_lock)
					{
						var dictionary = new Dictionary<StatType, QuantumStatConfig>();
						for (var i = 0; i < QuantumConfigs.Count; i++)
						{
							dictionary.Add(QuantumConfigs[i].StatType, QuantumConfigs[i]);
						}
						_dictionary = dictionary;
					}
				}
				return _dictionary;
			}
		}

		/// <summary>
		/// Requests the <see cref="QuantumBaseEquipmentStatConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumStatConfig GetConfig(StatType type)
		{
			return Dictionary[type];
		}
	}
}