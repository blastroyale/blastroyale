using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumChestConfig
	{
		public GameId Id;
		public AssetRefEntityPrototype AssetRef;
		public ChestType ChestType;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumChestConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumChestConfigs
	{
		public List<QuantumChestConfig> QuantumConfigs = new List<QuantumChestConfig>();

		private IDictionary<GameId, QuantumChestConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumChestConfig GetConfig(GameId id)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<GameId, QuantumChestConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
				}
			}

			return _dictionary[id];
		}
	}
}