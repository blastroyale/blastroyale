using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumConsumableConfig
	{
		public GameId Id;
		public ConsumableType ConsumableType;
		public AssetRefEntityPrototype AssetRef; 
		public uint PowerAmount;
		public FP ConsumableCollectTime;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumConsumableConfig"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = false)]
	public partial class QuantumConsumableConfigs
	{
		public List<QuantumConsumableConfig> QuantumConfigs = new List<QuantumConsumableConfig>();

		private IDictionary<GameId, QuantumConsumableConfig> _dictionary = null;

		/// <summary>
		/// Requests the <see cref="QuantumConsumableConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumConsumableConfig GetConfig(GameId id)
		{
			if (_dictionary == null)
			{
				_dictionary = new Dictionary<GameId, QuantumConsumableConfig>();

				for (var i = 0; i < QuantumConfigs.Count; i++)
				{
					_dictionary.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
				}
			}

			return _dictionary[id];
		}
	}
}