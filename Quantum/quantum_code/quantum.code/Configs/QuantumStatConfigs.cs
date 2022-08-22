using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumStatConfig
	{
		public StatType StatType;
		public FP BaseValue;
		public FP RarityMultiplier;
		public FP LevelStepMultiplier;
		public FP GradeStepMultiplier;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumStatConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumStatConfigs
	{
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
					_dictionary = new Dictionary<StatType, QuantumStatConfig>();

					for (var i = 0; i < QuantumConfigs.Count; i++)
					{
						_dictionary.Add(QuantumConfigs[i].StatType, QuantumConfigs[i]);
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