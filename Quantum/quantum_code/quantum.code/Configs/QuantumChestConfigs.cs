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
	public struct WeightedGameId
	{
		public GameId Id;
		public int Weight;
	}

	[Serializable]
	public class WeightedPoolDrop
	{
		/// <summary>
		/// Chance to drop this pool
		/// </summary>
		public FP Chance;
		
		/// <summary>
		/// Amount of items from the pool to drop.
		/// </summary>
		public byte Amount;
		
		/// <summary>
		/// Pool of items that the wheighted random will pick
		/// </summary>
		public List<WeightedGameId> Pool;
	}

	[Serializable]
	public partial class QuantumChestConfig
	{
		public GameId Id;
		public ChestType ChestType;

		public FP GoldenGunChance;
		
		public WeightedPoolDrop Specials;
		
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
	}
}