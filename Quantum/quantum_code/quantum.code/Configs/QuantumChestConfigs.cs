using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public partial struct QuantumChestConfig
	{
		public GameId Id;
		public ChestType ChestType;
		public QuantumPair<int, int> RarityModifierRange;

		public List<QuantumPair<FP, uint>> RandomEquipment;
		public List<QuantumPair<FP, uint>> SmallConsumable;
		public List<QuantumPair<FP, uint>> LargeConsumable;
	}

	/// <summary>
	/// This is the quantum's asset config container for <see cref="QuantumChestConfigs"/>
	/// </summary>
	[AssetObjectConfig(GenerateAssetCreateMenu = true)]
	public partial class QuantumChestConfigs
	{
		public List<QuantumChestConfig> QuantumConfigs = new List<QuantumChestConfig>();

		private IDictionary<GameId, QuantumChestConfig> _dictionary;

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

		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="type"/>
		/// </summary>
		public QuantumChestConfig GetConfig(ChestType type)
		{
			return type switch
			{
				ChestType.Common => _dictionary[GameId.ChestCommon],
				ChestType.Uncommon => _dictionary[GameId.ChestUncommon],
				ChestType.Rare => _dictionary[GameId.ChestRare],
				ChestType.Epic => _dictionary[GameId.ChestEpic],
				ChestType.Legendary => _dictionary[GameId.ChestLegendary],
				_ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
			};
		}
	}
}