using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public enum MutatorType
	{
		SpecialsCooldowns,
		Speed,
		HealthPerSeconds
	}

	[Serializable]
	public partial struct QuantumMutatorConfig
	{
		public string Id;
		public MutatorType Type;
		public string Param1;
		public string Param2;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMutatorConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumMutatorConfigs
	{
		public List<QuantumMutatorConfig> QuantumConfigs = new List<QuantumMutatorConfig>();

		private IDictionary<string, QuantumMutatorConfig> _dictionary;

		/// <summary>
		/// Requests the <see cref="QuantumMutatorConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumMutatorConfig GetConfig(string id)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<string, QuantumMutatorConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
				}
			}

			return _dictionary[id];
		}
	}
}