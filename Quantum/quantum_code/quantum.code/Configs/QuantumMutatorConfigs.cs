using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	public enum MutatorType
	{
		SpecialsCooldowns,
		Speed,
		HealthPerSeconds,
		AbsoluteAccuracy, // DEPRECATED, but kept in ENUM to not mess IDs
		HammerTime,
		ForceLevelPlayingField,
		HidePlayerNames,
		DoNotDropSpecials,
		PistolsOnly,
		SMGsOnly,
		MinigunsOnly,
		ShotgunsOnly,
		SnipersOnly,
		RPGsOnly,
		ConsumablesSharing,
		SpecialsMayhem,
	}

	[Serializable]
	public partial class QuantumMutatorConfig
	{
		public string Id;
		public MutatorType Type;
		public FP Param1;
		public FP Param2;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumMutatorConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumMutatorConfigs
	{
		public List<QuantumMutatorConfig> QuantumConfigs = new List<QuantumMutatorConfig>();

		private IDictionary<string, QuantumMutatorConfig> _dictionary;

		private object _lock = new object();
		
		/// <summary>
		/// Requests the <see cref="QuantumMutatorConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumMutatorConfig GetConfig(string id)
		{
			if (_dictionary == null)
			{
				lock (_lock)
				{
					var dictionary = new Dictionary<string, QuantumMutatorConfig>();
					for (var i = 0; i < QuantumConfigs.Count; i++)
					{
						dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
					}
					_dictionary = dictionary;
				}
			}
			return _dictionary[id];
		}
	}
}