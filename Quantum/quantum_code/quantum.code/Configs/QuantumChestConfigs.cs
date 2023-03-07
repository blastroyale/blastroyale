using System;
using System.Collections.Generic;
using Photon.Deterministic;

namespace Quantum
{
	[Serializable]
	public struct QuantumChestRarityModifierEntry
	{
		public FP Chance;
		public ChestType NewType;
	}

	[Serializable]
	public partial struct QuantumChestConfig
	{
		public GameId Id;
		public ChestType ChestType;
		public QuantumPair<int, int> RarityModifierRange;

		public List<QuantumPair<FP, uint>> RandomEquipment;
		public List<QuantumPair<FP, uint>> SmallConsumable;
		public List<QuantumPair<FP, uint>> LargeConsumable;

		public QuantumPair<int, int> DropFromPlayerBasedOnItemsRange;

		public QuantumGameModePair<List<QuantumChestRarityModifierEntry>> ChestTypeModifiers;
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

		/// <summary>
		/// Requests the <see cref="QuantumChestConfig"/> defined by the given <paramref name="type"/>
		/// </summary>
		public QuantumChestConfig GetConfig(ChestType type)
		{
			return GetConfig(type.GameId());
		}

		public GameId CheckItemRange(int itemCount)
		{
			for (var i = 0; i < QuantumConfigs.Count; i++)
			{
				var config = GetConfig(QuantumConfigs[i].Id);

				if (itemCount >= config.DropFromPlayerBasedOnItemsRange.Value1 &&
				    itemCount <= config.DropFromPlayerBasedOnItemsRange.Value2)
				{
					return config.Id;
				}
			}

			throw new ArgumentOutOfRangeException(nameof(ChestType), itemCount, null);
		}

		public EquipmentRarity GetChestRarity(ChestType type)
		{
			switch (type)
			{
				case ChestType.Common:
					return EquipmentRarity.Common;
				case ChestType.Uncommon:
					return EquipmentRarity.Uncommon;
				case ChestType.Rare:
					return EquipmentRarity.Rare;
				case ChestType.Epic:
					return EquipmentRarity.Epic;
				case ChestType.Legendary:
					return EquipmentRarity.Legendary;
				default:
					return EquipmentRarity.Common;
			}
		}
	}
}