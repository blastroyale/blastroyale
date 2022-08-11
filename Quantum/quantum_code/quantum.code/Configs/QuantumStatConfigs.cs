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

		private IDictionary<StatType, QuantumStatConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumBaseEquipmentStatConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumStatConfig GetConfig(StatType type)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<StatType, QuantumStatConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].StatType, QuantumConfigs[i]);
				}
			}

			return _dictionary[type];
		}
	}
}