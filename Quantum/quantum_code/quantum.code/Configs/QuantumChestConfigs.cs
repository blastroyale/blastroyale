using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public class QuantumChestRarityModifierEntry
	{
		public FP Chance;
		public ChestType NewType;
	}

	[Serializable]
	public partial class QuantumChestConfig
	{
		public GameId Id;
		public ChestType ChestType;

		public List<QuantumPair<FP, uint>> RandomEquipment;
		public List<QuantumPair<FP, uint>> SmallConsumable;

		public FP CollectableChestPickupRadius;
		public FP CollectTime;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumChestConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumChestConfigs
	{
		public List<QuantumChestConfig> QuantumConfigs = new List<QuantumChestConfig>();

		private IDictionary<GameId, QuantumChestConfig> _dictionary = null;

		private object _lock = new object();

		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="id"/>
		/// </summary>
		public QuantumChestConfig GetConfig(GameId id)
		{
			if (_dictionary == null)
			{
				lock (_lock)
				{
					var dict = new Dictionary<GameId, QuantumChestConfig>();

					for (var i = 0; i < QuantumConfigs.Count; i++)
					{
						dict.Add(QuantumConfigs[i].Id, QuantumConfigs[i]);
					}

					_dictionary = dict;
				}
			}

			return _dictionary[id];
		}

		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="type"/>
		/// </summary>
		public QuantumChestConfig GetConfig(ChestType type)
		{
			return GetConfig(type.GameId());
		}
	}
}